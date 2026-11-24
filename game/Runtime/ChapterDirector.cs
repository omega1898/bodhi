// Purpose: runs the whole game in one Unity scene. Put this one script on an empty GameObject
// and press Play: it makes the camera, the on-screen text and buttons, shows the title, and then
// plays every chapter in turn, building each one's place from its exported stage.
//
// Between and after chapters it shows home (HomeScreen), and it tells the session the date at
// start-up, on Continue, when the phone shows the game again and once a minute, so the home
// tasks roll over at midnight.
//
// It decides nothing about the story or about movement itself. Each frame it reads the buttons,
// asks the tested Logic code (GameSession, ChapterRun, CharacterMover, CompanionSteering,
// BallFlight, PromptPicker, StageTracker) what happens, and shows the answer. The order of a
// frame follows the browser game's update (prototype/web/js/game.js), so it plays the same.
using System.Collections.Generic;
using System.IO;
using Bodhi.Rules;
using UnityEngine;

namespace Bodhi.Game
{
    public class ChapterDirector : MonoBehaviour
    {
        public const string dog_name = "Bodhi";      // He. The name filled into {dog}.
        public const float longest_frame_seconds = 0.1f;  // A long hitch plays as a short one.
        public const double autopilot_arrived = 10;  // A story walk is over this close to its spot.
        public const double guest_set_off = 60, guest_stop = 24, guest_behind = 56, guest_speed_scale = 0.8;
        public const float date_check_seconds = 60;   // How often the date is checked while the game is open.

        static readonly Color her_color = new Color(0.78f, 0.58f, 0.46f);
        static readonly Color his_color = new Color(0.86f, 0.66f, 0.32f);
        static readonly Color guest_color = new Color(0.55f, 0.45f, 0.38f);

        GameSession session;
        PlayerControls controls;
        GameHud hud;
        TitleScreens titles;                         // The opening, the back story and the memory box.
        HomeScreen home_screen;                      // Home: the bond, the streak, today's tasks and fetch.
        float seconds_until_date_check = date_check_seconds;
        CameraRig camera_rig;
        GameAudio game_audio;
        bool started = false;                        // Past the title screen.
        bool paused = false;
        bool aftermath_showing = false;

        // The chapter on screen, and what was built for it.
        ChapterRun built_for;
        ChapterStage stage;
        ChapterLevel level;
        StageTracker tracker;
        CharacterBody her, him, guest;
        BallFlight ball;
        SpriteRenderer ball_drawing, scent_drawing;
        CompanionSteering steering;
        Prompt prompt;
        double guest_jump_rest = 0;
        bool guest_following = false;

        // Makes everything that lasts the whole game, then shows the title.
        void Awake()
        {
            // As many frames a second as the screen shows (120 on ProMotion iPhones), so movement
            // is as smooth as the screen allows. See FramePacing.
            Application.targetFrameRate = FramePacing.frames_per_second(Screen.currentResolution.refreshRateRatio.value);
            controls = new PlayerControls();
            camera_rig = new CameraRig(transform);
            hud = new GameHud(transform, controls);
            titles = new TitleScreens(transform);
            game_audio = gameObject.AddComponent<GameAudio>();
            session = new GameSession(new SaveFiles(Path.Combine(Application.persistentDataPath, "saves")), dog_name);
            home_screen = new HomeScreen(transform, session, dog_name);
            check_today();
            show_title();
        }

        // Turns the buttons off when the scene closes.
        void OnDestroy()
        {
            controls?.dispose();
        }

        // The phone hid the game: save now, if this is a moment that can be saved. The phone
        // showed it again: check the date, as a day may have passed.
        void OnApplicationPause(bool hiding)
        {
            if (hiding && session != null && her != null)
            {
                session.save_now(places_now(), both_on_ground());
            }
            if (!hiding)
            {
                check_today();
            }
        }

        // The game came back to the front (on a computer, its window was clicked): check the date.
        void OnApplicationFocus(bool has_focus)
        {
            if (has_focus)
            {
                check_today();
            }
        }

