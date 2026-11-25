# Purpose: builds Em's 3D model in Blender, from nothing, every time it runs: her body grown round
# a skeleton, her teal coat (sleeves and a skirt that flares to mid-thigh, so her legs move inside
# it), jeans, brown boots and rust mittens, her rust scarf, a gently stylised face (blue-grey eyes,
# rosy cheeks) and long honey-blonde hair, straight and parted in the middle, made of many strands
# so it falls naturally (docs/characters.md). It adds the bones the game moves her with, saves the
# model for Unity (FBX), and can render pictures of her to check by eye.
#
# Run it from the repository folder:
#   blender -b --factory-startup -P art/blender/build_em.py -- game/Resources/Models/em.fbx previews/
#
# Blender's axes: x to her left, y behind her, z up (she faces -y, which Unity turns into its
# "forward"). Lengths are metres: she is about 1.68 m tall. Her joints match the game's figure
# (game/Runtime/Characters/HumanFigure.cs), so her real model and the figure move as one.
import math
import os
import random
import sys

import bpy
from mathutils import Vector

# Her joints: where each sits, and how thick she is there (sideways, front-to-back).
BODY = {
    "hips": ((0, 0, 0.93), (0.15, 0.11)),
    "spine": ((0, 0, 1.03), (0.13, 0.1)),
    "chest": ((0, 0, 1.2), (0.15, 0.1)),
    "neck": ((0, 0, 1.43), (0.05, 0.05)),
    "head": ((0, -0.01, 1.515), (0.05, 0.05)),
}
for side, x in (("L", 1), ("R", -1)):
    BODY["upper_arm." + side] = ((x * 0.175, 0.01, 1.4), (0.048, 0.048))
    BODY["forearm." + side] = ((x * 0.19, 0.01, 1.12), (0.04, 0.04))
    BODY["hand." + side] = ((x * 0.2, 0.01, 0.87), (0.034, 0.03))
    BODY["hand_end." + side] = ((x * 0.205, 0.01, 0.76), (0.03, 0.02))
    BODY["thigh." + side] = ((x * 0.085, 0, 0.89), (0.1, 0.1))
    BODY["shin." + side] = ((x * 0.085, 0, 0.47), (0.072, 0.072))
    BODY["foot." + side] = ((x * 0.085, 0, 0.07), (0.064, 0.064))
    BODY["toe." + side] = ((x * 0.085, -0.17, 0.035), (0.058, 0.04))

LINKS = [("hips", "spine"), ("spine", "chest"), ("chest", "neck"), ("neck", "head")]
for side in ("L", "R"):
    LINKS += [
        ("chest", "upper_arm." + side), ("upper_arm." + side, "forearm." + side), ("forearm." + side, "hand." + side), ("hand." + side, "hand_end." + side),
        ("hips", "thigh." + side), ("thigh." + side, "shin." + side), ("shin." + side, "foot." + side), ("foot." + side, "toe." + side),
    ]

# Her bones: name, from joint, to joint, parent. The game finds them by these names.
BONES = [
    ("hips", "hips", "spine", None),
    ("spine", "spine", "chest", "hips"),
    ("chest", "chest", "neck", "spine"),
    ("neck", "neck", "head", "chest"),
    ("head", "head", None, "neck"),
    ("hair", None, None, "head"),
    ("coat_skirt", None, None, "hips"),
    ("scarf_end", None, None, "chest"),
]
for side in ("L", "R"):
    BONES += [
        ("upper_arm." + side, "upper_arm." + side, "forearm." + side, "chest"),
        ("forearm." + side, "forearm." + side, "hand." + side, "upper_arm." + side),
        ("hand." + side, "hand." + side, "hand_end." + side, "forearm." + side),
        ("thigh." + side, "thigh." + side, "shin." + side, "hips"),
        ("shin." + side, "shin." + side, "foot." + side, "thigh." + side),
        ("foot." + side, "foot." + side, "toe." + side, "shin." + side),
    ]
