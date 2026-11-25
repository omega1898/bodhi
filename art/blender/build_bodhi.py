# Purpose: builds Bodhi's 3D model in Blender, from nothing, every time it runs: a smooth
# one-piece goldendoodle body grown round a skeleton, a curly coat, floppy ears, a beard and a
# topknot, his colours (docs/characters.md), and the bones the game moves him with. It saves the
# model for Unity (FBX) and a few rendered pictures of him to check by eye.
#
# Run it from the repository folder (Blender needs no window for this):
#   blender -b --factory-startup -P art/blender/build_bodhi.py -- game/Resources/Models/bodhi.fbx previews/
#
# Blender's axes: x to his right, y toward his tail, z up (he faces -y, which Unity turns into
# its "forward"). Lengths are metres; he stands about 0.58 m at the shoulder.
import math
import os
import sys

import bpy
from mathutils import Vector

# The joints of his skeleton: where each one sits, and how thick his body is there (sideways,
# up-and-down). These match the game's stand-in dog (game/Runtime/Characters/DogFigure.cs).
JOINTS = {
    "tail_tip": ((0, 0.38, 0.84), (0.05, 0.05)),
    "tail_mid": ((0, 0.45, 0.72), (0.052, 0.052)),
    "tail_base": ((0, 0.39, 0.56), (0.038, 0.038)),
    "hips": ((0, 0.23, 0.47), (0.165, 0.17)),
    "back": ((0, 0.04, 0.48), (0.16, 0.17)),
    "chest": ((0, -0.15, 0.46), (0.175, 0.2)),
    "neck": ((0, -0.29, 0.58), (0.115, 0.12)),
    "head": ((0, -0.37, 0.72), (0.12, 0.115)),
    "muzzle": ((0, -0.5, 0.685), (0.072, 0.066)),
    "nose": ((0, -0.57, 0.68), (0.04, 0.035)),
}
for side, x in (("L", -1), ("R", 1)):
    JOINTS["shoulder." + side] = ((x * 0.09, -0.24, 0.36), (0.092, 0.095))
    JOINTS["elbow." + side] = ((x * 0.095, -0.21, 0.22), (0.072, 0.072))
    JOINTS["wrist." + side] = ((x * 0.095, -0.24, 0.07), (0.06, 0.06))
    JOINTS["front_paw." + side] = ((x * 0.095, -0.28, 0.03), (0.062, 0.035))
    JOINTS["hip." + side] = ((x * 0.08, 0.26, 0.4), (0.105, 0.11))
    JOINTS["stifle." + side] = ((x * 0.09, 0.17, 0.25), (0.078, 0.078))
    JOINTS["hock." + side] = ((x * 0.085, 0.31, 0.14), (0.056, 0.056))
    JOINTS["hind_paw." + side] = ((x * 0.085, 0.28, 0.03), (0.062, 0.035))

# Which joints join to which: the body's tree, from the hips outward.
LINKS = [
    ("hips", "tail_base"), ("tail_base", "tail_mid"), ("tail_mid", "tail_tip"),
    ("hips", "back"), ("back", "chest"), ("chest", "neck"), ("neck", "head"), ("head", "muzzle"), ("muzzle", "nose"),
]
for side in ("L", "R"):
    LINKS += [
        ("chest", "shoulder." + side), ("shoulder." + side, "elbow." + side), ("elbow." + side, "wrist." + side), ("wrist." + side, "front_paw." + side),
        ("hips", "hip." + side), ("hip." + side, "stifle." + side), ("stifle." + side, "hock." + side), ("hock." + side, "hind_paw." + side),
    ]