        // Tells the session today's date (the phone's own local date). A new day brings new home
        // tasks, so home is redrawn if it's showing.
        void check_today()
        {
            if (session == null || home_screen == null)
            {
                return;  // Not made yet.
            }
            var change = session.set_today(GameSession.day_number(System.DateTime.Now));
            if (change == DayChange.new_day_started && home_screen.showing)
            {
                home_screen.refresh();
            }
        }

        // The opening: the house, the door, the two of them, then New Game, Continue and the memory box.
        void show_title()
        {
            titles.show_opening(session.has_save, start_new_year, () => begin(true), show_memory_box, go_home_from_title);
        }

        // Home from the title: opens the saved year without starting a chapter, with Bodhi as
        // old as he is in the next chapter. Back returns to the title.
        void go_home_from_title()
        {
            session.open_home();
            var next = session.campaign.current_chapter;  // Null once the year is over: grown up.
            string dog_set = FrameSheets.dog_set_for_age(next?.dog_age);
            SpriteLibrary.prepare(new[] { "her", "bodhi", dog_set });  // Loaded now, before home shows.
            titles.hide();
            home_screen.show(dog_set, next != null ? next.dog_size : 1, "Back", () =>
            {
                home_screen.hide();
                show_title();
            });
        }

        // Home after a chapter's aftermath: Bodhi as he was in that chapter (already loaded), and
        // a button on to the next chapter.
        void go_home_after_chapter()
        {
            hud.hide_screens();
            session.open_home();
            var chapter = session.run.chapter;
            string leave_text = session.campaign.is_finished ? "Carry on" : "Next chapter";
            home_screen.show(FrameSheets.dog_set_for_age(chapter.dog_age), chapter.dog_size, leave_text, () =>
            {
                home_screen.hide();
                session.next_chapter();
            });
        }

        // Home after the year is over; Back goes to the title.
        void go_home_after_year()
        {
            hud.hide_screens();
            go_home_from_title();
        }

        // New Game tells the back story first, then starts the first chapter.
        void start_new_year()
        {
            titles.show_back_story(dog_name, () => begin(false));
        }

        // The memory box from the title: what the saved year holds, then back to the title.
        void show_memory_box()
        {
            titles.show_memory_box(MemoryBox.build(session.saved_year(), dog_name), show_title);
        }

        // Leaves the title for the game.
        void begin(bool continuing)
        {
            check_today();
            if (continuing)
            {
                session.continue_game();
            }
            else
            {
                session.new_game();
            }
            titles.hide();
            hud.hide_screens();
            started = true;
        }

        // One frame of the game.
        void Update()
        {
            float delta_seconds = Mathf.Min(Time.deltaTime, longest_frame_seconds);
            hud.update(delta_seconds);
            titles.update(delta_seconds);
            home_screen.update(delta_seconds);
            seconds_until_date_check -= delta_seconds;
            if (seconds_until_date_check <= 0)
            {
                check_today();
                seconds_until_date_check = date_check_seconds;
            }
            if (home_screen.showing)
            {
                controls.end_frame();  // Home has its own buttons; the chapter waits behind it.
                return;
            }
            if (!started)
            {
                controls.end_frame();
                return;
            }
            if (session.run == null)
            {
                show_year_end();
                controls.end_frame();
                return;
            }
            if (session.run != built_for)
            {
                build_chapter();
            }
            if (controls.pressed(GameButton.pause))
            {
                set_paused(!paused);
            }
            if (paused || session.chapter_finished)
            {
                if (session.chapter_finished && !aftermath_showing)
                {
                    show_aftermath();
                }
                controls.end_frame();
                return;
            }
            play_frame(delta_seconds);
            controls.end_frame();
        }

        // Stops or restarts the game behind the pause panel.
        void set_paused(bool now_paused)
        {
            paused = now_paused;
            hud.show_pause(paused, () => set_paused(false));
        }

