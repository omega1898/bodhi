# Purpose: tests for export_sprites.py's pixel work, on small made-up pictures (no SVG drawing,
# so they run in a second): packing never overlaps or runs over, bleeding colours the see-through
# rim without making it visible, trimming keeps the whole drawing, and each drawing's feet point
# lands exactly where its feet were drawn. Includes break-it tests with odd input.
#
# Run from the repository root:  python3 art/tools/export_sprites_test.py
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image  # noqa: E402

import export_sprites as tool  # noqa: E402

problems = []


# Records a result: prints ok or FAIL with what was checked.
def check(passed, what):
    print(('  ok   ' if passed else '  FAIL ') + what)
    if not passed:
        problems.append(what)


# Packed boxes stay inside the sheet width, never overlap, and keep the gap between them.
def test_packing_never_overlaps():
    sizes = [(300, 200), (700, 180), (900, 210), (200, 50), (1000, 190), (2048, 30), (5, 5)]
    places, (sheet_width, sheet_height) = tool.pack_in_rows(sizes)
    boxes = [(left, top, left + width, top + height) for (left, top), (width, height) in zip(places, sizes)]
    inside = all(right <= tool.widest_sheet and right <= sheet_width and bottom <= sheet_height for _, _, right, bottom in boxes)
    apart = True
    for first in range(len(boxes)):
        for second in range(first + 1, len(boxes)):
            a, b = boxes[first], boxes[second]
            gap_across = max(b[0] - a[2], a[0] - b[2])
            gap_down = max(b[1] - a[3], a[1] - b[3])
            if max(gap_across, gap_down) < tool.packing_gap:
                apart = False
    check(inside, 'packed drawings stay inside the sheet')
    check(apart, 'packed drawings never overlap and keep a ' + str(tool.packing_gap) + ' pixel gap')


# Bleeding gives the see-through rim the drawing's colour but leaves it see-through, and leaves
# drawn pixels exactly as they were.
def test_bleeding_colours_the_rim():
    picture = Image.new('RGBA', (9, 9), (0, 0, 0, 0))
    picture.putpixel((4, 4), (200, 100, 50, 255))
    picture.putpixel((5, 4), (200, 100, 50, 128))
    bled = tool.bleed_colours(picture)
    check(bled.getpixel((4, 4)) == (200, 100, 50, 255) and bled.getpixel((5, 4)) == (200, 100, 50, 128),
          'bleeding leaves drawn pixels unchanged')
    check(bled.getpixel((3, 4)) == (200, 100, 50, 0), 'the pixel beside a drawing takes its colour and stays see-through')
    check(bled.getpixel((0, 0))[3] == 0 and bled.getpixel((0, 0))[:3] == (200, 100, 50),
          'the colour reaches ' + str(tool.bleed_pixels) + ' pixels out')
    check(bled.getpixel((8, 8))[:3] == (200, 100, 50), 'corners are reached too')


# An empty picture bleeds to nothing without dividing by zero.
def test_bleeding_an_empty_picture():
    bled = tool.bleed_colours(Image.new('RGBA', (4, 4), (0, 0, 0, 0)))
    check(all(value == 0 for value in bled.tobytes()), 'an empty picture stays empty')


# Trimming keeps every drawn pixel and a margin, and an empty cell gives a 1 by 1 box.
def test_trimming():
    cell = Image.new('RGBA', (40, 60), (0, 0, 0, 0))
    for x in range(10, 20):
        cell.putpixel((x, 30), (10, 10, 10, 255))
    left, top, right, bottom = tool.trimmed_box(cell)
    check((left, top, right, bottom) == (9, 29, 21, 32), 'a trimmed box holds the drawing and a 1 pixel margin')
    cell.putpixel((0, 0), (10, 10, 10, 255))
    cell.putpixel((39, 59), (10, 10, 10, 255))
    check(tool.trimmed_box(cell) == (0, 0, 40, 60), 'a drawing touching the cell edge is kept whole')
    check(tool.trimmed_box(Image.new('RGBA', (40, 60), (0, 0, 0, 0))) == (0, 0, 1, 1), 'an empty cell becomes a 1 by 1 box')