# His bones: name, from joint, to joint, parent bone. The game finds them by these names.
BONES = [
    ("back_half", "hips", "back", None),
    ("front_half", "back", "chest", "back_half"),
    ("neck", "chest", "neck", "front_half"),
    ("head", "neck", "head", "neck"),
    ("tail_1", "tail_base", "tail_mid", "back_half"),
    ("tail_2", "tail_mid", "tail_tip", "tail_1"),
]
for side in ("L", "R"):
    BONES += [
        ("front_upper." + side, "shoulder." + side, "elbow." + side, "front_half"),
        ("front_lower." + side, "elbow." + side, "wrist." + side, "front_upper." + side),
        ("front_foot." + side, "wrist." + side, "front_paw." + side, "front_lower." + side),
        ("hind_upper." + side, "hip." + side, "stifle." + side, "back_half"),
        ("hind_lower." + side, "stifle." + side, "hock." + side, "hind_upper." + side),
        ("hind_foot." + side, "hock." + side, "hind_paw." + side, "hind_lower." + side),
        ("ear." + side, None, None, "head"),  # Placed by hand below: ears aren't on the body's tree.
    ]

# His colours (sRGB), from docs/characters.md: red-apricot curls, darker ginger head and ears,
# cream muzzle, beard and bib, paler paws, a black nose and dark eyes.
APRICOT = (0.86, 0.6, 0.37)
SHADE = (0.72, 0.47, 0.28)
GINGER = (0.74, 0.45, 0.26)
CREAM = (0.96, 0.9, 0.8)
PAWS = (0.93, 0.83, 0.68)
NOSE_BLACK = (0.05, 0.04, 0.04)
EYE_BROWN = (0.12, 0.07, 0.05)
COLLAR_RED = (0.65, 0.12, 0.12)
BELL_GOLD = (0.9, 0.74, 0.32)


def joint(name):
    """Where a joint is, as a Blender vector."""
    return Vector(JOINTS[name][0])


def clear_scene():
    """Starts from an empty scene, so every run builds the same model."""
    bpy.ops.wm.read_factory_settings(use_empty=True)


def make_active(obj):
    """Selects one object and makes it the one Blender's tools act on."""
    for other in bpy.context.view_layer.objects:
        other.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_modifiers(obj):
    """Bakes an object's modifiers into its mesh, in order."""
    make_active(obj)
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)


def curl_texture(size):
    """A cellular texture whose cells read as curls: each cell bulges like a clump of fur."""
    texture = bpy.data.textures.new("curls", type="VORONOI")
    texture.noise_scale = size
    texture.distance_metric = "DISTANCE"
    texture.weight_1 = 1.0
    texture.noise_intensity = 1.0
    return texture


def curly(obj, strength, size, subdivide=2):
    """Makes a surface finer, then pushes it in and out in curly clumps, then softens it a touch.
    The cellular texture is 0 at the middle of each cell, so the push is negative: each cell's
    middle bulges out into a clump, and the gaps between clumps sink in."""
    smooth = obj.modifiers.new("Smooth surface", "SUBSURF")
    smooth.levels = subdivide
    smooth.render_levels = subdivide
    push = obj.modifiers.new("Curls", "DISPLACE")
    push.texture = curl_texture(size)
    push.strength = -strength
    push.mid_level = 0.35
    soften = obj.modifiers.new("Soften", "SMOOTH")
    soften.factor = 0.4
    soften.iterations = 2
    apply_modifiers(obj)


def body_mesh():
    """His body: a skin of the right thickness grown round the skeleton's joints and links
    (Blender's Skin modifier), made smooth, then curled."""
    names = list(JOINTS.keys())
    mesh = bpy.data.meshes.new("bodhi body")
    mesh.from_pydata([JOINTS[name][0] for name in names], [(names.index(a), names.index(b)) for a, b in LINKS], [])
    obj = bpy.data.objects.new("Bodhi", mesh)
    bpy.context.collection.objects.link(obj)
    obj.modifiers.new("Skin", "SKIN")
    for index, name in enumerate(names):
        skin = mesh.skin_vertices[0].data[index]
        skin.radius = JOINTS[name][1]
        skin.use_root = name == "hips"
    smooth = obj.modifiers.new("Smooth", "SUBSURF")
    smooth.levels = 2
    apply_modifiers(obj)
    curly(obj, 0.03, 0.045, subdivide=2)
    return obj


