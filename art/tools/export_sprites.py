# Purpose: turns the animation drawings (art/animations/*.svg) into the pictures the Unity iPhone
# app draws: one PNG per animation in game/Resources/Frames, plus game/Logic/FrameSheets.Data.cs,
# which says where each drawing is in its PNG and where its feet are.
#
# Why pictures and not the SVG files: a phone's graphics chip can only draw pictures. Turning a
# furry SVG into pixels takes far too long to do while the game runs (it is what made the browser
# version stutter), so it is done once, here, at the size the iPhone shows it.
#
# What it does for each drawing, pixel by pixel:
#   1. Draws the SVG sheet at `pixels_per_unit` (3.5 pixels per game unit: a touch sharper than
#      the most the iPhone camera ever zooms to, so nothing is blurry and nothing is wasted).
#   2. Cuts each drawing out of its cell and trims the empty space around it, so the phone's
#      memory holds only drawn pixels.
#   3. Fills the colour of the see-through pixels round each drawing with the colour of the
#      drawing next to them ("bleeding"). The graphics chip blends neighbouring pixels when it
#      draws a picture between whole pixels, and a see-through pixel left black would blend a dark
#      rim round the fur on every frame.
#   4. Packs the trimmed drawings in rows into one PNG per animation, with a gap between them so
#      the blending never reaches the next drawing.
#
# Needs: Python 3 with Pillow and numpy, ffmpeg (draws the SVG files) and node (reads the frame
# list). Run from the repository root, after node art/tools/draw_frames.js:
#   python3 art/tools/export_sprites.py
import json
import os
import subprocess

import numpy
from PIL import Image

# Pixels per game unit for each set of drawings. The puppy is never shown at more than about
# 0.7 of full size and Biscuit at 0.9, so they're drawn smaller and use less memory.
pixels_per_unit_of_set = {'her': 3.5, 'bodhi': 3.5, 'biscuit': 3.15, 'bodhi_puppy': 2.45}

# Empty pixels kept round each trimmed drawing, and the gap between drawings in a sheet.
trim_margin = 1
packing_gap = 2

# The widest a sheet may be. Old iPhones can't use pictures over 4096 pixels on a side, and
# 2048 keeps every sheet comfortably under that.
widest_sheet = 2048

# How many pixels out from a drawing the bleeding reaches.
bleed_pixels = 4

repository = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
frames_folder = os.path.join(repository, 'game', 'Resources', 'Frames')
data_file = os.path.join(repository, 'game', 'Logic', 'FrameSheets.Data.cs')


# Reads the frame list that draw_frames.js wrote (prototype/web/js/frames_atlas.js) by asking
# node to print it as JSON, so this tool never has to understand JavaScript itself.
def read_frame_list():
    script = ("const vm = require('vm'); const fs = require('fs'); const box = {};"
              "vm.runInNewContext(fs.readFileSync('prototype/web/js/frames_atlas.js', 'utf8') +"
              " ';this.atlas = frames_atlas', box); console.log(JSON.stringify(box.atlas));")
    output = subprocess.run(['node', '-e', script], cwd=repository, check=True, capture_output=True, text=True)
    return json.loads(output.stdout)


# Draws one SVG sheet into an RGBA picture `width` pixels wide (ffmpeg's SVG reader).
def draw_sheet(svg_path, width, scratch_path):
    subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-width', str(width), '-i', svg_path, scratch_path], check=True)
    picture = Image.open(scratch_path).convert('RGBA')
    picture.load()
    os.remove(scratch_path)
    return picture


# The box round the drawn pixels of a cell, with `trim_margin` empty pixels kept round it, as
# (left, top, right, bottom). An empty cell keeps a 1 by 1 box so every frame still exists.
def trimmed_box(cell_picture):
    drawn = cell_picture.getchannel('A').getbbox()
    if drawn is None:
        return (0, 0, 1, 1)
    left, top, right, bottom = drawn
    return (max(0, left - trim_margin), max(0, top - trim_margin),
            min(cell_picture.width, right + trim_margin), min(cell_picture.height, bottom + trim_margin))