# Bones that aren't on the body's tree: where each starts and ends.
EXTRA_BONES = {
    "head": ((0, -0.01, 1.515), (0, -0.01, 1.7)),
    "hair": ((0, 0.03, 1.62), (0, 0.06, 1.3)),
    "coat_skirt": ((0, 0, 0.99), (0, 0, 0.6)),
    "scarf_end": ((-0.05, -0.1, 1.39), (-0.06, -0.12, 1.18)),
}

# Her colours (sRGB): the art bible's teal coat, rust scarf and honey-blonde hair, fair skin with
# rosy cheeks, blue-grey eyes, jeans and brown boots.
SKIN = (0.95, 0.8, 0.72)
CHEEKS = (0.95, 0.62, 0.6)
LIPS = (0.82, 0.5, 0.5)
EYE_WHITE = (0.95, 0.95, 0.94)
EYE_IRIS = (0.36, 0.47, 0.56)
EYE_DARK = (0.08, 0.08, 0.1)
COAT = (0.17, 0.4, 0.39)
COAT_DARK = (0.12, 0.3, 0.29)
SCARF = (0.73, 0.33, 0.17)
HAIR = (0.86, 0.72, 0.4)
HAIR_DEEP = (0.66, 0.5, 0.25)
JEANS = (0.2, 0.26, 0.4)
BOOTS = (0.32, 0.21, 0.14)
BUTTON = (0.1, 0.18, 0.18)


def point(name):
    """Where one of her joints is."""
    return Vector(BODY[name][0])


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


def grow(name, joints, links, smooth_levels=2):
    """A smooth body part grown round joints joined by links (Blender's Skin modifier), each
    joint as thick as it says."""
    names = list(joints.keys())
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([joints[n][0] for n in names], [(names.index(a), names.index(b)) for a, b in links], [])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.modifiers.new("Skin", "SKIN")
    for index, joint_name in enumerate(names):
        skin = mesh.skin_vertices[0].data[index]
        skin.radius = joints[joint_name][1]
        skin.use_root = index == 0
    smooth = obj.modifiers.new("Smooth", "SUBSURF")
    smooth.levels = smooth_levels
    apply_modifiers(obj)
    return obj


