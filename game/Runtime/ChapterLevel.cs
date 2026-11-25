// Purpose: builds one chapter's place in the Unity scene from its exported stage
// (game/Logic/Chapters/): the sky, the floors and walls, the water, the furniture and the
// markers on things you can use. (She and Bodhi bump into the floors through CharacterMover, not
// Unity's physics, so nothing here needs a collider.) Each frame it
// keeps them in step with the story: a floor the story takes away goes, a flood rises, the open
// carrier replaces the closed one. Everything is a grey box until an artist paints it.
using System.Collections.Generic;
using Bodhi.Rules;
using UnityEngine;

namespace Bodhi.Game
{
    public class ChapterLevel
    {
        public const double water_ease = 0.8;  // A flood eases toward its new height, as in the browser game.

        // One floor or wall, and whether it was there last frame.
        class SolidPiece
        {
            public StageSolid solid;
            public GameObject piece;
            public bool present;
        }

        // One pool or flood, and the height its surface is drawn at now.
        class WaterPiece
        {
            public StageWater water;
            public SpriteRenderer drawing;
            public double bottom;       // Its floor never moves; only its surface does.
            public double drawn_top;
        }

        // One prop and its drawing.
        class PropPiece
        {
            public StageProp prop;
            public GameObject piece;
        }

        // One thing you can use and its marker.
        class ThingPiece
        {
            public ChapterThing thing;
            public GameObject marker;
        }

        readonly List<SolidPiece> solids = new List<SolidPiece>();
        readonly List<WaterPiece> waters = new List<WaterPiece>();
        readonly List<PropPiece> props = new List<PropPiece>();
        readonly List<ThingPiece> things = new List<ThingPiece>();

        public ChapterStage stage { get; }
        public LayoutSpace space { get; }
        public GameObject root { get; }

        // Builds the chapter under a new object in the scene.
        public ChapterLevel(ChapterStage stage, ChapterScript script)
        {
            this.stage = stage;
            space = new LayoutSpace(stage.height);
            root = new GameObject("Chapter " + stage.id);
            build_sky();
            foreach (var solid in stage.solids ?? new StageSolid[0])
            {
                if (solid != null)
                {
                    build_solid(solid);
                }
            }
            foreach (var water in stage.water ?? new StageWater[0])
            {
                if (water != null)
                {
                    build_water(water);
                }
            }
            foreach (var prop in stage.props ?? new StageProp[0])
            {
                if (prop != null)
                {
                    build_prop(prop);
                }
            }
            foreach (var thing in script.things ?? new ChapterThing[0])
            {
                if (thing != null)
                {
                    build_thing(thing);
                }
            }
        }

        // Keeps floors, props and markers in step with the story; raises or lowers the water
        // toward the tracker's heights (straight there after a Continue).
        public void refresh(ChapterRun run, StageTracker tracker, float delta_seconds)
        {
            foreach (var piece in solids)
            {
                bool present = StageLook.solid_is_present(piece.solid, run);
                if (present != piece.present)
                {
                    piece.present = present;
                    piece.piece.SetActive(present);
                }
            }
            foreach (var piece in props)
            {
                piece.piece.SetActive(StageLook.prop_is_showing(piece.prop, run));
            }
            foreach (var piece in things)
            {
                var thing = piece.thing;
                bool found = !thing.hidden || run.is_revealed(thing.reveal_name);
                piece.marker.SetActive(found && !run.has_happened(thing.event_name));
            }
            bool snap = tracker.snap_water;
            foreach (var piece in waters)
            {
                double? wanted = tracker.water_top(piece.water.id);
                double target = wanted ?? piece.water.y;
                double share = System.Math.Min(1, water_ease * delta_seconds);
                piece.drawn_top = snap ? target : piece.drawn_top + (target - piece.drawn_top) * share;
                place_water(piece);
            }
            if (snap)
            {
                tracker.water_snapped();
            }
        }

        // True if feet at (x, feet_y) in the layout are in water now (a little below its surface,
        // as in the browser game); says whether it's mud.
        public bool in_water(double x, double feet_y, out bool mud)
        {
            mud = false;
            foreach (var piece in waters)
            {
                var water = piece.water;
                if (x > water.x && x < water.x + water.width && feet_y > piece.drawn_top + 3)
                {
                    mud = water.mud;
                    return true;
                }
            }
            return false;
        }

        // Removes the chapter from the scene.
        public void destroy()
        {
            Object.Destroy(root);
        }

        // The sky behind everything: its top colour above, its bottom colour below.
        void build_sky()
        {
            var top = GreyBox.color_of(scenery_text("sky_top"), new Color(0.45f, 0.55f, 0.68f));
            var bottom = GreyBox.color_of(scenery_text("sky_bottom"), new Color(0.85f, 0.8f, 0.7f));
            double margin = 400;  // The camera can see a little past each end.
            GreyBox.make_layout_box(root.transform, "sky top", space, -margin, -margin, stage.width + margin * 2, stage.height / 2 + margin, top, GreyBox.sky_order);
            GreyBox.make_layout_box(root.transform, "sky bottom", space, -margin, stage.height / 2, stage.width + margin * 2, stage.height / 2 + margin, bottom, GreyBox.sky_order);
        }