# Gives every see-through pixel near the drawing the average colour of its drawn neighbours,
# a ring at a time, keeping it see-through. Returns a new picture.
def bleed_colours(picture):
    pixels = numpy.array(picture).astype(numpy.float64)
    colour = pixels[:, :, :3]
    known = pixels[:, :, 3] > 0
    for _ring in range(bleed_pixels):
        colour_sum = numpy.zeros_like(colour)
        known_count = numpy.zeros(known.shape)
        padded_colour = numpy.pad(colour * known[:, :, None], ((1, 1), (1, 1), (0, 0)))
        padded_known = numpy.pad(known.astype(numpy.float64), 1)
        for down in (0, 1, 2):
            for across in (0, 1, 2):
                colour_sum += padded_colour[down:down + known.shape[0], across:across + known.shape[1]]
                known_count += padded_known[down:down + known.shape[0], across:across + known.shape[1]]
        newly_known = (~known) & (known_count > 0)
        colour[newly_known] = colour_sum[newly_known] / known_count[newly_known][:, None]
        known = known | newly_known
    pixels[:, :, :3] = colour
    return Image.fromarray(numpy.clip(numpy.round(pixels), 0, 255).astype(numpy.uint8), 'RGBA')


# Places boxes of the given (width, height) in rows, left to right, no wider than `widest_sheet`.
# Returns each box's (left, top) and the sheet's (width, height).
def pack_in_rows(sizes):
    places = []
    row_left, row_top, row_height, sheet_width = 0, 0, 0, 0
    for width, height in sizes:
        if width > widest_sheet:
            raise ValueError('a drawing ' + str(width) + ' pixels wide is wider than a sheet')
        if row_left > 0 and row_left + width > widest_sheet:
            row_top += row_height + packing_gap
            row_left, row_height = 0, 0
        places.append((row_left, row_top))
        row_left += width + packing_gap
        row_height = max(row_height, height)
        sheet_width = max(sheet_width, row_left - packing_gap)
    return places, (max(1, sheet_width), max(1, row_top + row_height))