def ball(name, centre, radii, subdivisions=3):
    """A smooth ball (squashed or stretched), as its own object."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=centre)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = radii
    make_active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.shade_smooth()
    return obj


def body():
    """Her body under the clothes: legs (jeans), neck, and boots. The torso and arms are inside
    her coat, so they only need to be roughly the right shape."""
    obj = grow("Em body", BODY, LINKS)
    return obj


def coat():
    """Her teal coat: a fitted body from shoulders to waist, sleeves to the wrist, and a skirt
    flaring from the waist to mid-thigh, open round her legs so they swing inside it."""
    joints = {
        "waist": ((0, 0, 1.0), (0.155, 0.12)),
        "chest": ((0, 0, 1.22), (0.17, 0.12)),
        "collar": ((0, 0, 1.38), (0.075, 0.07)),
        "hem": ((0, 0, 0.62), (0.2, 0.15)),
    }
    links = [("waist", "chest"), ("chest", "collar"), ("waist", "hem")]
    for side, x in (("L", 1), ("R", -1)):
        joints["shoulder." + side] = ((x * 0.165, 0.01, 1.36), (0.065, 0.065))
        joints["elbow." + side] = ((x * 0.19, 0.01, 1.12), (0.052, 0.052))
        joints["cuff." + side] = ((x * 0.2, 0.01, 0.9), (0.05, 0.05))
        links += [("chest", "shoulder." + side), ("shoulder." + side, "elbow." + side), ("elbow." + side, "cuff." + side)]
    obj = grow("Em coat", joints, links)
    # Open the bottom of the skirt (remove the cap the Skin modifier puts there), so the legs
    # come out of it like a real coat.
    mesh = obj.data
    import bmesh
    edit = bmesh.new()
    edit.from_mesh(mesh)
    cap = [face for face in edit.faces if face.calc_center_median().z < 0.64 and face.normal.z < -0.6]
    bmesh.ops.delete(edit, geom=cap, context="FACES")
    edit.to_mesh(mesh)
    edit.free()
    solid = obj.modifiers.new("Thickness", "SOLIDIFY")
    solid.thickness = 0.008
    apply_modifiers(obj)
    return obj


def scarf():
    """Her rust scarf: a soft roll wound round her neck, a knot at the front, and one end hanging
    down her chest."""
    bpy.ops.mesh.primitive_torus_add(major_radius=0.075, minor_radius=0.035, location=(0, -0.005, 1.385))
    roll = bpy.context.active_object
    roll.name = "scarf"
    roll.scale = (1.05, 0.95, 1.0)
    make_active(roll)
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.shade_smooth()
    knot = ball("knot", (-0.04, -0.075, 1.36), (0.035, 0.03, 0.04))
    end = grow("scarf end", {"top": ((-0.05, -0.1, 1.36), (0.035, 0.012)), "bottom": ((-0.065, -0.12, 1.16), (0.03, 0.01))}, [("top", "bottom")], 2)
    return [roll, knot, end]


def head():
    """Her head and face, gently stylised: a rounded skull and soft jaw, a small nose, full
    cheeks with a rosy flush, lips, and large blue-grey eyes with dark pupils and a bright catch
    of light, and small ears mostly hidden by her hair."""
    centre = Vector((0, -0.01, 1.6))
    skull = ball("skull", centre, (0.082, 0.094, 0.108))
    # Shape the jaw: narrow the lower half toward the chin, and bring the chin a little forward.
    for vertex in skull.data.vertices:  # The skull's own corners are measured from its middle.
        below = max(0.0, -vertex.co.z / 0.108)
        vertex.co.x *= 1 - 0.2 * below ** 1.5
        vertex.co.y -= 0.012 * below
    parts = [skull]
    parts.append(ball("nose", centre + Vector((0, -0.09, -0.016)), (0.0085, 0.012, 0.016)))
    for x in (-1, 1):
        parts.append(ball("ear", centre + Vector((x * 0.08, 0.005, -0.005)), (0.012, 0.02, 0.026)))
    parts.append(ball("upper lip", centre + Vector((0, -0.084, -0.052)), (0.017, 0.006, 0.0045)))
    parts.append(ball("lower lip", centre + Vector((0, -0.082, -0.06)), (0.015, 0.006, 0.005)))
    eyes = []
    for x in (-1, 1):
        eye_centre = centre + Vector((x * 0.031, -0.076, 0.01))
        eyes.append(("white", ball("eye white", eye_centre, (0.015, 0.008, 0.011))))
        eyes.append(("iris", ball("iris", eye_centre + Vector((0, -0.0055, -0.001)), (0.0085, 0.005, 0.0085))))
        eyes.append(("pupil", ball("pupil", eye_centre + Vector((0, -0.009, -0.001)), (0.004, 0.002, 0.004))))
        eyes.append(("lid", ball("eyelid", eye_centre + Vector((0, -0.001, 0.0065)), (0.0165, 0.0095, 0.0065))))
        eyes.append(("brow", ball("brow", eye_centre + Vector((x * 0.003, -0.003, 0.021)), (0.017, 0.0035, 0.0035))))
    return parts, eyes


def hair():
    """Her hair: honey-blonde, straight, parted in the middle, worn down past her shoulders. Made
    of many flat strands, each starting at the parting on top of her head, sweeping down over her
    head to the side and then falling straight, a little apart, so it moves and catches light
    like real hair. A smooth cap under the strands hides the scalp."""
    random.seed(7)
    centre = Vector((0, -0.01, 1.6))
    cap = ball("hair cap", centre + Vector((0, 0.008, 0.012)), (0.088, 0.1, 0.112))
    # Tuck the front of the cap under her skin, so her face shows and the hairline is the smooth
    # curve where the hair meets the skull (cutting it away would leave a jagged edge).
    for vertex in cap.data.vertices:
        if vertex.co.y < -0.03 and vertex.co.z < 0.075 + 0.3 * (vertex.co.y + 0.1):
            vertex.co *= 0.86

    # The long hair: one smooth curtain round the back and sides of her head, following her
    # skull at the top and then falling straight past her shoulders, its front edges hanging just
    # in front of them. Built as a grid of corners (round, and down), given thickness, and made
    # smooth: the clean, sculpted hair of a storybook film, not a mop of cords.
    import bmesh
    around_steps, down_steps = 40, 26
    top, bottom = 1.67, 1.24
    edit = bmesh.new()
    grid = []
    for down in range(down_steps + 1):
        share_down = down / down_steps
        z = top + (bottom - top) * share_down
        height = (z - centre.z) / 0.112
        on_skull = math.sqrt(max(0.0, 1 - min(1.0, height) ** 2))
        reach = max(0.09 * on_skull + 0.008, 0.088 + 0.04 * max(0.0, share_down - 0.4))
        row = []
        for around in range(around_steps + 1):
            angle = math.radians(-112 + 224 * around / around_steps)  # 0 is straight behind her.
            forward = -0.03 * max(0.0, abs(math.sin(angle)) - 0.85) / 0.15 * share_down
            spot = centre + Vector((math.sin(angle) * reach, math.cos(angle) * reach * 0.95 + 0.012 + forward, z - centre.z))
            row.append(edit.verts.new(spot))
        grid.append(row)
    for down in range(down_steps):
        for around in range(around_steps):
            edit.faces.new((grid[down][around], grid[down][around + 1], grid[down + 1][around + 1], grid[down + 1][around]))
    curtain_mesh = bpy.data.meshes.new("hair curtain")
    edit.to_mesh(curtain_mesh)
    edit.free()
    curtain = bpy.data.objects.new("hair curtain", curtain_mesh)
    bpy.context.collection.objects.link(curtain)
    thick = curtain.modifiers.new("Thickness", "SOLIDIFY")
    thick.thickness = 0.022
    thick.offset = 1.0
    smooth = curtain.modifiers.new("Smooth", "SUBSURF")
    smooth.levels = 2
    apply_modifiers(curtain)
    strands = [curtain]
    return cap, strands


def paint(obj, colour_at):
    """Gives every corner of an object a colour from `colour_at(position)`, in the colour layer
    "Col" the game's shader reads."""
    layer = obj.data.color_attributes.new(name="Col", type="BYTE_COLOR", domain="POINT")
    for vertex in obj.data.vertices:
        r, g, b = colour_at(vertex.co)
        layer.data[vertex.index].color_srgb = (r, g, b, 1.0)


