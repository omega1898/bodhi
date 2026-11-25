// Purpose: keeps the scene in step with the story. Each frame it takes the story rules the
// chapter has just carried out and works out what the screen should now show: how dark it is,
// the music's mood, how high each flood stands, what Bodhi carries, who is hidden, where the
// camera holds, where the scent trail leads and who is walking somewhere on their own. It is the
// staging half of the browser game's apply_effects (prototype/web/js/game.js), kept out of Unity
// so it can be tested.
//
// Two kinds of change. Lasting ones stay true afterwards (night has fallen, the flood is up), so
// they count even when Continue replays a save. Moment-only ones (a sound, a shake, a caption, a
// walk, a cut) only make sense as they happen, so take_live_staging hands over only the rules
// that played live, and a replayed save plays none of them.
using System;
using System.Collections.Generic;
using Bodhi.Rules;

namespace Bodhi.Game
{
    public class StageTracker
    {
        public const double darkest = 0.95;  // Never fully black: you can always see her.

        readonly ChapterStage stage;
        readonly ChapterRun run;
        readonly Dictionary<string, double> water_tops = new Dictionary<string, double>();
        readonly HashSet<StageWho> hidden = new HashSet<StageWho>();
        readonly Dictionary<StageWho, double> walk_targets = new Dictionary<StageWho, double>();
        readonly List<RuleStaging> live = new List<RuleStaging>();
        int events_seen = 0;

        public double darkness { get; private set; }
        public string mood { get; private set; }
        public string guest_mode { get; private set; } = "";
        public string dog_carrying { get; private set; } = "";
        public CameraChange camera { get; private set; } = CameraChange.release;  // hold or release.
        public double camera_x { get; private set; }
        public double camera_y { get; private set; }
        public double camera_zoom { get; private set; } = 1;
        public bool has_scent { get; private set; }
        public double scent_x { get; private set; }
        public double scent_y { get; private set; }

        // True after a replayed save moved a flood: the scene should put the water straight at its
        // new height instead of raising it slowly, then call water_snapped.
        public bool snap_water { get; private set; }

        // Starts tracking one chapter's run. The stage must be that chapter's.
        public StageTracker(ChapterStage stage, ChapterRun run)
        {
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            if (run.chapter == null || run.chapter.id != stage.id)
            {
                throw new ArgumentException("The stage \"" + stage.id + "\" isn't this chapter's.", nameof(stage));
            }
            var start_darkness = stage.scenery_setting("darkness");
            darkness = start_darkness.kind == SettingKind.number ? usable_darkness(start_darkness.number) : 0;
            mood = stage.mood ?? "";
            guest_mode = stage.guest != null ? stage.guest.mode ?? "still" : "";
            foreach (var pool in stage.water ?? Array.Empty<StageWater>())
            {
                if (pool != null && !string.IsNullOrEmpty(pool.id))
                {
                    water_tops[pool.id] = pool.y;
                }
            }
        }

        // The surface a pool or flood should stand at, or null if the stage hasn't got it.
        public double? water_top(string water_id)
        {
            return water_id != null && water_tops.TryGetValue(water_id, out double top) ? top : (double?)null;
        }

        // True if this one is out of sight (she's in bed, the guest has gone home).
        public bool is_hidden(StageWho who)
        {
            return hidden.Contains(who);
        }

        // The x this one is walking (or he is running) to on their own, or null.
        public double? walk_target(StageWho who)
        {
            return walk_targets.TryGetValue(who, out double x) ? x : (double?)null;
        }

        // The scene says they got there: the walk is over.
        public void arrived(StageWho who)
        {
            walk_targets.Remove(who);
        }

        // The scene has put every flood at its height.
        public void water_snapped()
        {
            snap_water = false;
        }