# Cuts, trims, bleeds and packs one animation's sheet. Returns the packed picture and, per
# drawing, its box in Unity's terms: x and y of the bottom-left corner (Unity counts rows up from
# the bottom of a picture), width, height, and the feet's position in pixels from that corner.
def pack_animation(sheet_picture, cell, frame_count, columns, pixels_per_unit):
    cell_width = round(cell['width'] * pixels_per_unit)
    cell_height = round(cell['height'] * pixels_per_unit)
    feet_x = cell['feet_x'] * pixels_per_unit
    feet_y = cell['feet_y'] * pixels_per_unit
    pieces, boxes = [], []
    for index in range(frame_count):
        cell_left = (index % columns) * cell_width
        cell_top = (index // columns) * cell_height
        cell_picture = sheet_picture.crop((cell_left, cell_top, cell_left + cell_width, cell_top + cell_height))
        box = trimmed_box(cell_picture)
        pieces.append(bleed_colours(cell_picture.crop(box)))
        boxes.append(box)
    places, (sheet_width, sheet_height) = pack_in_rows([piece.size for piece in pieces])
    packed = Image.new('RGBA', (sheet_width, sheet_height), (0, 0, 0, 0))
    frames = []
    for piece, box, (left, top) in zip(pieces, boxes, places):
        packed.paste(piece, (left, top))
        width, height = piece.size
        frames.append({
            'x': left, 'y': sheet_height - top - height, 'width': width, 'height': height,
            'pivot_x': feet_x - box[0], 'pivot_y': box[3] - feet_y,
        })
    return bleed_colours(packed), frames


# A number written for C#: whole numbers plainly, others to 2 decimal places.
def number(value):
    rounded = round(float(value), 2)
    return str(int(rounded)) if rounded == int(rounded) else repr(rounded)


# A list of numbers written as a C# array.
def number_list(values):
    return 'new double[] { ' + ', '.join(number(value) for value in values) + ' }'


# Writes FrameSheets.Data.cs: every set, animation and drawing, as C# the game compiles in.
def write_data(sets):
    lines = [
        '// Purpose: where every animation drawing is in its picture (game/Resources/Frames), where',
        "// its feet are, and the points the game hangs things on (a carried glove, mud on the paws).",
        '// Generated by art/tools/export_sprites.py. Don\'t edit by hand: change the art, then run',
        '// node art/tools/draw_frames.js and python3 art/tools/export_sprites.py.',
        'namespace Bodhi.Game',
        '{',
        '    public static partial class FrameSheets',
        '    {',
        '        // Makes every set of drawings.',
        '        static FrameSet[] make_all()',
        '        {',
        '            return new[]',
        '            {',
    ]
    for set_name, set_data in sets.items():
        lines.append('                new FrameSet("' + set_name + '", ' + number(set_data['pixels_per_unit']) + ', new[]')
        lines.append('                {')
        for animation_name, animation in set_data['animations'].items():
            lines.append('                    new FrameAnimation("' + animation_name + '", "' + animation['picture'] + '", ' +
                         str(animation['sheet_width']) + ', ' + str(animation['sheet_height']) + ', ' +
                         number(animation['cycle_length']) + ', new[]')
            lines.append('                    {')
            for frame in animation['frames']:
                feet = [coordinate for foot in frame['feet'] for coordinate in foot]
                lines.append('                        new SpriteFrame(' + ', '.join([
                    str(frame['x']), str(frame['y']), str(frame['width']), str(frame['height']),
                    number(frame['pivot_x']), number(frame['pivot_y']),
                    number_list(feet), number_list(frame['hold'])]) + '),')
            lines.append('                    }),')
        lines.append('                }),')
    lines += ['            };', '        }', '    }', '}', '']
    with open(data_file, 'w', newline='\n') as file:
        file.write('\n'.join(lines))


# The point a character holds things at, in game units from the feet: a dog's mouth, her hand.
def hold_point(points):
    point = points.get('mouth') or points.get('hand') or [0, 0]
    return [point[0], point[1]]


# Exports every set.
def main():
    atlas = read_frame_list()
    os.makedirs(frames_folder, exist_ok=True)
    sets = {}
    for set_name in atlas:
        pixels_per_unit = pixels_per_unit_of_set[set_name]
        cell = atlas[set_name]['cell']
        sets[set_name] = {'pixels_per_unit': pixels_per_unit, 'animations': {}}
        for animation_name, animation in atlas[set_name]['animations'].items():
            picture_name = set_name + '_' + animation_name
            picture_path = os.path.join(frames_folder, picture_name + '.png')
            sheet_width = round(animation['columns'] * cell['width'] * pixels_per_unit)
            sheet = draw_sheet(os.path.join(repository, animation['sheet']), sheet_width, picture_path + '.drawing.png')
            packed, frames = pack_animation(sheet, cell, animation['frames'], animation['columns'], pixels_per_unit)
            packed.save(picture_path, optimize=True)
            for frame, points in zip(frames, animation['points']):
                frame['feet'] = points['feet']
                frame['hold'] = hold_point(points)
            sets[set_name]['animations'][animation_name] = {
                'picture': 'Frames/' + picture_name, 'sheet_width': packed.width, 'sheet_height': packed.height,
                'cycle_length': animation.get('cycle_length', 0), 'frames': frames,
            }
            print(picture_name.ljust(26), str(len(frames)).rjust(3), 'drawings', str(packed.width) + 'x' + str(packed.height))
    write_data(sets)


if __name__ == '__main__':
    main()