def mix(a, b, share):
    """`share` of the way from colour `a` to colour `b`."""
    share = max(0.0, min(1.0, share))
    return tuple(a[i] + (b[i] - a[i]) * share for i in range(3))


def body_colour(position):
    """Under her coat: skin at the neck, jeans on her legs, boots on her feet."""
    if position.z > 1.36 and abs(position.x) < 0.075:
        return SKIN  # Her neck. Everything else above the waist is inside her coat.
    if position.z > 1.0:
        return COAT
    if position.z < 0.22:
        return BOOTS
    if abs(position.x) > 0.17 and position.z > 0.75:
        return SCARF  # Her mittens, in the scarf's rust.
    return JEANS


def coat_colour(position):
    """Her coat: teal, darker at the hem and cuffs, with a row of buttons down the front."""
    colour = COAT
    colour = mix(colour, COAT_DARK, (0.67 - position.z) / 0.05)
    colour = mix(colour, COAT_DARK, (abs(position.x) - 0.17) / 0.02 * (position.z < 0.95))
    for button_z in (0.82, 0.95, 1.08, 1.21):
        if position.y < -0.1 and abs(position.x - 0.03) < 0.012 and abs(position.z - button_z) < 0.012:
            colour = BUTTON
    return colour


def face_colour(position):
    """Her skin, with a soft rosy flush on each cheek fading out into it (the skull's corners are
    measured from the middle of her head)."""
    colour = SKIN
    for x in (-1, 1):
        cheek = Vector((x * 0.046, -0.07, -0.03))
        colour = mix(colour, CHEEKS, (0.032 - (position - cheek).length) / 0.032 * 0.7)
    return colour