        // Everything that happens in one frame of play, in the browser game's order.
        void play_frame(float delta_seconds)
        {
            var run = session.run;
            bool locked = run.controls_locked;
            bool as_her = run.controlled_character == Character.human;
            var player = as_her ? her : him;
            var companion = as_her ? him : her;

            // Buttons that tell the story something.
            if (controls.pressed(GameButton.skip))
            {
                session.skip_film();
            }
            if (!locked)
            {
                if (controls.pressed(GameButton.call) && run.call())
                {
                    play_sound(as_her ? "whistle" : "bark");
                }
                if (controls.pressed(GameButton.switch_character) && run.switch_character())
                {
                    play_sound("pickup");
                }
            }
            prompt = PromptPicker.pick(run, prompt_view(), dog_name);
            if (controls.pressed(GameButton.low))
            {
                press_low(run);
            }
            if (controls.pressed(GameButton.use) && !prompt.on_low_button)
            {
                press_use(run);
            }

            // The one you play: your buttons, or the story walking them somewhere.
            as_her = run.controlled_character == Character.human;  // A button above may have switched.
            player = as_her ? her : him;
            companion = as_her ? him : her;
            move_player(run, player, as_her ? StageWho.human : StageWho.dog, locked, delta_seconds);
            if (run.reunion_stage != ReunionStage.embrace)
            {
                move_companion(run, companion, delta_seconds);
                move_guest(delta_seconds);
            }

            // The ball, the places that set off the story, and looking after him.
            if (ball != null)
            {
                ball.step(delta_seconds, stage, run, ball_holder_place());
                ball_drawing.transform.localPosition = new Vector3((float)level.space.to_world_x(ball.x - BallFlight.radius), (float)level.space.to_world_y(ball.y + BallFlight.radius), 0);
            }
            report_zones(run);
            bool dog_wet = level.in_water(him.mover.center_x, him.mover.feet_y, out bool mud);
            run.care.update(delta_seconds, dog_wet ? (mud ? Surface.mud : Surface.water) : Surface.dry_ground, run.chapter.dog_size, run.cutscene_playing);

            // The story moves on, then the scene catches up with it.
            session.tick(delta_seconds, places_now(), both_on_ground());
            if (session.run != run)
            {
                return;  // Defensive: the chapter changed under us; rebuild next frame.
            }
            tracker.update();
            foreach (var staging in tracker.take_live_staging())
            {
                stage_moment(staging);
            }
            keep_lasting_staging();
            level.refresh(run, tracker, delta_seconds);

            // What's on screen.
            hud.set_subtitle(session.subtitles.showing, session.subtitles.text, session.subtitles.voice);
            hud.set_objective(run.controls_locked ? "" : StoryText.personalize(run.objective, dog_name));
            var spot = new Vector3((float)level.space.to_world_x(prompt.x), (float)level.space.to_world_y(prompt.y), 0);
            hud.set_prompt(prompt.action == PromptAction.none ? "" : prompt.text, spot, camera_rig.view);
            var followed = run.controlled_character == Character.human ? her : him;
            camera_rig.follow(delta_seconds, level.space, stage, tracker, followed.mover.center_x, followed.mover.feet_y, followed.facing);
            play_music(tracker.mood);
        }

        // The low button: the reunion's kneel, else she kneels (or stands back up), or he sniffs.
        void press_low(ChapterRun run)
        {
            if (prompt.on_low_button)
            {
                if (PromptPicker.act(run, prompt, prompt_view()) && prompt.action == PromptAction.kneel)
                {
                    her.strike_pose("kneel", 0);
                }
                return;
            }
            if (run.controls_locked)
            {
                return;
            }
            if (run.controlled_character == Character.dog)
            {
                if (run.sniff(him.mover.center_x, him.mover.feet_y))
                {
                    him.strike_pose("sniff", 0.8);
                    play_sound("sniff");
                }
            }
            else if (her.mover.on_ground)
            {
                if (her.pose == "kneel")
                {
                    her.end_pose();
                }
                else
                {
                    her.strike_pose("kneel", 0);
                    if (PromptPicker.dog_is_near(prompt_view(), her.mover.center_x) && run.kneel(true))
                    {
                        him.strike_pose("lean", 1.4);
                        show_bond(session.record_care(BondCareKind.pet));  // He leaned in: a cuddle.
                    }
                }
            }
        }

