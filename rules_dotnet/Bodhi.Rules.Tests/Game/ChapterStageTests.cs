// Purpose: proves each chapter's stage (its floors, water, props and what each story rule does
// on screen) was exported whole and lines up with its story, and that StageTracker keeps the
// scene right: lasting changes (darkness, floods, who's hidden) survive a Continue, while the
// moment-only ones (a sound, a shake, a caption) play only when they really happen.
using System;
using System.Collections.Generic;
using System.Linq;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ChapterStageTests
    {
        static readonly SavedPlaces here = new SavedPlaces(300, 400, 340, 400);

        // Every chapter and its stage, side by side.
        static IEnumerable<(ChapterScript script, ChapterStage stage)> pairs()
        {
            var scripts = StoryChapters.all();
            var stages = ChapterStages.all();
            for (int index = 0; index < scripts.Length; index++)
            {
                yield return (scripts[index], stages[index]);
            }
        }

        // There is one stage for every chapter, in the same order.
        [Test]
        public void every_chapter_has_a_stage()
        {
            var script_ids = StoryChapters.all().Select(script => script.id).ToArray();
            var stage_ids = ChapterStages.all().Select(stage => stage.id).ToArray();
            Assert.That(stage_ids, Is.EqualTo(script_ids));
            Assert.That(stage_ids.Length, Is.EqualTo(12));
        }

        // A stage can be found by its chapter's id; an unknown id finds nothing.
        [Test]
        public void a_stage_is_found_by_its_chapter()
        {
            Assert.That(ChapterStages.for_chapter("home").id, Is.EqualTo("home"));
            Assert.That(ChapterStages.for_chapter("the_storm").id, Is.EqualTo("the_storm"));
            Assert.That(ChapterStages.for_chapter("nowhere"), Is.Null);
            Assert.That(ChapterStages.for_chapter(null), Is.Null);
        }

        // Rule 4's staging belongs to rule 4, and each thing's look to the same thing.
        [Test]
        public void staging_lines_up_with_the_story()
        {
            foreach (var (script, stage) in pairs())
            {
                Assert.That(stage.rules.Length, Is.EqualTo(script.rules.Length), script.id + " rules");
                Assert.That(stage.things.Select(look => look.event_name), Is.EqualTo(script.things.Select(thing => thing.event_name)), script.id + " things");
                Assert.That(stage.rules, Has.None.Null, script.id);
            }
        }

        // Every floor the story removes or adds is on the stage, so a gate never fails to open.
        [Test]
        public void every_solid_the_story_changes_exists()
        {
            foreach (var (script, stage) in pairs())
            {
                foreach (var rule in script.rules)
                {
                    foreach (var solid_id in (rule.remove ?? new string[0]).Concat(rule.add ?? new string[0]))
                    {
                        Assert.That(stage.find_solid(solid_id), Is.Not.Null, script.id + " changes a missing solid " + solid_id);
                    }
                }
            }
        }

        // A solid that starts missing is added by some rule, or it could never appear.
        [Test]
        public void every_hidden_solid_can_appear()
        {
            foreach (var (script, stage) in pairs())
            {
                var added = script.rules.SelectMany(rule => rule.add ?? new string[0]).ToHashSet();
                foreach (var solid in stage.solids.Where(solid => solid.hidden))
                {
                    Assert.That(added, Does.Contain(solid.id), script.id + " solid " + solid.id);
                }
            }
        }

        // Every flood the story raises is on the stage.
        [Test]
        public void every_water_the_story_changes_exists()
        {
            foreach (var (script, stage) in pairs())
            {
                foreach (var level in stage.rules.SelectMany(staging => staging.water))
                {
                    Assert.That(stage.find_water(level.water_id), Is.Not.Null, script.id + " " + level.water_id);
                }
            }
        }

        // She, Bodhi, the things and the zones are all inside the chapter, and floors are real boxes.
        [Test]
        public void everything_is_inside_the_chapter()
        {
            foreach (var (script, stage) in pairs())
            {
                Assert.That(stage.width, Is.GreaterThan(600), script.id);
                Assert.That(stage.height, Is.GreaterThan(300), script.id);
                Assert.That(stage.human_start_x, Is.InRange(0, stage.width), script.id);
                Assert.That(stage.dog_start_x, Is.InRange(0, stage.width), script.id);
                Assert.That(stage.human_start_feet_y, Is.InRange(0, stage.height), script.id);
                Assert.That(stage.dog_start_feet_y, Is.InRange(0, stage.height), script.id);
                foreach (var thing in script.things)
                {
                    Assert.That(thing.x, Is.InRange(0, stage.width), script.id + " " + thing.event_name);
                }
                foreach (var solid in stage.solids)
                {
                    Assert.That(solid.width, Is.GreaterThan(0), script.id + " solid");
                    Assert.That(solid.height, Is.GreaterThan(0), script.id + " solid");
                }
                Assert.That(stage.solids.Count(solid => solid.kind == "floor" && !solid.hidden), Is.GreaterThan(0), script.id + " has a floor");
            }
        }

        // A few values checked against levels.js by hand, so the exporter can't quietly swap fields.
        [Test]
        public void the_export_keeps_the_details()
        {
            var opening = ChapterStages.coming_home();
            Assert.That(opening.human_start_x, Is.EqualTo(130));
            Assert.That(opening.human_start_feet_y, Is.EqualTo(520));
            Assert.That(opening.dog_start_x, Is.EqualTo(404));
            Assert.That(opening.guest, Is.Not.Null);
            Assert.That(opening.guest.x, Is.EqualTo(1050));
            Assert.That(opening.guest.size, Is.EqualTo(0.86));
            Assert.That(opening.guest.pose, Is.EqualTo("sit"));
            Assert.That(opening.rules.Count(staging => staging.caption == "The first of May"), Is.EqualTo(1));
            Assert.That(opening.rules.Any(staging => staging.fade_in), Is.True);
            Assert.That(opening.rules.Any(staging => staging.hide.Any(hiding => hiding.who == StageWho.human && hiding.hidden)), Is.True);

            var storm = ChapterStages.the_storm();
            Assert.That(storm.find_water("flood"), Is.Not.Null);
            var flood_tops = storm.rules.SelectMany(staging => staging.water).Select(level => level.top).ToArray();
            Assert.That(flood_tops, Does.Contain(335.0).And.Contain(498.0));
            Assert.That(storm.rules.Any(staging => staging.effect != null && staging.effect.kind == "lightning" && !staging.effect.has_place), Is.True);
            Assert.That(storm.rules.Any(staging => staging.effect != null && staging.effect.kind == "debris" && staging.effect.width == 200), Is.True);
            Assert.That(storm.rules.Select(staging => staging.shake_seconds), Does.Contain(1.4));

            var snow = ChapterStages.first_snow();
            Assert.That(snow.rules.Any(staging => staging.carry == CarryChange.carry && staging.carry_item == "collar"), Is.True);
            Assert.That(snow.rules.Any(staging => staging.carry == CarryChange.drop), Is.True);

            var lessons = ChapterStages.lessons();
            Assert.That(lessons.rules.Select(staging => staging.dog_runs_to), Does.Contain((double?)1420));

            var station = ChapterStages.other_side();
            Assert.That(station.scenery_setting("art.far").text, Is.EqualTo("art/scenery/station_far.svg"));
            Assert.That(ChapterStages.home().scenery_setting("mid").kind, Is.EqualTo(SettingKind.nothing));
            Assert.That(station.aftermath.scene, Is.EqualTo("cabin_fire"));
        }

        // Scents, cameras and hidden things come through with their numbers.
        [Test]
        public void scent_camera_and_props_come_through()
        {
            bool saw_scent = false, saw_clear = false, saw_hold = false, saw_release = false;
            foreach (var (script, stage) in pairs())
            {
                foreach (var staging in stage.rules)
                {
                    saw_scent |= staging.scent == ScentChange.point && staging.scent_x > 0;
                    saw_clear |= staging.scent == ScentChange.clear;
                    saw_hold |= staging.camera == CameraChange.hold && staging.camera_zoom > 0;
                    saw_release |= staging.camera == CameraChange.release;
                }
                foreach (var prop in stage.props)
                {
                    Assert.That(prop.kind, Is.Not.Empty, script.id);
                    Assert.That(prop.until, Has.None.Null.And.None.Empty, script.id + " " + prop.kind);
                }
            }
            Assert.That(saw_scent && saw_clear && saw_hold && saw_release, Is.True);
            var doorframe = ChapterStages.coming_home().props.First(prop => prop.kind == "doorframe" && prop.settings.Any(setting => setting.name == "marks.0.1"));
            Assert.That(ChapterStage.find_setting(doorframe.settings, "marks.0.0").number, Is.EqualTo(18));
            Assert.That(ChapterStage.find_setting(doorframe.settings, "marks.0.1").text, Is.EqualTo("{dog}, 8 weeks"));
            var lamp = ChapterStages.all().SelectMany(stage => stage.props).First(prop => prop.kind == "lamp");
            Assert.That(ChapterStage.find_setting(lamp.settings, "radius").kind, Is.EqualTo(SettingKind.number));
        }

        // Break it: a setting that isn't there reads as "nothing", never as a crash.
        [Test]
        public void a_missing_setting_is_nothing()
        {
            Assert.That(ChapterStage.find_setting(null, "color").kind, Is.EqualTo(SettingKind.nothing));
            Assert.That(new ChapterStage().scenery_setting("rain").kind, Is.EqualTo(SettingKind.nothing));
            Assert.That(new ChapterStage().find_solid(null), Is.Null);
            Assert.That(new ChapterStage().find_water(""), Is.Null);
        }

        // ---------- StageTracker ----------

        // A small chapter: lights dim, a flood rises, she hides, and a film scene holds the camera.
        static ChapterScript test_script()
        {
            return new ChapterScript
            {
                id = "test_cellar",
                title = "Test Cellar",
                things = new[]
                {
                    new ChapterThing { event_name = "lamp_used", who = Character.human, x = 100, y = 300 },
                    new ChapterThing { event_name = "bed_used", who = Character.human, requires = new[] { "lamp_used" }, x = 300, y = 300 },
                },
                rules = new[]
                {
                    new StoryRule { on = "level_started", objective = "Look around." },                           // 0
                    new StoryRule { on = "lamp_used", objective = "Bed." },                                        // 1
                    new StoryRule { on = "lamp_used", unless = new[] { "level_started" } },                       // 2: never applies
                    new StoryRule { on = "bed_used", cutscene = CutsceneChange.start, cutscene_skip_to = "film_done" },  // 3
                    new StoryRule { on = "film_done", cutscene = CutsceneChange.end },                             // 4
                },
            };
        }

        static ChapterStage test_stage()
        {
            return new ChapterStage
            {
                id = "test_cellar",
                mood = "calm",
                scenery = new[] { StageSetting.of_number("darkness", 0.2) },
                guest = new StageGuest { mode = "still" },
                water = new[] { new StageWater { id = "flood", x = 0, y = 560, width = 800, height = 40 } },
                rules = new[]
                {
                    new RuleStaging { sound = "chime", caption = "Night" },
                    new RuleStaging { darkness = 0.7, mood = "tense", water = new[] { new StageWaterLevel { water_id = "flood", top = 400 }, new StageWaterLevel { water_id = "nowhere", top = 1 } },
                        carry = CarryChange.carry, carry_item = "glove", guest_mode = "follow", scent = ScentChange.point, scent_x = 500, scent_y = 280,
                        walk = new[] { new StageWalk { who = StageWho.human, x = 250 } }, shake_seconds = 1 },
                    new RuleStaging { darkness = 0, mood = "never" },
                    new RuleStaging { camera = CameraChange.hold, camera_x = 300, camera_y = 200, camera_zoom = 1.3, hide = new[] { new StageHiding { who = StageWho.human, hidden = true } },
                        walk = new[] { new StageWalk { who = StageWho.dog, x = 320 } } },
                    new RuleStaging { scent = ScentChange.clear, carry = CarryChange.drop },
                },
            };
        }

        static ChapterRun started_run()
        {
            var run = new ChapterRun(test_script());
            run.start();
            return run;
        }

        // Before anything happens, the scene is as the chapter starts it.
        [Test]
        public void the_tracker_starts_as_the_chapter_does()
        {
            var tracker = new StageTracker(test_stage(), new ChapterRun(test_script()));
            Assert.That(tracker.darkness, Is.EqualTo(0.2));
            Assert.That(tracker.mood, Is.EqualTo("calm"));
            Assert.That(tracker.guest_mode, Is.EqualTo("still"));
            Assert.That(tracker.water_top("flood"), Is.EqualTo(560));
            Assert.That(tracker.water_top("nowhere"), Is.Null);
            Assert.That(tracker.camera, Is.EqualTo(CameraChange.release));
            Assert.That(tracker.has_scent, Is.False);
            Assert.That(tracker.dog_carrying, Is.EqualTo(""));
            Assert.That(tracker.is_hidden(StageWho.human), Is.False);
            Assert.That(tracker.walk_target(StageWho.human), Is.Null);
        }

        // A rule that plays live changes the scene and is handed over for its sound and shake.
        [Test]
        public void a_live_rule_changes_the_scene_and_plays_its_moment()
        {
            var run = started_run();
            var tracker = new StageTracker(test_stage(), run);
            tracker.update();
            var first = tracker.take_live_staging();
            Assert.That(first.Select(staging => staging.sound), Is.EqualTo(new[] { "chime" }));
            Assert.That(tracker.take_live_staging(), Is.Empty, "handed over once");

            run.use("lamp_used", dog_is_near: false);
            tracker.update();
            Assert.That(tracker.darkness, Is.EqualTo(0.7));
            Assert.That(tracker.mood, Is.EqualTo("tense"));
            Assert.That(tracker.water_top("flood"), Is.EqualTo(400));
            Assert.That(tracker.water_top("nowhere"), Is.Null, "a flood the stage hasn't got is ignored");
            Assert.That(tracker.dog_carrying, Is.EqualTo("glove"));
            Assert.That(tracker.guest_mode, Is.EqualTo("follow"));
            Assert.That(tracker.has_scent, Is.True);
            Assert.That(tracker.scent_x, Is.EqualTo(500));
            Assert.That(tracker.scent_y, Is.EqualTo(280));
            Assert.That(tracker.walk_target(StageWho.human), Is.EqualTo(250));
            var second = tracker.take_live_staging();
            Assert.That(second.Count, Is.EqualTo(1), "the rule blocked by unless plays nothing");
            Assert.That(second[0].shake_seconds, Is.EqualTo(1));
        }

        // Continue: the replayed story sets the lasting scene, but no sound, caption or walk plays.
        [Test]
        public void a_continued_chapter_keeps_the_lasting_scene_only()
        {
            var run = started_run();
            run.use("lamp_used", dog_is_near: false);
            var resumed = ChapterRun.resume(run.chapter, run.to_save_text(here), out _);
            Assert.That(resumed, Is.Not.Null);
            var tracker = new StageTracker(test_stage(), resumed);
            tracker.update();
            Assert.That(tracker.take_live_staging(), Is.Empty);
            Assert.That(tracker.darkness, Is.EqualTo(0.7));
            Assert.That(tracker.water_top("flood"), Is.EqualTo(400));
            Assert.That(tracker.dog_carrying, Is.EqualTo("glove"));
            Assert.That(tracker.has_scent, Is.True);
            Assert.That(tracker.walk_target(StageWho.human), Is.Null, "the save already says where she stands");
            Assert.That(tracker.snap_water, Is.True, "a continued flood is already risen, not rising");
            tracker.water_snapped();
            Assert.That(tracker.snap_water, Is.False);
        }

        // A film scene holds the camera and hides her; skipping it lets the camera go and stops walks.
        [Test]
        public void ending_a_film_lets_the_camera_go()
        {
            var run = started_run();
            var tracker = new StageTracker(test_stage(), run);
            run.use("lamp_used", dog_is_near: false);
            run.use("bed_used", dog_is_near: false);
            tracker.update();
            Assert.That(tracker.camera, Is.EqualTo(CameraChange.hold));
            Assert.That(tracker.camera_x, Is.EqualTo(300));
            Assert.That(tracker.camera_zoom, Is.EqualTo(1.3));
            Assert.That(tracker.is_hidden(StageWho.human), Is.True);
            Assert.That(tracker.walk_target(StageWho.dog), Is.EqualTo(320));
            tracker.take_live_staging();

            Assert.That(run.skip_cutscene(), Is.True);
            tracker.update();
            Assert.That(tracker.camera, Is.EqualTo(CameraChange.release));
            Assert.That(tracker.walk_target(StageWho.dog), Is.Null, "a skipped film drops its walks");
            Assert.That(tracker.walk_target(StageWho.human), Is.Null);
            Assert.That(tracker.has_scent, Is.False);
            Assert.That(tracker.dog_carrying, Is.EqualTo(""));
            Assert.That(tracker.take_live_staging().Any(staging => staging.caption == ""), Is.True, "the film's caption is cleared");
        }

        // Reaching a walk's end lets go of it; moving someone by a cut stops her walk.
        [Test]
        public void walks_end_on_arrival_or_a_cut()
        {
            var run = started_run();
            var stage = test_stage();
            stage.rules[4].place = new[] { new StagePlacement { who = StageWho.human, x = 10, feet_y = 500 } };
            var tracker = new StageTracker(stage, run);
            run.use("lamp_used", dog_is_near: false);
            run.use("bed_used", dog_is_near: false);
            tracker.update();
            tracker.arrived(StageWho.dog);
            Assert.That(tracker.walk_target(StageWho.dog), Is.Null);
            Assert.That(tracker.walk_target(StageWho.human), Is.EqualTo(250));
            run.report_event("film_done");
            tracker.update();
            Assert.That(tracker.walk_target(StageWho.human), Is.Null);
        }

        // He runs to a spot on his own (dog_runs_to), and the walk counts for him like any other.
        [Test]
        public void dog_runs_to_is_his_walk()
        {
            var run = started_run();
            var stage = test_stage();
            stage.rules[1].dog_runs_to = 777;
            var tracker = new StageTracker(stage, run);
            run.use("lamp_used", dog_is_near: false);
            tracker.update();
            Assert.That(tracker.walk_target(StageWho.dog), Is.EqualTo(777));
        }

        // The wind starts as the chapter's scenery says, and a story moment can raise it (a
        // blizzard at midnight). Like the dark, it lasts: a Continue comes back to the same wind.
        [Test]
        public void the_wind_rises_with_the_story_and_survives_a_continue()
        {
            var stage = test_stage();
            stage.scenery = new[] { StageSetting.of_number("darkness", 0.2), StageSetting.of_number("wind", 0.25) };
            stage.rules[1].wind = 1.0;
            var run = started_run();
            var tracker = new StageTracker(stage, run);
            Assert.That(tracker.wind, Is.EqualTo(0.25));
            run.use("lamp_used", dog_is_near: false);
            tracker.update();
            Assert.That(tracker.wind, Is.EqualTo(1.0));
            var resumed = ChapterRun.resume(run.chapter, run.to_save_text(here), out _);
            var continued = new StageTracker(stage, resumed);
            continued.update();
            Assert.That(continued.wind, Is.EqualTo(1.0));
            Assert.That(new RuleStaging { wind = 0.5 }.is_empty, Is.False, "a wind change is a change");
        }

        // Break it: wind the data gets wrong is kept between still (0) and a blizzard (1).
        [Test]
        public void wind_is_kept_in_range()
        {
            foreach (var (written, expected) in new[] { (5.0, 1.0), (-2.0, 0.0), (double.NaN, 0.0), (double.PositiveInfinity, 1.0) })
            {
                var run = started_run();
                var stage = test_stage();
                stage.rules[1].wind = written;
                var tracker = new StageTracker(stage, run);
                run.use("lamp_used", dog_is_near: false);
                tracker.update();
                Assert.That(tracker.wind, Is.EqualTo(expected), written.ToString());
            }
        }

        // Break it: darkness the data gets wrong is kept between 0 and 0.95.
        [Test]
        public void darkness_is_kept_in_range()
        {
            foreach (var (written, expected) in new[] { (5.0, 0.95), (-2.0, 0.0), (double.NaN, 0.0), (double.PositiveInfinity, 0.95) })
            {
                var run = started_run();
                var stage = test_stage();
                stage.rules[1].darkness = written;
                var tracker = new StageTracker(stage, run);
                run.use("lamp_used", dog_is_near: false);
                tracker.update();
                Assert.That(tracker.darkness, Is.EqualTo(expected), written.ToString());
            }
            var bad_start = test_stage();
            bad_start.scenery = new[] { StageSetting.of_number("darkness", double.NaN) };
            Assert.That(new StageTracker(bad_start, new ChapterRun(test_script())).darkness, Is.EqualTo(0));
        }

        // Break it: a stage for another chapter, or a missing one, is refused at once.
        [Test]
        public void the_wrong_stage_is_refused()
        {
            var run = new ChapterRun(test_script());
            Assert.Throws<ArgumentNullException>(() => new StageTracker(null, run));
            Assert.Throws<ArgumentNullException>(() => new StageTracker(test_stage(), null));
            var other = test_stage();
            other.id = "home";
            Assert.Throws<ArgumentException>(() => new StageTracker(other, run));
        }

        // Break it: a stage with fewer stagings than rules (an old export) skips the extra rules
        // instead of crashing, and a null staging counts as nothing.
        [Test]
        public void a_short_stage_does_not_crash()
        {
            var run = started_run();
            var stage = test_stage();
            stage.rules = new[] { null, stage.rules[1] };
            var tracker = new StageTracker(stage, run);
            run.use("lamp_used", dog_is_near: false);
            run.use("bed_used", dog_is_near: false);
            Assert.DoesNotThrow(() => tracker.update());
            Assert.That(tracker.darkness, Is.EqualTo(0.7));
            Assert.That(tracker.camera, Is.EqualTo(CameraChange.release));
        }

        // Every real chapter can be played to the end with a tracker watching, without a crash,
        // and the tracker never holds the camera once the chapter is done.
        [Test]
        public void every_chapter_plays_with_a_tracker()
        {
            foreach (var (script, stage) in pairs())
            {
                var run = new ChapterRun(script);
                var tracker = new StageTracker(stage, run);
                run.start();
                ChapterRobot.finish(run, skip_films: true, after_each_step: _ => { tracker.update(); tracker.take_live_staging(); });
                tracker.update();
                Assert.That(run.is_complete, Is.True, script.id);
                Assert.That(tracker.darkness, Is.InRange(0, 0.95), script.id);
            }
        }

        // A film with no end of its own: Skip still lets the camera go and drops its walks. And a
        // rule that Skip itself sets off still gets to start a walk, because Skip drops the old
        // walks first, as the browser game does.
        [Test]
        public void skipping_a_film_with_no_end_rule()
        {
            foreach (bool skip_starts_a_walk in new[] { false, true })
            {
                var script = new ChapterScript
                {
                    id = "test_film",
                    rules = skip_starts_a_walk
                        ? new[] { new StoryRule { on = "level_started", cutscene = CutsceneChange.start }, new StoryRule { on = "cutscene_skipped" } }
                        : new[] { new StoryRule { on = "level_started", cutscene = CutsceneChange.start } },
                };
                var stage = new ChapterStage
                {
                    id = "test_film",
                    guest = new StageGuest { mode = null },
                    rules = new[]
                    {
                        new RuleStaging { camera = CameraChange.hold, camera_zoom = 2, walk = new[] { new StageWalk { who = StageWho.dog, x = 400 }, new StageWalk { who = StageWho.human, x = 300 } },
                            hide = new[] { new StageHiding { who = StageWho.guest, hidden = true }, new StageHiding { who = StageWho.human, hidden = true } } },
                        new RuleStaging { walk = new[] { new StageWalk { who = StageWho.human, x = 90 } }, guest_mode = "follow",
                            hide = new[] { new StageHiding { who = StageWho.human, hidden = false } } },
                    },
                };
                var run = new ChapterRun(script);
                var tracker = new StageTracker(stage, run);
                Assert.That(tracker.guest_mode, Is.EqualTo("still"), "a guest with no mode stays still");
                run.start();
                tracker.update();
                Assert.That(tracker.camera, Is.EqualTo(CameraChange.hold));
                Assert.That(tracker.is_hidden(StageWho.guest), Is.True);
                Assert.That(run.skip_cutscene(), Is.True);
                tracker.update();
                Assert.That(tracker.camera, Is.EqualTo(CameraChange.release), skip_starts_a_walk.ToString());
                Assert.That(tracker.walk_target(StageWho.dog), Is.Null, skip_starts_a_walk.ToString());
                if (skip_starts_a_walk)
                {
                    Assert.That(tracker.walk_target(StageWho.human), Is.EqualTo(90), "the walk Skip set off survives");
                    Assert.That(tracker.is_hidden(StageWho.guest), Is.False, "a guest called to follow comes back into view");
                    Assert.That(tracker.is_hidden(StageWho.human), Is.False);
                    Assert.That(tracker.guest_mode, Is.EqualTo("follow"));
                }
                else
                {
                    Assert.That(tracker.walk_target(StageWho.human), Is.Null);
                }
            }
        }

        // The run hands each applied rule over once, saying whether it was replayed.
        [Test]
        public void the_run_reports_applied_rules_once()
        {
            var run = started_run();
            run.use("lamp_used", dog_is_near: false);
            var applied = run.take_applied_rules();
            Assert.That(applied.Select(rule => rule.rule_number), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(applied.All(rule => !rule.replayed), Is.True);
            Assert.That(run.take_applied_rules(), Is.Empty);
            var resumed = ChapterRun.resume(run.chapter, run.to_save_text(here), out _);
            var replayed = resumed.take_applied_rules();
            Assert.That(replayed.Select(rule => rule.rule_number), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(replayed.All(rule => rule.replayed), Is.True);
        }
    }
}