def hair_colour(position):
    """Honey-blonde, a shade deeper toward the ends, with soft lighter and darker streaks running
    down it, like strands catching the light, and a fine line at the middle parting."""
    centre = Vector((0, -0.01, 1.6))
    colour = HAIR
    colour = mix(colour, HAIR_DEEP, (1.5 - position.z) / 0.25 * 0.4)
    angle = math.atan2(position.x - centre.x, position.y - centre.y)
    streak = math.sin(angle * 31) * 0.5 + math.sin(angle * 13 + 1.7) * 0.5
    colour = mix(colour, (1.0, 0.88, 0.6), max(0.0, streak) * 0.25)
    colour = mix(colour, HAIR_DEEP, max(0.0, -streak) * 0.3)
    if abs(position.x) < 0.004 and position.z > 1.7:
        colour = mix(colour, HAIR_DEEP, 0.6)
    return colour


def distance_to_segment(p, start, end):
    """How far a point is from the line between two points."""
    line = end - start
    share = max(0.0, min(1.0, (p - start).dot(line) / max(line.length_squared, 1e-9)))
    return (p - (start + line * share)).length


def bone_segment(name):
    """Where a bone runs, start to end."""
    if name in EXTRA_BONES:
        start, end = EXTRA_BONES[name]
        return Vector(start), Vector(end)
    for bone_name, start, end, _ in BONES:
        if bone_name == name:
            return point(start), point(end)
    raise KeyError(name)


def weigh(obj, bone_names, fixed=None):
    """Ties each corner to the nearest of `bone_names`, more to closer ones, so skin bends
    smoothly where bones meet. `fixed` ties every corner to one bone instead."""
    groups = {name: obj.vertex_groups.new(name=name) for name in ([fixed] if fixed else bone_names)}
    if fixed:
        groups[fixed].add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        return
    segments = {name: bone_segment(name) for name in bone_names}
    for vertex in obj.data.vertices:
        distances = sorted((distance_to_segment(vertex.co, *segments[name]), name) for name in segments)
        nearest = distances[0][0]
        chosen = [(d, n) for d, n in distances[:3] if d < nearest * 1.4 + 0.012]
        strengths = [(1.0 / max(d, 0.004) ** 4, n) for d, n in chosen]
        total = sum(s for s, _ in strengths)
        for strength, name in strengths:
            groups[name].add([vertex.index], strength / total, "REPLACE")


def weigh_hair(obj):
    """Hair near the scalp moves with her head; the falling part swings on the hair bone."""
    head_group = obj.vertex_groups.new(name="head")
    hair_group = obj.vertex_groups.new(name="hair")
    for vertex in obj.data.vertices:
        swing = max(0.0, min(1.0, (1.56 - vertex.co.z) / 0.18))
        head_group.add([vertex.index], 1 - swing, "REPLACE")
        hair_group.add([vertex.index], swing, "REPLACE")


def weigh_coat(obj):
    """The coat's body and sleeves follow her spine and arms; its skirt hangs from the coat-skirt
    bone (which swings on a spring in the game), a little from her thighs at the front."""
    arm_bones = [n for n in ("upper_arm.L", "forearm.L", "hand.L", "upper_arm.R", "forearm.R", "hand.R")]
    weigh(obj, ["spine", "chest", "neck"] + arm_bones + ["coat_skirt"])