        // The use button: whatever the prompt offers, with the scene's half of it (the ball's
        // flight, a sound, a pose).
        void press_use(ChapterRun run)
        {
            var view = prompt_view();
            if (!PromptPicker.act(run, prompt, view))
            {
                return;
            }
            if (PromptPicker.bond_care_for(prompt.action, out BondCareKind care_kind))
            {
                show_bond(session.record_care(care_kind));
            }
            switch (prompt.action)
            {
                case PromptAction.throw_ball:
                    ball.throw_ball(her.facing);
                    break;
                case PromptAction.pick_up_ball:
                    ball.pick_up();
                    play_sound("pickup");
                    break;
                case PromptAction.hold:
                    her.strike_pose("kneel", 0);
                    play_sound("heartbeat");
                    break;
                case PromptAction.pet:
                    her.strike_pose("kneel", 0);
                    her.face(him.mover.center_x < her.mover.center_x ? -1 : 1);
                    him.strike_pose("lean", 1.6);
                    break;
                case PromptAction.lean:
                    him.strike_pose("lean", 1.6);
                    break;
                case PromptAction.feed:
                    play_sound("eat");
                    break;
                case PromptAction.towel:
                    play_sound("towel");
                    break;
                case PromptAction.use_thing:
                    var look = thing_look(prompt.thing_event);
                    if (look != null)
                    {
                        play_sound(look.sound);
                        if (!string.IsNullOrEmpty(look.pose))
                        {
                            (run.controlled_character == Character.human ? her : him).strike_pose(look.pose, 0.8);
                        }
                    }
                    break;
            }
        }

        // Moves the one you play: your buttons, unless the story is walking them somewhere.
        void move_player(ChapterRun run, CharacterBody player, StageWho who, bool locked, float delta_seconds)
        {
            int direction = locked ? 0 : controls.direction;
            bool jump_held = !locked && controls.held(GameButton.jump);
            if (!locked && controls.pressed(GameButton.jump))
            {
                if (player.pose == "kneel" || player.pose == "sit")
                {
                    player.end_pose();
                }
                player.mover.motion.press_jump();
            }
            double? target = tracker.walk_target(who);
            if (target != null)
            {
                direction = walk_toward(player, target.Value, who);
                jump_held = true;
            }
            if (run.reunion_stage == ReunionStage.embrace)
            {
                direction = 0;
            }
            bool wet = level.in_water(player.mover.center_x, player.mover.feet_y, out _);
            double speed = player.mover.who == Character.dog ? run.care.speed_scale() : 1;
            player.move(direction, jump_held, wet, speed, delta_seconds, stage, run);
        }

        // Steers someone toward a story spot; jumps when a wall's in the way; ends the walk there.
        int walk_toward(CharacterBody body, double target_x, StageWho who)
        {
            double distance = target_x - body.mover.center_x;
            if (System.Math.Abs(distance) <= autopilot_arrived)
            {
                tracker.arrived(who);
                return 0;
            }
            if (body.mover.blocked && body.mover.on_ground)
            {
                body.mover.motion.press_jump();
            }
            return distance < 0 ? -1 : 1;
        }