        // A floor or wall: a drawing, unless it's an invisible one that only keeps them in.
        void build_solid(StageSolid solid)
        {
            string color_text = StageLook.solid_color(solid, stage);
            var corner = new Vector2((float)space.to_world_x(solid.x), (float)space.to_world_y(solid.y + solid.height));
            var size = new Vector2((float)(solid.width / LayoutSpace.units_per_metre), (float)(solid.height / LayoutSpace.units_per_metre));
            var piece = new GameObject("solid " + (string.IsNullOrEmpty(solid.id) ? solid.kind : solid.id));
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = new Vector3(corner.x, corner.y, 0);
            if (color_text != "")
            {
                GreyBox.make_box(piece.transform, "drawing", Vector2.zero, size, GreyBox.color_of(color_text, Color.gray), GreyBox.solid_order);
            }
            bool present = StageLook.solid_is_present(solid, null);
            piece.SetActive(present);
            solids.Add(new SolidPiece { solid = solid, piece = piece, present = present });
        }

        // A pool, river or flood: a see-through box from its surface down to its floor.
        void build_water(StageWater water)
        {
            var color = water.mud ? new Color(0.36f, 0.27f, 0.17f, 0.85f) : new Color(0.3f, 0.48f, 0.62f, 0.55f);
            var drawing = GreyBox.make_box(root.transform, "water " + water.id, Vector2.zero, Vector2.one, color, GreyBox.water_order);
            var piece = new WaterPiece { water = water, drawing = drawing, bottom = water.y + water.height, drawn_top = water.y };
            place_water(piece);
            waters.Add(piece);
        }

        // Puts a water box at its drawn surface.
        void place_water(WaterPiece piece)
        {
            double top = System.Math.Min(piece.drawn_top, piece.bottom);
            var corner = new Vector2((float)space.to_world_x(piece.water.x), (float)space.to_world_y(piece.bottom));
            float height = (float)((piece.bottom - top) / LayoutSpace.units_per_metre);
            piece.drawing.transform.localPosition = new Vector3(corner.x, corner.y, 0);
            piece.drawing.transform.localScale = new Vector3((float)(piece.water.width / LayoutSpace.units_per_metre), Mathf.Max(0.001f, height), 1);
        }

        // A prop as a box of its usual size, with a sign's words written on it.
        void build_prop(StageProp prop)
        {
            var box = StageLook.prop_box(prop);
            var piece = new GameObject("prop " + prop.kind);
            piece.transform.SetParent(root.transform, false);
            if (box.width > 0 && box.height > 0)
            {
                var color_setting = ChapterStage.find_setting(prop.settings, "color");
                var color = GreyBox.color_of(color_setting.kind == SettingKind.text ? color_setting.text : "", new Color(0.42f, 0.38f, 0.33f, 0.9f));
                bool in_front = ChapterStage.find_setting(prop.settings, "front").kind == SettingKind.flag
                    && ChapterStage.find_setting(prop.settings, "front").number != 0;
                int order = prop.kind == "room_wall" ? GreyBox.wall_order : in_front ? GreyBox.front_order : GreyBox.prop_order;
                GreyBox.make_layout_box(piece.transform, "drawing", space, box.left, box.top, box.width, box.height, color, order);
                var words = ChapterStage.find_setting(prop.settings, "text");
                if (words.kind == SettingKind.text && !string.IsNullOrEmpty(words.text))
                {
                    add_words(piece.transform, StoryText.personalize(words.text, ChapterDirector.dog_name), box);
                }
            }
            piece.SetActive(StageLook.prop_is_showing(prop, null));
            props.Add(new PropPiece { prop = prop, piece = piece });
        }

        // Words on a sign or a scrawl, so the puzzles that need them can be read.
        void add_words(Transform parent, string words, PropBox box)
        {
            var label = new GameObject("words");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3((float)space.to_world_x(box.left + box.width / 2), (float)space.to_world_y(box.top + box.height / 2), 0);
            var text = label.AddComponent<TextMesh>();
            text.text = words;
            text.anchor = TextAnchor.MiddleCenter;
            text.characterSize = 0.05f;
            text.fontSize = 48;
            text.color = new Color(0.92f, 0.89f, 0.83f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var drawing = label.GetComponent<MeshRenderer>();
            drawing.sharedMaterial = text.font.material;
            drawing.sortingOrder = GreyBox.front_order + 1;
        }

        // A small gold marker on a thing you can use, standing at its spot.
        void build_thing(ChapterThing thing)
        {
            var color = string.IsNullOrEmpty(thing.keepsake) ? new Color(0.95f, 0.8f, 0.35f, 0.9f) : new Color(0.95f, 0.55f, 0.7f, 0.9f);
            var marker = GreyBox.make_layout_box(root.transform, "thing " + thing.event_name, space, thing.x - 8, thing.y - 16, 16, 16, color, GreyBox.thing_order);
            marker.gameObject.SetActive(!thing.hidden);
            things.Add(new ThingPiece { thing = thing, marker = marker.gameObject });
        }

        // A scenery colour, or "".
        string scenery_text(string name)
        {
            var setting = stage.scenery_setting(name);
            return setting.kind == SettingKind.text ? setting.text : "";
        }
    }
}
