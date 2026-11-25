// Purpose: proves the small decisions the Unity scene leans on: reading the chapters' colours,
// which floors, walls and props show at each moment, the rough box each prop fills, the ball in
// the fetch chapters (thrown, bouncing, rolling, carried, dropped), and Bodhi's errands (running
// where the story sends him, fetching the ball and bringing it back).
using System;
using System.Linq;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class StageLookTests
    {
        // Colours written the browser's ways all read; broken ones don't.
        [Test]
        public void colours_read_in_every_way_the_chapters_write_them()
        {
            Assert.That(StageLook.parse_color("#c8a27a", out var hex), Is.True);
            Assert.That(hex.red, Is.EqualTo(200 / 255.0).Within(1e-9));
            Assert.That(hex.green, Is.EqualTo(162 / 255.0).Within(1e-9));
            Assert.That(hex.blue, Is.EqualTo(122 / 255.0).Within(1e-9));
            Assert.That(hex.alpha, Is.EqualTo(1));
            Assert.That(StageLook.parse_color("#FFF", out var short_hex), Is.True);
            Assert.That(short_hex.red + short_hex.green + short_hex.blue, Is.EqualTo(3).Within(1e-9));
            Assert.That(StageLook.parse_color("rgb(30,30,45)", out var rgb), Is.True);
            Assert.That(rgb.blue, Is.EqualTo(45 / 255.0).Within(1e-9));
            Assert.That(rgb.alpha, Is.EqualTo(1));
            Assert.That(StageLook.parse_color(" rgba(255, 214, 170, 0.14) ", out var rgba), Is.True);
            Assert.That(rgba.red, Is.EqualTo(1).Within(1e-9));
            Assert.That(rgba.alpha, Is.EqualTo(0.14).Within(1e-9));
            Assert.That(StageLook.parse_color("rgba(300,-5,0,2)", out var wild), Is.True, "out-of-range numbers are kept in range");
            Assert.That(wild.red, Is.EqualTo(1));
            Assert.That(wild.green, Is.EqualTo(0));
            Assert.That(wild.alpha, Is.EqualTo(1));
        }

        // Break it: anything else is refused, never half-read.
        [Test]
        public void broken_colours_are_refused()
        {
            foreach (var text in new[] { null, "", "  ", "#12", "#1234567", "#ggg", "red", "rgb(1,2)", "rgb(1,2,3,4)", "rgba(1,2,3)", "rgb(1,2,3", "rgb(1,2,34", "rgb(a,2,3)", "rgb(NaN,2,3)", "rgb(Infinity,2,3)" })
            {
                Assert.That(StageLook.parse_color(text, out var color), Is.False, text ?? "null");
                Assert.That(color.alpha, Is.EqualTo(0), "left see-through");
            }
        }

        // A floor the story takes away goes; one that starts hidden comes only when it's added.
        [Test]
        public void floors_come_and_go_with_the_story()
        {
            var stage = ChapterStages.for_chapter("other_side");
            var run = new ChapterRun(StoryChapters.other_side());
            run.start();
            var gantry = stage.find_solid("gantry");
            Assert.That(gantry.hidden, Is.True);
            Assert.That(StageLook.solid_is_present(gantry, null), Is.False);
            Assert.That(StageLook.solid_is_present(gantry, run), Is.False);
            Assert.That(StageLook.solid_is_present(stage.solids[0], run), Is.True, "an ordinary floor is there");
            Assert.That(StageLook.solid_is_present(stage.solids[0], null), Is.True);
            Assert.That(StageLook.solid_is_present(null, run), Is.False);
            var hidden_without_id = new StageSolid { hidden = true };
            Assert.That(StageLook.solid_is_present(hidden_without_id, run), Is.False, "nothing can ever add it");
            ChapterRobot.finish(run, skip_films: true);
            Assert.That(StageLook.solid_is_present(gantry, run), Is.True, "the story put the gantry in place");
        }

        // Who each floor stops.
        [Test]
        public void a_gap_stops_only_her_and_a_low_gate_only_him()
        {
            var everyone = new StageSolid();
            var hers = new StageSolid { blocks = SolidBlocks.human };
            var his = new StageSolid { blocks = SolidBlocks.dog };
            Assert.That(StageLook.solid_blocks(everyone, Character.human) && StageLook.solid_blocks(everyone, Character.dog), Is.True);
            Assert.That(StageLook.solid_blocks(hers, Character.human), Is.True);
            Assert.That(StageLook.solid_blocks(hers, Character.dog), Is.False);
            Assert.That(StageLook.solid_blocks(his, Character.dog), Is.True);
            Assert.That(StageLook.solid_blocks(his, Character.human), Is.False);
            Assert.That(StageLook.solid_blocks(null, Character.human), Is.False);
        }

        // The carrier swaps from closed to open once the story moves on.
        [Test]
        public void props_swap_with_until_and_after()
        {
            var stage = ChapterStages.for_chapter("coming_home");
            var closed = stage.props.First(prop => prop.kind == "carrier" && prop.until.Length > 0);
            var open = stage.props.First(prop => prop.kind == "carrier" && prop.after != "");
            var run = new ChapterRun(StoryChapters.coming_home());
            run.start();
            Assert.That(StageLook.prop_is_showing(closed, run), Is.True);
            Assert.That(StageLook.prop_is_showing(open, run), Is.False);
            Assert.That(StageLook.prop_is_showing(closed, null), Is.True, "before the story starts");
            Assert.That(StageLook.prop_is_showing(open, null), Is.False);
            Assert.That(StageLook.prop_is_showing(null, run), Is.False);
            for (int step = 0; step < 80 && !run.has_happened(closed.until[0]); step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.has_happened(closed.until[0]), Is.True);
            Assert.That(StageLook.prop_is_showing(closed, run), Is.False);
            Assert.That(StageLook.prop_is_showing(open, run), Is.True);
        }

        // Floors take the chapter's ground colour, invisible walls aren't drawn, a solid's own
        // colour wins.
        [Test]
        public void floors_and_walls_take_their_colours()
        {
            var stage = ChapterStages.for_chapter("coming_home");
            Assert.That(StageLook.solid_color(new StageSolid { kind = "floor" }, stage), Is.EqualTo("#7a8a52"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "wall" }, stage), Is.EqualTo("#8a7a66"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "room_wall" }, stage), Is.EqualTo("#c9b79c"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "invisible", color = "#fff" }, stage), Is.EqualTo(""));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "plank", color = "#123456" }, stage), Is.EqualTo("#123456"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "plank" }, stage), Is.EqualTo("#6b5233"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "glass" }, stage), Does.StartWith("rgba"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "gate" }, stage), Is.EqualTo("#4a3b2a"));
            Assert.That(StageLook.solid_color(new StageSolid { kind = "floor" }, null), Is.EqualTo("#5a5a48"), "no stage: a plain ground colour");
            Assert.That(StageLook.solid_color(null, stage), Is.EqualTo(""));
            foreach (var each in ChapterStages.all())
            {
                foreach (var solid in each.solids)
                {
                    string color = StageLook.solid_color(solid, each);
                    Assert.That(color == "" || StageLook.parse_color(color, out _), Is.True, each.id + " " + solid.kind + " " + color);
                }
            }
        }

        // Props stand on the floor, hang from the ceiling or sit on a wall, as the chapters place them.
        [Test]
        public void props_stand_hang_or_sit_on_the_wall()
        {
            var tree = StageLook.prop_box(new StageProp { kind = "tree", x = 500, y = 520 });
            Assert.That(tree.left, Is.EqualTo(460));
            Assert.That(tree.top, Is.EqualTo(300));
            Assert.That(tree.width, Is.EqualTo(80));
            var sofa = StageLook.prop_box(new StageProp { kind = "sofa", x = 3080, y = 520 });
            Assert.That(sofa.left, Is.EqualTo(3080));
            Assert.That(sofa.top + sofa.height, Is.EqualTo(520), "stands on the floor");
            var lamp = StageLook.prop_box(new StageProp { kind = "lamp", x = 1500, y = 200, settings = new[] { StageSetting.of_flag("hanging", true) } });
            Assert.That(lamp.top, Is.EqualTo(200), "hangs from the ceiling");
            Assert.That(lamp.left, Is.EqualTo(1488));
            var standing_lamp = StageLook.prop_box(new StageProp { kind = "lamp", x = 100, y = 520 });
            Assert.That(standing_lamp.top + standing_lamp.height, Is.EqualTo(520));
            var window = StageLook.prop_box(new StageProp { kind = "window", x = 2200, y = 280, settings = new[] { StageSetting.of_number("w", 90), StageSetting.of_number("h", 100) } });
            Assert.That((window.left, window.top, window.width, window.height), Is.EqualTo((2200.0, 280.0, 90.0, 100.0)));
            var sign = StageLook.prop_box(new StageProp { kind = "sign", x = 10, y = 20, settings = new[] { StageSetting.of_text("text", "Exit") } });
            Assert.That(sign.width, Is.EqualTo(4 * 7 + 22));
            Assert.That(sign.top, Is.EqualTo(20));
            var unknown = StageLook.prop_box(new StageProp { kind = "something_new", x = 0, y = 100 });
            Assert.That((unknown.width, unknown.height, unknown.top), Is.EqualTo((60.0, 60.0, 40.0)));
            Assert.That(StageLook.prop_box(new StageProp { kind = "lightning" }).width, Is.EqualTo(0), "a flash, not a box");
            Assert.That(StageLook.prop_box(null).width, Is.EqualTo(0));
        }

        // Break it: sizes that aren't real numbers above zero are ignored.
        [Test]
        public void bad_sizes_fall_back_to_the_usual()
        {
            foreach (var bad in new[] { 0, -40, double.NaN, double.PositiveInfinity })
            {
                var box = StageLook.prop_box(new StageProp { kind = "sofa", settings = new[] { StageSetting.of_number("w", bad), StageSetting.of_number("h", bad) } });
                Assert.That((box.width, box.height), Is.EqualTo((160.0, 70.0)), bad.ToString());
            }
            var text_size = StageLook.prop_box(new StageProp { kind = "sofa", settings = new[] { StageSetting.of_text("w", "wide") } });
            Assert.That(text_size.width, Is.EqualTo(160));
        }

        // Each fetch chapter's ball starts inside the chapter, above a floor, never inside one.
        [Test]
        public void every_fetch_chapter_has_a_ball_to_find()
        {
            foreach (var script in StoryChapters.all().Where(each => each.has_ball))
            {
                var stage = ChapterStages.for_chapter(script.id);
                Assert.That(stage.ball_x, Is.GreaterThan(0).And.LessThan(stage.width), script.id);
                var ball = new BallFlight(stage.ball_x, stage.ball_y);
                Assert.That(stage.solids.Any(solid => StageLook.solid_is_present(solid, null) && ball.x > solid.x && ball.x < solid.x + solid.width
                    && ball.y > solid.y && ball.y < solid.y + solid.height), Is.False, script.id + ": not inside a floor");
                for (int step = 0; step < 120; step++)
                {
                    ball.step(1.0 / 60, stage, null, new BallHolderPlace());
                }
                Assert.That(ball.velocity_y, Is.EqualTo(0), script.id + ": it comes to rest on something");
                Assert.That(ball.y, Is.LessThan(stage.height), script.id);
            }
        }

        // Every chapter's prop colours read, so nothing on screen comes out see-through by mistake.
        [Test]
        public void every_prop_colour_in_every_chapter_reads()
        {
            foreach (var stage in ChapterStages.all())
            {
                foreach (var prop in stage.props)
                {
                    foreach (var setting in prop.settings.Where(each => each.kind == SettingKind.text && (each.name == "color" || each.name.EndsWith("_color") || each.name == "blanket")))
                    {
                        Assert.That(StageLook.parse_color(setting.text, out _), Is.True, stage.id + " " + prop.kind + " " + setting.name + " " + setting.text);
                    }
                }
                foreach (var setting in stage.scenery.Where(each => each.kind == SettingKind.text && (each.name.Contains("color") || each.name.StartsWith("sky") || each.name.StartsWith("ground") || each.name == "wall" || each.name == "room_wall" || each.name == "mist" || each.name == "horizon_glow" || each.name == "darkness_floor")))
                {
                    Assert.That(StageLook.parse_color(setting.text, out _), Is.True, stage.id + " scenery " + setting.name + " " + setting.text);
                }
            }
        }
    }

    public class BallFlightTests
    {
        const double frame = 1.0 / 60;

        // A flat floor at 520, and a wall at x 900 to 920.
        static ChapterStage yard()
        {
            return new ChapterStage
            {
                id = "test_yard",
                width = 1200,
                solids = new[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 1200, height = 80 },
                    new StageSolid { kind = "wall", x = 900, y = 300, width = 20, height = 220 },
                    new StageSolid { kind = "invisible", x = 600, y = 300, width = 10, height = 220, blocks = SolidBlocks.human },
                },
            };
        }

        static void fly(BallFlight ball, ChapterStage stage, double seconds)
        {
            for (double time = 0; time < seconds; time += frame)
            {
                ball.step(frame, stage, null, new BallHolderPlace());
            }
        }

        // She picks it up and throws it: up and away, then down, a bounce or two, a roll, a stop.
        [Test]
        public void thrown_it_bounces_rolls_and_stops_on_the_floor()
        {
            var stage = yard();
            var ball = new BallFlight(300, 515);
            Assert.That(ball.throw_ball(1), Is.False, "not in her hand yet");
            ball.pick_up();
            Assert.That(ball.held_by, Is.EqualTo(BallHolder.human));
            Assert.That(ball.throw_ball(1), Is.True);
            Assert.That(ball.held_by, Is.EqualTo(BallHolder.nobody));
            Assert.That(ball.velocity_x, Is.EqualTo(BallFlight.throw_speed_across));
            Assert.That(ball.velocity_y, Is.EqualTo(-BallFlight.throw_speed_up));
            double highest = ball.y;
            bool bounced = false;
            for (double time = 0; time < 4; time += frame)
            {
                double falling_before = ball.velocity_y;
                ball.step(frame, stage, null, new BallHolderPlace());
                highest = Math.Min(highest, ball.y);
                bounced |= falling_before > 0 && ball.velocity_y < 0;
            }
            Assert.That(highest, Is.LessThan(515 - 40), "it went up");
            Assert.That(bounced, Is.True);
            Assert.That(ball.y, Is.EqualTo(520 - BallFlight.radius).Within(1e-9), "resting on the floor");
            Assert.That(ball.velocity_y, Is.EqualTo(0));
            Assert.That(ball.x, Is.GreaterThan(610).And.LessThan(900 - BallFlight.radius), "the gap that stops only her didn't stop it, and it rolled to a stop before the wall");
        }

        // It comes back off a wall that blocks him at half speed; a gap only she can't pass lets it through.
        [Test]
        public void it_bounces_off_walls_that_stop_him()
        {
            var stage = yard();
            var ball = new BallFlight(880, 400);
            ball.pick_up();
            ball.throw_ball(1);
            ball.step(frame * 3, stage, null, new BallHolderPlace());
            Assert.That(ball.x, Is.LessThanOrEqualTo(900 - BallFlight.radius));
            Assert.That(ball.velocity_x, Is.LessThan(0), "coming back");
            Assert.That(ball.velocity_x, Is.EqualTo(-BallFlight.throw_speed_across * BallFlight.wall_keep).Within(1e-9));
            var through = new BallFlight(590, 400);
            through.pick_up();
            through.throw_ball(1);
            fly(through, stage, 0.1);
            Assert.That(through.x, Is.GreaterThan(610), "the gap that stops only her lets the ball through");
        }

        // He fetches it: only a loose ball, carried in his mouth (nearer for a puppy), dropped at
        // her feet, and then he waits for the next throw.
        [Test]
        public void he_fetches_carries_and_drops_it_at_her_feet()
        {
            var ball = new BallFlight(400, 515);
            Assert.That(ball.drop_at_her_feet(100, 520, 1), Is.False, "he hasn't got it");
            Assert.That(ball.dog_picks_up(), Is.True);
            Assert.That(ball.dog_picks_up(), Is.False, "already his");
            ball.step(frame, null, null, new BallHolderPlace { x = 400, top_y = 480, facing = -1, size_scale = 0.5 });
            Assert.That(ball.x, Is.EqualTo(400 - 15));
            Assert.That(ball.y, Is.EqualTo(486));
            Assert.That(ball.drop_at_her_feet(100, 520, -1), Is.True);
            Assert.That(ball.held_by, Is.EqualTo(BallHolder.nobody));
            Assert.That((ball.x, ball.y), Is.EqualTo((82.0, 514.0)));
            Assert.That(ball.waiting_for_throw, Is.True);
            Assert.That(ball.dog_picks_up(), Is.False, "he waits for her to throw it");
            ball.pick_up();
            ball.step(frame, null, null, new BallHolderPlace { x = 100, top_y = 434, facing = 1 });
            Assert.That((ball.x, ball.y), Is.EqualTo((112.0, 476.0)), "in her hand");
            ball.throw_ball(-1);
            Assert.That(ball.waiting_for_throw, Is.False);
            Assert.That(ball.velocity_x, Is.LessThan(0));
            ball.pick_up();
            ball.throw_ball(0);
            Assert.That(ball.velocity_x, Is.GreaterThan(0), "no facing counts as right");
        }

        // Break it: it never leaves the chapter, a stalled frame doesn't tunnel it through the
        // floor, and nonsense numbers don't break it.
        [Test]
        public void it_stays_in_the_chapter_whatever_happens()
        {
            var stage = yard();
            var ball = new BallFlight(20, 400);
            ball.pick_up();
            ball.throw_ball(-1);
            fly(ball, stage, 2);
            Assert.That(ball.x, Is.EqualTo(BallFlight.edge_margin));
            var dropped = new BallFlight(300, 300);
            dropped.step(10, stage, null, new BallHolderPlace());
            Assert.That(dropped.y, Is.LessThanOrEqualTo(520 - BallFlight.radius), "a long frame doesn't send it through the floor");
            stage.solids = stage.solids.Append(new StageSolid { kind = "plank", x = 0, y = 400, width = 200, height = 6 }).ToArray();
            var falling_fast = new BallFlight(100, -2000);
            for (int frame_number = 0; frame_number < 40; frame_number++)
            {
                falling_fast.step(0.25, stage, null, new BallHolderPlace());
            }
            Assert.That(falling_fast.y, Is.EqualTo(400 - BallFlight.radius).Within(1e-9), "falling at full speed through stalled frames, it still lands on a thin plank");
            var weird = new BallFlight(double.NaN, double.PositiveInfinity);
            Assert.That((weird.x, weird.y), Is.EqualTo((0.0, 0.0)));
            weird.step(double.NaN, stage, null, new BallHolderPlace());
            weird.step(-1, stage, null, new BallHolderPlace());
            weird.step(double.PositiveInfinity, stage, null, new BallHolderPlace());
            Assert.That((weird.x, weird.y), Is.EqualTo((0.0, 0.0)), "bad frame times move nothing");
            weird.dog_picks_up();
            weird.step(frame, stage, null, new BallHolderPlace { x = double.NaN, top_y = double.NegativeInfinity, size_scale = double.NaN });
            Assert.That(double.IsNaN(weird.x) || double.IsNaN(weird.y), Is.False);
            Assert.That(weird.x, Is.EqualTo(30), "a bad size counts as a grown dog");
            var no_stage = new BallFlight(100, 100);
            no_stage.step(0.5, null, null, new BallHolderPlace());
            Assert.That(no_stage.y, Is.GreaterThan(100), "with no stage it simply falls");
        }

        // Thrown under a low ceiling, it hits it and drops instead of passing through.
        [Test]
        public void a_low_ceiling_stops_it_rising()
        {
            var stage = yard();
            stage.solids = stage.solids.Append(new StageSolid { kind = "plank", x = 100, y = 440, width = 300, height = 10 }).ToArray();
            var ball = new BallFlight(150, 500);
            ball.pick_up();
            ball.throw_ball(1);
            double highest = ball.y;
            for (int step = 0; step < 30; step++)
            {
                ball.step(frame, stage, null, new BallHolderPlace());
                highest = Math.Min(highest, ball.y);
            }
            Assert.That(highest, Is.GreaterThanOrEqualTo(450 + BallFlight.radius - 1e-9), "never above the ceiling's underside");
        }

        // A plank that starts hidden isn't there until the story adds it: the ball falls past it.
        [Test]
        public void a_hidden_plank_lets_it_fall()
        {
            var stage = yard();
            stage.solids = stage.solids.Append(new StageSolid { kind = "plank", id = "shelf", x = 200, y = 300, width = 200, height = 12, hidden = true }).ToArray();
            var ball = new BallFlight(300, 280);
            fly(ball, stage, 2);
            Assert.That(ball.y, Is.EqualTo(520 - BallFlight.radius).Within(1e-9), "on the floor, not the plank");
            stage.solids[3].hidden = false;
            var on_shelf = new BallFlight(300, 280);
            fly(on_shelf, stage, 2);
            Assert.That(on_shelf.y, Is.EqualTo(300 - BallFlight.radius).Within(1e-9), "a plank that's there holds it");
        }
    }

    public class CompanionErrandTests
    {
        const double frame = 1.0 / 60;

        static CompanionView dog_at(double x, double leader_x = 500)
        {
            return new CompanionView
            {
                who = Character.dog, companion_x = x, companion_feet_y = 520, leader_x = leader_x, leader_facing = 1,
                follow = true, beat = ChapterBeat.human_section, closeness = 0.5, on_ground = true,
            };
        }

        // The story sends him running: faster than following, no trotting, and once there he sits
        // facing her and says he arrived.
        [Test]
        public void he_runs_where_the_story_sends_him_and_sits()
        {
            var steering = new CompanionSteering(new Random(1));
            var view = dog_at(100);
            view.run_to_x = 300;
            var order = steering.steer(view, frame);
            Assert.That(order.direction, Is.EqualTo(1));
            Assert.That(order.speed_scale, Is.EqualTo(1.1).Within(1e-9), "no trot, even close");
            view.companion_x = 250;
            Assert.That(steering.steer(view, frame).speed_scale, Is.EqualTo(1.1).Within(1e-9));
            Assert.That(steering.steer(view, frame).arrived, Is.False);
            view.companion_x = 290;
            view.leader_x = 50;
            var there = steering.steer(view, frame);
            Assert.That(there.arrived, Is.True);
            Assert.That(there.sit_seconds, Is.EqualTo(2));
            Assert.That(there.face, Is.EqualTo(-1), "looks back at her");
            view.run_to_x = double.NaN;
            view.companion_x = 100;
            view.leader_x = 500;
            Assert.That(steering.steer(view, frame).arrived, Is.False, "a nonsense spot is ignored: he follows her");
            Assert.That(steering.steer(view, frame).direction, Is.EqualTo(1));
        }

        // A reunion beats a story run; a story run beats fetch.
        [Test]
        public void a_reunion_beats_a_run_and_a_run_beats_fetch()
        {
            var view = dog_at(300, 500);
            view.run_to_x = 100;
            view.reunion_running = true;
            Assert.That(new CompanionSteering(new Random(1)).steer(view, frame).direction, Is.EqualTo(1), "to her, not to the spot");
            view.reunion_running = false;
            view.fetching = true;
            view.ball_x = 800;
            Assert.That(new CompanionSteering(new Random(1)).steer(view, frame).direction, Is.EqualTo(-1), "to the spot, not the ball");
        }

        // Fetch: he waits for a rolling ball to slow, goes to it, picks it up, brings it back and
        // drops it when he's near her, sitting proudly.
        [Test]
        public void he_waits_fetches_and_brings_it_back()
        {
            var steering = new CompanionSteering(new Random(1));
            var view = dog_at(100, 500);
            view.fetching = true;
            view.ball_x = 800;
            view.ball_y = 515;
            view.ball_speed_x = 200;
            Assert.That(steering.steer(view, frame).direction, Is.EqualTo(0), "watches it roll, rather than following her");
            view.companion_x = 500;
            view.ball_speed_x = 20;
            Assert.That(steering.steer(view, frame).direction, Is.EqualTo(1), "off after it");
            view.companion_x = 790;
            var at_ball = steering.steer(view, frame);
            Assert.That(at_ball.pick_up_ball, Is.True);
            view.ball_y = 400;
            Assert.That(steering.steer(view, frame).pick_up_ball, Is.False, "it's up on a shelf, out of reach");
            view.carrying_ball = true;
            var back = steering.steer(view, frame);
            Assert.That(back.direction, Is.EqualTo(-1));
            Assert.That(back.drop_ball, Is.False);
            view.companion_x = 530;
            var dropped = steering.steer(view, frame);
            Assert.That(dropped.drop_ball, Is.True);
            Assert.That(dropped.sit_seconds, Is.EqualTo(1.6));
            view.fetching = false;
            view.companion_x = 100;
            Assert.That(steering.steer(view, frame).drop_ball, Is.False, "not fetching: no drop, he simply follows");
        }
    }
}