        // Moves the one you aren't playing, as CompanionSteering says; she follows a story walk too.
        void move_companion(ChapterRun run, CharacterBody companion, float delta_seconds)
        {
            var leader = companion == her ? him : her;
            bool is_dog = companion == him;
            double? her_walk = is_dog ? null : tracker.walk_target(StageWho.human);
            bool wet = level.in_water(companion.mover.center_x, companion.mover.feet_y, out _);
            if (her_walk != null)
            {
                int walk = walk_toward(companion, her_walk.Value, StageWho.human);
                companion.move(walk, true, wet, 1, delta_seconds, stage, run);
                return;
            }
            bool fetching = is_dog && ball != null && run.fetch_enabled && ball.held_by != BallHolder.human && !ball.waiting_for_throw;
            var view = new CompanionView
            {
                who = companion.mover.who,
                companion_x = companion.mover.center_x,
                companion_speed = companion.mover.motion.velocity_x,
                companion_feet_y = companion.mover.feet_y,
                leader_x = leader.mover.center_x,
                leader_speed = leader.mover.motion.velocity_x,
                leader_facing = leader.facing,
                follow = run.dog_follows,
                reunion_running = run.reunion_stage == ReunionStage.approach,
                beat = run.current_beat,
                closeness = run.bond.closeness_need,
                fear = run.bond.fear,
                care_speed_scale = run.care.speed_scale(),
                jump_scale = is_dog ? run.chapter.dog_jump : 1,
                on_ground = companion.mover.on_ground,
                blocked = companion.mover.blocked,
                obstacle_height = companion.mover.obstacle_height,
                run_to_x = is_dog ? tracker.walk_target(StageWho.dog) : null,
                fetching = fetching,
                carrying_ball = fetching && ball.held_by == BallHolder.dog,
                ball_x = ball != null ? ball.x : 0,
                ball_y = ball != null ? ball.y : 0,
                ball_speed_x = ball != null ? ball.velocity_x : 0,
            };
            var order = steering.steer(view, delta_seconds);
            if (order.wants_jump)
            {
                companion.mover.motion.press_jump();
            }
            if (order.sit_seconds > 0)
            {
                companion.strike_pose("sit", order.sit_seconds);
            }
            if (order.face != 0)
            {
                companion.face(order.face);
            }
            if (order.arrived)
            {
                tracker.arrived(StageWho.dog);
            }
            if (order.pick_up_ball && ball.dog_picks_up())
            {
                play_sound("pickup");
            }
            if (order.drop_ball && ball.drop_at_her_feet(her.mover.center_x, her.mover.feet_y, her.facing))
            {
                run.report_event("ball_fetched");
            }
            companion.move(order.direction, true, wet, order.speed_scale, delta_seconds, stage, run);
        }

        // The visiting dog: stays put, follows a little behind Bodhi at an old dog's pace, or has gone home.
        void move_guest(float delta_seconds)
        {
            if (guest == null)
            {
                return;
            }
            bool gone = tracker.guest_mode == "gone" || tracker.is_hidden(StageWho.guest);
            guest.hide(gone);
            if (gone)
            {
                return;
            }
            int direction = 0;
            guest_jump_rest = System.Math.Max(0, guest_jump_rest - delta_seconds);
            if (tracker.guest_mode == "follow")
            {
                double distance = him.mover.center_x - him.facing * guest_behind - guest.mover.center_x;
                if (System.Math.Abs(distance) > guest_set_off)
                {
                    guest_following = true;
                }
                else if (System.Math.Abs(distance) < guest_stop)
                {
                    guest_following = false;
                }
                if (guest_following)
                {
                    direction = distance < 0 ? -1 : 1;
                    guest.end_pose();
                }
                if (direction != 0 && guest.mover.blocked && guest.mover.on_ground && guest_jump_rest <= 0)
                {
                    guest.mover.motion.press_jump();
                    guest_jump_rest = 0.8;
                }
            }
            guest.move(direction, true, false, guest_speed_scale, delta_seconds, stage, session.run);
        }

        // Tells the story who is standing in each of its areas (the middle of them counts).
        void report_zones(ChapterRun run)
        {
            foreach (var zone in run.chapter.zones ?? new ChapterZone[0])
            {
                if (zone == null || run.has_happened(zone.event_name))
                {
                    continue;
                }
                run.report_zone(zone.event_name, is_inside(her, zone), is_inside(him, zone));
            }
        }

        // True if the middle of this one's box is in the area.
        static bool is_inside(CharacterBody body, ChapterZone zone)
        {
            double middle_x = body.mover.center_x;
            double middle_y = body.mover.top + body.mover.height / 2;
            return middle_x >= zone.x && middle_x <= zone.x + zone.width && middle_y >= zone.y && middle_y <= zone.y + zone.height;
        }

        // A story rule's moment-only staging: people put, turned and posed, a sound, a shake,
        // a caption, a fade, an effect.
        void stage_moment(RuleStaging staging)
        {
            foreach (var placing in staging.place ?? new StagePlacement[0])
            {
                body_of(placing.who)?.place(placing.x, placing.feet_y);
            }
            foreach (var facing in staging.face ?? new StageFacing[0])
            {
                body_of(facing.who)?.face(facing.direction);
            }
            foreach (var posing in staging.pose ?? new StagePose[0])
            {
                body_of(posing.who)?.strike_pose(posing.pose, posing.seconds);
            }
            play_sound(staging.sound);
            camera_rig.shake(staging.shake_seconds);
            if (staging.caption != null)
            {
                hud.set_caption(StoryText.personalize(staging.caption, dog_name));
            }
            if (staging.fade_in)
            {
                hud.fade_from_black();
            }
            if (staging.effect != null && staging.effect.kind == "lightning")
            {
                hud.lightning();
                play_sound("thunder");
            }
        }