        // Catches up with the story: call once a frame, after the run has moved on.
        public void update()
        {
            var events = run.event_order;
            int skipped_at = -1;
            for (int index = events_seen; index < events.Count; index++)
            {
                if (events[index] == "cutscene_skipped")
                {
                    skipped_at = index;
                }
            }
            events_seen = events.Count;
            bool skip_handled = skipped_at < 0;
            var rules = run.chapter.rules ?? Array.Empty<StoryRule>();
            foreach (var applied in run.take_applied_rules())
            {
                // Skip drops the film's walks before the rules it sets off run, as in the browser.
                if (!skip_handled && applied.events_so_far > skipped_at)
                {
                    drop_walks();
                    skip_handled = true;
                }
                var rule = applied.rule_number >= 0 && applied.rule_number < rules.Length ? rules[applied.rule_number] : null;
                if (rule != null && rule.cutscene == CutsceneChange.end)
                {
                    end_film(applied.replayed);
                }
                var staging = applied.rule_number >= 0 && applied.rule_number < (stage.rules?.Length ?? 0) ? stage.rules[applied.rule_number] : null;
                if (staging != null)
                {
                    stage_rule(staging, applied.replayed);
                }
            }
            if (!skip_handled)
            {
                drop_walks();
            }
            if (skipped_at >= 0 && !run.cutscene_playing && camera == CameraChange.hold)
            {
                end_film(false);  // The scene's end forgot to let the camera go.
            }
        }

        // Hands over the stagings that played live since the last call, oldest first, for their
        // moment-only parts: sounds, shakes, captions, poses, cuts and effects.
        public List<RuleStaging> take_live_staging()
        {
            var taken = new List<RuleStaging>(live);
            live.Clear();
            return taken;
        }

        // Carries out one rule's staging. A replayed rule changes only what lasts.
        void stage_rule(RuleStaging staging, bool replayed)
        {
            foreach (var level in staging.water ?? Array.Empty<StageWaterLevel>())
            {
                if (level.water_id != null && water_tops.ContainsKey(level.water_id))
                {
                    water_tops[level.water_id] = level.top;
                    snap_water |= replayed;
                }
            }
            if (staging.darkness != null)
            {
                darkness = usable_darkness(staging.darkness.Value);
            }
            if (!string.IsNullOrEmpty(staging.mood))
            {
                mood = staging.mood;
            }
            if (staging.scent == ScentChange.point)
            {
                has_scent = true;
                scent_x = staging.scent_x;
                scent_y = staging.scent_y;
            }
            else if (staging.scent == ScentChange.clear)
            {
                has_scent = false;
            }
            if (staging.carry == CarryChange.carry)
            {
                dog_carrying = staging.carry_item ?? "";
            }
            else if (staging.carry == CarryChange.drop)
            {
                dog_carrying = "";
            }
            if (!string.IsNullOrEmpty(staging.guest_mode) && stage.guest != null)
            {
                guest_mode = staging.guest_mode;
                if (guest_mode != "gone")
                {
                    hidden.Remove(StageWho.guest);
                }
            }
            foreach (var hiding in staging.hide ?? Array.Empty<StageHiding>())
            {
                if (hiding.hidden) hidden.Add(hiding.who);
                else hidden.Remove(hiding.who);
            }
            if (staging.camera == CameraChange.hold)
            {
                camera = CameraChange.hold;
                camera_x = staging.camera_x;
                camera_y = staging.camera_y;
                camera_zoom = staging.camera_zoom > 0 && !double.IsInfinity(staging.camera_zoom) ? staging.camera_zoom : 1;
            }
            else if (staging.camera == CameraChange.release)
            {
                camera = CameraChange.release;
            }
            if (replayed)
            {
                return;  // The save already says where everyone stands; nothing plays.
            }
            if ((staging.place ?? Array.Empty<StagePlacement>()).Length > 0)
            {
                walk_targets.Remove(StageWho.human);  // A cut moved someone: stop her walk.
            }
            if (staging.dog_runs_to != null)
            {
                walk_targets[StageWho.dog] = staging.dog_runs_to.Value;
            }
            foreach (var walk in staging.walk ?? Array.Empty<StageWalk>())
            {
                walk_targets[walk.who] = walk.x;
            }
            live.Add(staging);
        }

        // A film scene has ended: the camera follows the player again and its caption goes.
        void end_film(bool replayed)
        {
            camera = CameraChange.release;
            if (!replayed)
            {
                live.Add(new RuleStaging { caption = "" });
            }
        }

        // A skipped film drops the walks it had started.
        void drop_walks()
        {
            walk_targets.Remove(StageWho.human);
            walk_targets.Remove(StageWho.dog);
        }

        // Darkness between 0 and `darkest`; nonsense counts as 0.
        static double usable_darkness(double value)
        {
            return double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(darkest, value));
        }
    }
}