# A made-up sheet of three cells, each with a dot drawn on the feet point: after packing, every
# drawing's pivot (counted from its box's bottom-left, Unity's way) is exactly on the dot.
def test_pivot_lands_on_the_feet():
    pixels_per_unit = 2
    cell = {'width': 20, 'height': 30, 'feet_x': 8, 'feet_y': 25}
    columns = 2
    sheet = Image.new('RGBA', (columns * 40, 2 * 60), (0, 0, 0, 0))
    shifts = [(0, 0), (6, -10), (-5, 4)]
    for index, (shift_x, shift_y) in enumerate(shifts):
        cell_left, cell_top = (index % columns) * 40, (index // columns) * 60
        for x in range(4, 30):  # a body above the feet, shifted per drawing
            for y in range(8, 50):
                sheet.putpixel((cell_left + x + shift_x, cell_top + y + shift_y), (90, 90, 90, 255))
        sheet.putpixel((cell_left + 16, cell_top + 50), (255, 0, 0, 255))  # the feet: (8, 25) at 2 per unit
    packed, frames = tool.pack_animation(sheet, cell, 3, columns, pixels_per_unit)
    all_on_feet = True
    for frame in frames:
        # The pixel whose corner is the pivot: the box's left plus pivot_x, and down from the
        # box's top. Rows from the top of the picture are height - 1 - row from the bottom.
        column = frame['x'] + int(frame['pivot_x'])
        row_from_bottom = frame['y'] + int(frame['pivot_y']) - 1
        found = packed.getpixel((column, packed.height - 1 - row_from_bottom))
        if found[:3] != (255, 0, 0) or found[3] != 255:
            all_on_feet = False
    check(all_on_feet, "every packed drawing's pivot sits on its drawn feet")
    inside = all(0 <= f['x'] and f['x'] + f['width'] <= packed.width and 0 <= f['y'] and f['y'] + f['height'] <= packed.height for f in frames)
    check(inside, 'every box lies inside its picture')


# Break it: a drawing wider than any sheet is refused with a clear error, not packed off the edge.
def test_too_wide_is_refused():
    try:
        tool.pack_in_rows([(tool.widest_sheet + 1, 10)])
        check(False, 'a drawing wider than a sheet is refused')
    except ValueError:
        check(True, 'a drawing wider than a sheet is refused')


# Break it: no drawings packs to a 1 by 1 sheet rather than crashing.
def test_close_up_sheets_may_be_wider():
    places, (sheet_width, _) = tool.pack_in_rows([(3000, 10), (900, 10)], tool.widest_close_up_sheet)
    check(sheet_width == 3902 and places[1] == (3002, 0), 'a close-up sheet holds a drawing too wide for a normal one')


def test_too_tall_is_refused():
    # Break it: drawings that would stack up taller than the limit are refused, not saved as a
    # picture the iPhone would shrink and blur.
    tall = Image.new('RGBA', (20, 30), (200, 120, 60, 255))
    cell = {'width': 20, 'height': 30, 'feet_x': 10, 'feet_y': 30}
    try:
        tool.pack_animation(tall, cell, 1, 1, 1, widest=25)
        check(False, 'a sheet taller than the limit is refused')
    except ValueError as error:
        check('tall' in str(error), 'a sheet taller than the limit is refused')


def test_close_ups_come_from_real_sets():
    for close_name, (set_name, animation_names) in tool.close_up_sets.items():
        check(close_name == set_name + '_close' and set_name in tool.pixels_per_unit_of_set and len(animation_names) > 0,
              close_name + ' is drawn from ' + set_name)
    check(tool.close_up_pixels_per_unit >= 2 * tool.pixels_per_unit_of_set['her'], 'close-ups have at least twice the pixels')


def test_nothing_to_pack():
    places, size = tool.pack_in_rows([])
    check(places == [] and size == (1, 1), 'nothing to pack gives an empty 1 by 1 sheet')


# Numbers are written for C# without stray decimals, and odd values still give valid C#.
def test_numbers_for_csharp():
    check(tool.number(3.0) == '3' and tool.number(2.456) == '2.46' and tool.number(-0.5) == '-0.5', 'numbers are written plainly')
    check(tool.number_list([]) == 'new double[] {  }', 'an empty list is still valid C#')
    check(tool.hold_point({}) == [0, 0] and tool.hold_point({'mouth': [3, -4, 9]}) == [3, -4], 'a missing hold point is the feet')


for test in [test_packing_never_overlaps, test_bleeding_colours_the_rim, test_bleeding_an_empty_picture, test_trimming,
             test_pivot_lands_on_the_feet, test_too_wide_is_refused, test_close_up_sheets_may_be_wider, test_too_tall_is_refused,
             test_close_ups_come_from_real_sets, test_nothing_to_pack, test_numbers_for_csharp]:
    test()
if problems:
    print(str(len(problems)) + ' problem(s).')
    sys.exit(1)
print('All sprite export tests pass.')