        // What stays true until the story changes it: who's hidden, the dark, what he carries,
        // the scent trail.
        void keep_lasting_staging()
        {
            her.hide(tracker.is_hidden(StageWho.human));
            him.hide(tracker.is_hidden(StageWho.dog));
            him.carry(ball != null && ball.held_by == BallHolder.dog ? "ball" : tracker.dog_carrying);
            hud.set_darkness(tracker.darkness, GreyBox.color_of(scenery_text("darkness_color"), new Color(0.03f, 0.04f, 0.09f)));
            scent_drawing.enabled = tracker.has_scent && session.run.controlled_character == Character.dog;
            if (scent_drawing.enabled)
            {
                scent_drawing.transform.localPosition = new Vector3((float)level.space.to_world_x(tracker.scent_x - 6), (float)level.space.to_world_y(tracker.scent_y + 6), 0);
            }
        }

        // Builds the chapter on screen for the run the session now plays, and puts everyone at
        // their start (or where a Continue left them).
        void build_chapter()
        {
            level?.destroy();
            aftermath_showing = false;
            hud.hide_screens();
            hud.set_caption("");
            var run = session.run;
            built_for = run;
            stage = ChapterStages.for_chapter(run.chapter.id);
            game_audio.prepare(stage.all_moods());  // Made now, while loading, not mid-chapter.
            var weather = stage.weather_sound();
            game_audio.set_weather((float)weather.rain, (float)weather.wind);
            level = new ChapterLevel(stage, run.chapter);
            tracker = new StageTracker(stage, run);
            steering = new CompanionSteering(new System.Random());
            var root = level.root.transform;
            string dog_set = FrameSheets.dog_set_for_age(run.chapter.dog_age);
            SpriteLibrary.prepare(new[] { "her", dog_set, stage.guest != null ? "biscuit" : null });  // Loaded now, not mid-chapter.
            her = CharacterBody.create("her", Character.human, "her", root, level.space, stage.human_start_x, stage.human_start_feet_y, 1, 1, her_color);
            him = CharacterBody.create(dog_name, Character.dog, dog_set, root, level.space, stage.dog_start_x, stage.dog_start_feet_y, run.chapter.dog_size, run.chapter.dog_jump, his_color);
            guest = null;
            if (stage.guest != null)
            {
                guest = CharacterBody.create("guest", Character.dog, "biscuit", root, level.space, stage.guest.x, stage.guest.feet_y, stage.guest.size, 1, guest_color);
                if (!string.IsNullOrEmpty(stage.guest.pose))
                {
                    guest.strike_pose(stage.guest.pose, 0);
                }
            }
            if (session.restored_places != null)
            {
                var places = session.restored_places.Value;
                her.place(places.human_x, places.human_feet_y);
                him.place(places.dog_x, places.dog_feet_y);
            }
            ball = run.chapter.has_ball ? new BallFlight(stage.ball_x, stage.ball_y) : null;
            ball_drawing = GreyBox.make_layout_box(root, "ball", level.space, 0, 0, BallFlight.radius * 2, BallFlight.radius * 2, new Color(0.85f, 0.25f, 0.2f), GreyBox.character_order + 3);
            ball_drawing.enabled = ball != null;
            scent_drawing = GreyBox.make_layout_box(root, "scent", level.space, 0, 0, 12, 12, new Color(0.7f, 0.9f, 0.6f, 0.6f), GreyBox.thing_order);
            scent_drawing.enabled = false;
            tracker.update();          // A Continue replays the save: put the floods, the dark and the camera right.
            tracker.take_live_staging();
            level.refresh(run, tracker, 0);
            var start = run.controlled_character == Character.human ? her : him;
            camera_rig.jump_to(level.space, stage, start.mover.center_x, start.mover.feet_y);
        }