def face_outward(obj):
    """Turns every face of an object to point outward. Unity only draws the outward side of a
    surface (it skips the inside, to save work), so an inward-facing patch would look like a hole."""
    make_active(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def join(objects, into):
    """Merges several objects into one (keeping their colours and bone ties)."""
    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    for obj in objects:
        obj.select_set(True)
    into.select_set(True)
    bpy.context.view_layer.objects.active = into
    bpy.ops.object.join()


def make_armature():
    """Her bones, named as the game expects."""
    data = bpy.data.armatures.new("em skeleton")
    rig = bpy.data.objects.new("Em rig", data)
    bpy.context.collection.objects.link(rig)
    make_active(rig)
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for name, _, _, parent in BONES:
        bone = data.edit_bones.new(name)
        bone.head, bone.tail = bone_segment(name)
        if parent:
            bone.parent = made[parent]
        made[name] = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def preview_material():
    """A material showing the mesh's own colours, for the check pictures."""
    material = bpy.data.materials.new("em")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    colours = nodes.new("ShaderNodeVertexColor")
    colours.layer_name = "Col"
    material.node_tree.links.new(colours.outputs["Color"], shader.inputs["Base Color"])
    shader.inputs["Roughness"].default_value = 0.75
    return material


def render_previews(folder):
    """Pictures of her from the front, three-quarter and side, and a close-up of her face."""
    os.makedirs(folder, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 700, 900
    world = bpy.data.worlds.new("night")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.08, 0.1, 0.16, 1)
    scene.world = world
    key = bpy.data.objects.new("key", bpy.data.lights.new("key", "SUN"))
    key.data.energy = 3.2
    key.rotation_euler = (math.radians(55), 0, math.radians(-30))
    fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", "AREA"))
    fill.data.energy = 80
    fill.data.color = (1.0, 0.8, 0.6)
    fill.location = (1.5, -2.0, 1.6)
    for light in (key, fill):
        scene.collection.objects.link(light)
    camera = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
    scene.collection.objects.link(camera)
    scene.camera = camera
    shots = (("front", 0, 0.9, 3.4, 40), ("three_quarter", 35, 0.9, 3.4, 40), ("side", 90, 0.9, 3.4, 40), ("face", 20, 1.58, 0.75, 40))
    for name, angle, height, distance, lens in shots:
        radians = math.radians(angle)
        target = Vector((0, 0, height))
        camera.location = target + Vector((-math.sin(radians) * distance, -math.cos(radians) * distance, 0.1))
        camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.lens = lens if name != "face" else 60
        scene.render.filepath = os.path.join(folder, "em_" + name + ".png")
        bpy.ops.render.render(write_still=True)


def main():
    """Builds her, checks her in pictures, and saves her for Unity."""
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out_path = arguments[0] if arguments else "game/Resources/Models/em.fbx"
    preview_folder = arguments[1] if len(arguments) > 1 else ""
    clear_scene()

    figure = body()
    paint(figure, body_colour)
    leg_and_body_bones = [name for name, start, _, _ in BONES if start]
    weigh(figure, leg_and_body_bones)

    garment = coat()
    paint(garment, coat_colour)
    weigh_coat(garment)

    wrap = scarf()
    for part in wrap:
        paint(part, lambda p: SCARF)
        if part.name.startswith("scarf end"):
            weigh(part, ["scarf_end"], fixed="scarf_end")
        else:
            weigh(part, ["neck", "chest"])

    face_parts, eyes = head()
    for part in face_parts:
        if part.name.startswith("skull"):
            paint(part, face_colour)
        else:
            colour = LIPS if "lip" in part.name else SKIN
            paint(part, lambda p, colour=colour: colour)
        weigh(part, ["head"], fixed="head")
    for kind, part in eyes:
        colour = {"white": EYE_WHITE, "iris": EYE_IRIS, "pupil": EYE_DARK, "brow": HAIR_DEEP, "lid": SKIN}[kind]
        paint(part, lambda p, colour=colour: colour)
        weigh(part, ["head"], fixed="head")

    cap, strands = hair()
    paint(cap, hair_colour)
    weigh(cap, ["head"], fixed="head")
    for strand in strands:
        paint(strand, hair_colour)
        weigh_hair(strand)

    for part in [figure, garment, cap] + wrap + strands:
        face_outward(part)
    join([garment] + wrap + face_parts + [p for _, p in eyes] + [cap] + strands, figure)
    rig = make_armature()
    figure.parent = rig
    skin = figure.modifiers.new("Skeleton", "ARMATURE")
    skin.object = rig
    figure.data.materials.append(preview_material())
    for polygon in figure.data.polygons:
        polygon.use_smooth = True
    print("Em:", len(figure.data.vertices), "corners,", len(rig.data.bones), "bones")

    if preview_folder:
        render_previews(preview_folder)

    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)
    for obj in bpy.context.view_layer.objects:
        obj.select_set(obj in (figure, rig))
    bpy.ops.export_scene.fbx(
        filepath=out_path, use_selection=True, object_types={"ARMATURE", "MESH"},
        add_leaf_bones=True, bake_anim=False, axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", colors_type="SRGB",
    )
    print("Saved", out_path)


main()