def ball(name, centre, radii, subdivisions=3):
    """A smooth ball (squashed or stretched), as its own object."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=centre)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = radii
    make_active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def ear(side):
    """One long floppy ear: a flattened, drooping teardrop of curls hanging from the side of his
    head, ginger like his crown."""
    x = -1 if side == "L" else 1
    # Its top is tucked inside the side of his head, so it grows out of it with no seam, and it
    # hangs close against his cheek down past his jaw, flaring a little at the bottom.
    middle = joint("head") + Vector((x * 0.092, 0.012, -0.05))
    obj = ball("ear." + side, middle, (0.042, 0.068, 0.13), 3)
    for vertex in obj.data.vertices:  # The ear's own corners are measured from its middle.
        below = max(0.0, -vertex.co.z / 0.13)
        vertex.co.x += x * 0.018 * below
    curly(obj, 0.018, 0.03, subdivide=1)
    return obj


def head_details():
    """The parts that aren't curls: a fluffy beard and topknot (curled), eyes and a shiny nose,
    and his red collar with a little gold bell and a red bow."""
    parts = []
    beard = ball("beard", joint("muzzle") + Vector((0, 0.0, -0.05)), (0.078, 0.072, 0.062), 3)
    curly(beard, 0.018, 0.03, subdivide=1)
    topknot = ball("topknot", joint("head") + Vector((0, 0.025, 0.06)), (0.108, 0.1, 0.075), 3)
    curly(topknot, 0.024, 0.035, subdivide=1)
    brows = []
    for x in (-1, 1):
        brows.append(ball("brow", joint("head") + Vector((x * 0.05, -0.095, 0.04)), (0.034, 0.022, 0.018), 2))
    parts += [beard, topknot] + brows
    eyes = [ball("eye", joint("head") + Vector((x * 0.05, -0.104, 0.014)), (0.018, 0.015, 0.018), 3) for x in (-1, 1)]
    nose = ball("nose", joint("nose") + Vector((0, -0.016, 0.012)), (0.03, 0.022, 0.024), 3)
    collar_parts = []
    neck_bottom = joint("chest").lerp(joint("neck"), 0.45)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.098, minor_radius=0.014, location=neck_bottom)
    collar = bpy.context.active_object
    collar.name = "collar"
    collar.rotation_euler = (math.radians(58), 0, 0)
    make_active(collar)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    bell = ball("bell", neck_bottom + Vector((0, -0.105, -0.06)), (0.02, 0.02, 0.02), 2)
    bow = [ball("bow", neck_bottom + Vector((x * 0.03, -0.1, -0.035)), (0.028, 0.012, 0.018), 2) for x in (-1, 1)]
    collar_parts = [collar, bell] + bow
    return parts, eyes, nose, collar_parts


def paint(obj, colour_at, curl_shadow=0.0):
    """Gives every corner of an object a colour, from `colour_at(position)`, in a colour layer
    named "Col" (the game's shader reads it). `curl_shadow` darkens the gaps between curls: a
    corner that sits further in than its neighbours (found by comparing it with the middle of
    the corners around it) gets a little darker, as curly fur does in its folds."""
    mesh = obj.data
    layer = mesh.color_attributes.new(name="Col", type="BYTE_COLOR", domain="POINT")
    sunk = [0.0] * len(mesh.vertices)
    if curl_shadow > 0:
        neighbours = [[] for _ in mesh.vertices]
        for edge in mesh.edges:
            a, b = edge.vertices
            neighbours[a].append(b)
            neighbours[b].append(a)
        for vertex in mesh.vertices:
            if neighbours[vertex.index]:
                middle = sum((mesh.vertices[n].co for n in neighbours[vertex.index]), Vector()) / len(neighbours[vertex.index])
                sunk[vertex.index] = (middle - vertex.co).dot(vertex.normal)
    for vertex in mesh.vertices:
        r, g, b = colour_at(vertex.co)
        shade = 1.0 - max(0.0, min(0.28, sunk[vertex.index] * curl_shadow))
        layer.data[vertex.index].color_srgb = (r * shade, g * shade, b * shade, 1.0)


def mix(a, b, share):
    """`share` of the way from colour `a` to colour `b`."""
    share = max(0.0, min(1.0, share))
    return tuple(a[i] + (b[i] - a[i]) * share for i in range(3))


def distance_to_segment(point, start, end):
    """How far a point is from the line between two joints."""
    line = end - start
    share = max(0.0, min(1.0, (point - start).dot(line) / max(line.length_squared, 1e-9)))
    return (point - (start + line * share)).length


def body_colour(position):
    """His coat colour at a point: apricot curls, shaded underneath, a ginger head, a cream muzzle
    and bib, paler paws."""
    colour = APRICOT
    colour = mix(colour, SHADE, (0.32 - position.z) / 0.12 * (1 if abs(position.y) < 0.3 else 0))  # Belly.
    near_muzzle = distance_to_segment(position, joint("head").lerp(joint("muzzle"), 0.45), joint("nose"))
    colour = mix(colour, GINGER, (position.z - 0.6) / 0.05 * (1 if position.y < -0.27 else 0))
    colour = mix(colour, CREAM, (0.075 - near_muzzle) / 0.025)
    bib = (position.y < -0.2) * (0.2 < position.z < 0.5) * (abs(position.x) < 0.1)
    colour = mix(colour, CREAM, bib * (0.1 - abs(position.x)) / 0.06)
    colour = mix(colour, PAWS, (0.09 - position.z) / 0.04)
    return colour


def make_armature():
    """His bones, at his joints, named as the game expects. Ears hang from the sides of his head."""
    data = bpy.data.armatures.new("bodhi skeleton")
    rig = bpy.data.objects.new("Bodhi rig", data)
    bpy.context.collection.objects.link(rig)
    make_active(rig)
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for name, start, end, parent in BONES:
        bone = data.edit_bones.new(name)
        if name.startswith("ear."):
            x = -1 if name.endswith("L") else 1
            bone.head = joint("head") + Vector((x * 0.09, 0.012, 0.06))
            bone.tail = bone.head + Vector((x * 0.02, 0.0, -0.22))
        else:
            bone.head = joint(start)
            bone.tail = joint(end)
        if parent:
            bone.parent = made[parent]
        made[name] = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def weigh(obj, bone_names):
    """Ties each corner of a mesh to the nearest bones, more to closer ones, so the skin bends
    smoothly where two bones meet instead of creasing (a simple version of what riggers do)."""
    segments = {}
    for name, start, end, _ in BONES:
        if name in bone_names and start:
            segments[name] = (joint(start), joint(end))
        elif name in bone_names:
            x = -1 if name.endswith("L") else 1
            head = joint("head") + Vector((x * 0.09, 0.012, 0.06))
            segments[name] = (head, head + Vector((x * 0.02, 0.0, -0.22)))
    groups = {name: obj.vertex_groups.new(name=name) for name in segments}
    for vertex in obj.data.vertices:
        distances = sorted((distance_to_segment(vertex.co, *segments[name]), name) for name in segments)
        nearest = distances[0][0]
        chosen = [(d, name) for d, name in distances[:3] if d < nearest * 1.5 + 0.015]
        strengths = [(1.0 / max(d, 0.004) ** 4, name) for d, name in chosen]
        total = sum(s for s, _ in strengths)
        for strength, name in strengths:
            groups[name].add([vertex.index], strength / total, "REPLACE")


def join(objects, into):
    """Merges several objects into one mesh object (keeping their colours and bone ties)."""
    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    for obj in objects:
        obj.select_set(True)
    into.select_set(True)
    bpy.context.view_layer.objects.active = into
    bpy.ops.object.join()


def preview_material():
    """A material showing the mesh's own colours, for the check pictures (Unity uses its own)."""
    material = bpy.data.materials.new("coat")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    colours = nodes.new("ShaderNodeVertexColor")
    colours.layer_name = "Col"
    material.node_tree.links.new(colours.outputs["Color"], shader.inputs["Base Color"])
    shader.inputs["Roughness"].default_value = 0.8
    return material


def render_previews(folder, target):
    """Three pictures of him (side, front three-quarter, back three-quarter) under a cool moon
    and a warm fill, so his shape and colours can be checked without opening Blender."""
    os.makedirs(folder, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 900, 700
    world = bpy.data.worlds.new("night")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.07, 0.12, 1)
    scene.world = world
    sun = bpy.data.objects.new("moon", bpy.data.lights.new("moon", "SUN"))
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(50), 0, math.radians(-40))
    fill = bpy.data.objects.new("warm", bpy.data.lights.new("warm", "AREA"))
    fill.data.energy = 60
    fill.data.color = (1.0, 0.75, 0.5)
    fill.location = (1.2, -1.2, 0.8)
    for light in (sun, fill):
        scene.collection.objects.link(light)
    camera = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
    scene.collection.objects.link(camera)
    scene.camera = camera
    middle = Vector((0, 0, 0.42))
    for name, angle in (("side", 90), ("front", 35), ("back", 145)):
        radians = math.radians(angle)
        camera.location = middle + Vector((math.sin(radians) * -1.9, -math.cos(radians) * 1.9, 0.35))
        camera.rotation_euler = (middle - camera.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(folder, "bodhi_" + name + ".png")
        bpy.ops.render.render(write_still=True)


def main():
    """Builds him, checks him in pictures, and saves him for Unity."""
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out_path = arguments[0] if arguments else "game/Resources/Models/bodhi.fbx"
    preview_folder = arguments[1] if len(arguments) > 1 else ""
    clear_scene()

    body = body_mesh()
    paint(body, body_colour, curl_shadow=90.0)
    body_bones = [name for name, start, _, _ in BONES if start]
    weigh(body, body_bones)

    ears = [ear("L"), ear("R")]
    for one in ears:
        paint(one, lambda position: mix(GINGER, SHADE, 0.2), curl_shadow=90.0)
        weigh(one, [one.name, "head"])
    fluff, eyes, nose, collar = head_details()
    for part in fluff:
        is_beard = part.name.startswith("beard")
        paint(part, (lambda position: CREAM) if is_beard else (lambda position: GINGER), curl_shadow=90.0)
        weigh(part, ["head"])
    for part in eyes:
        paint(part, lambda position: EYE_BROWN)
        weigh(part, ["head"])
    paint(nose, lambda position: NOSE_BLACK)
    weigh(nose, ["head"])
    for part in collar:
        colour = BELL_GOLD if part.name.startswith("bell") else COLLAR_RED
        paint(part, lambda position, colour=colour: colour)
        weigh(part, ["neck", "front_half"])
    join(ears + fluff + eyes + [nose] + collar, body)

    rig = make_armature()
    body.parent = rig
    skin = body.modifiers.new("Skeleton", "ARMATURE")
    skin.object = rig
    body.data.materials.append(preview_material())
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    print("Bodhi:", len(body.data.vertices), "corners,", len(rig.data.bones), "bones")

    if preview_folder:
        render_previews(preview_folder, body)

    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)
    for obj in bpy.context.view_layer.objects:
        obj.select_set(obj in (body, rig))
    # Leaf bones: an extra tiny bone at the end of each chain, so the game can tell which way
    # every bone points (it aims each bone from its start to its child).
    bpy.ops.export_scene.fbx(
        filepath=out_path, use_selection=True, object_types={"ARMATURE", "MESH"},
        add_leaf_bones=True, bake_anim=False, axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", colors_type="SRGB",
    )
    print("Saved", out_path)


main()