        // The quiet screen after a chapter: its lines, then Next starts the next one.
        void show_aftermath()
        {
            aftermath_showing = true;
            var chapter = session.run.chapter;
            var lines = new List<string> { chapter.title, "" };
            foreach (var line in chapter.aftermath ?? new string[0])
            {
                lines.Add(StoryText.personalize(line, dog_name));
            }
            var bond = session.campaign.last_chapter_bond;  // What this chapter added to the bond.
            if (BondFeedback.heart_pop(bond) != "")
            {
                lines.Add("");
                lines.Add(BondFeedback.heart_pop(bond));
                lines.AddRange(BondFeedback.lines_for(bond, dog_name));
            }
            hud.set_subtitle(false, "", StoryVoice.her);
            hud.set_prompt("", Vector3.zero, null);
            hud.show_aftermath(string.Join("\n", lines), () => session.next_chapter(), go_home_after_chapter);
            play_music(stage.aftermath.mood);
            game_audio.set_weather((float)stage.aftermath.rain, 0);
        }

        // The end of the year: what they kept.
        void show_year_end()
        {
            level?.destroy();
            level = null;
            built_for = null;
            var campaign = session.campaign;
            hud.show_year_end(StoryText.her_name + " and " + dog_name + "'s first year.\n\nKeepsakes found: " + campaign.keepsakes_found + " of " + campaign.keepsake_total, go_home_after_year);
            started = false;
        }

        // The heart pop and any lines worth celebrating when care in a chapter grows the bond.
        void show_bond(BondChange change)
        {
            hud.show_bond_note(BondFeedback.heart_pop(change), string.Join("\n", BondFeedback.lines_for(change, dog_name)));
        }

        // Where the two of them stand, for a save.
        SavedPlaces places_now()
        {
            return new SavedPlaces(her.mover.center_x, her.mover.feet_y, him.mover.center_x, him.mover.feet_y);
        }

        // Both on the ground: a safe moment to save.
        bool both_on_ground()
        {
            return her.mover.on_ground && him.mover.on_ground;
        }

        // Where the two of them (and the ball) are, for the prompt.
        PromptView prompt_view()
        {
            return new PromptView
            {
                human_x = her.mover.center_x, human_feet_y = her.mover.feet_y, human_height = her.mover.height,
                dog_x = him.mover.center_x, dog_feet_y = him.mover.feet_y, dog_height = him.mover.height,
                human_kneeling = her.pose == "kneel",
                ball_exists = ball != null,
                ball_held_by_human = ball != null && ball.held_by == BallHolder.human,
                ball_held_by_dog = ball != null && ball.held_by == BallHolder.dog,
                ball_x = ball != null ? ball.x : 0,
                ball_y = ball != null ? ball.y : 0,
            };
        }

        // Where a held ball goes: her hand or his mouth.
        BallHolderPlace ball_holder_place()
        {
            var holder = ball.held_by == BallHolder.dog ? him : her;
            return new BallHolderPlace { x = holder.mover.center_x, top_y = holder.mover.top, facing = holder.facing, size_scale = holder.mover.size_scale };
        }

        // Her, him or the visiting dog.
        CharacterBody body_of(StageWho who)
        {
            return who == StageWho.human ? her : who == StageWho.dog ? him : guest;
        }

        // How a thing you can use looks and sounds, or null.
        StageThingLook thing_look(string event_name)
        {
            foreach (var look in stage.things ?? new StageThingLook[0])
            {
                if (look != null && look.event_name == event_name)
                {
                    return look;
                }
            }
            return null;
        }

        // A scenery colour, or "".
        string scenery_text(string name)
        {
            var setting = stage.scenery_setting(name);
            return setting.kind == SettingKind.text ? setting.text : "";
        }

        // Plays a sound effect (an artist's file if there is one, otherwise the made one).
        void play_sound(string name)
        {
            game_audio.play_sound(name);
        }

        // Fades into the music for a mood.
        void play_music(string mood)
        {
            game_audio.play_music(mood);
        }
    }
}
