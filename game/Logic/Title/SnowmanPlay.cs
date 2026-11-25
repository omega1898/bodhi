// Purpose: what Em and Bodhi do on the title screen, second by second. First they build a snowman
// (about half a minute): Em rolls the big bottom ball while Bodhi races round her, rolls and
// lifts the middle one, packs the head, gives it a face, and Bodhi fetches a stick for its arms.
// Then they play for as long as the title shows, in a loop that ends exactly where it began:
// she throws a snowball and he leaps to catch it, he does zoomies round the snowman, runs into
// her arms, they chase each other round it, and they rest while she looks up at the aurora.
//
// TitleWorld asks frame_at(seconds) and draws the answer. Plain C#, no Unity: tested with dotnet.
using System;
using System.Collections.Generic;

namespace Bodhi.Game
{
    // Where a snowball is.
    public enum BallState
    {
        hidden,    // Not made yet.
        ground,    // On the snow (being rolled, or waiting to be lifted).
        held,      // In Em's hands (being packed or carried).
        placing,   // On its way from her hands to the top of the snowman.
        stacked,   // Part of the snowman.
    }

    // One snowball at one moment.
    public struct Snowball
    {
        public BallState state;
        public double radius;        // Metres.
        public double x, z;          // On the ground: where its centre is above the snow.
        public double roll_degrees;  // How far it has rolled (it turns as it's pushed).
        public double share;         // While placing: 0 in her hands, 1 on top of the snowman.
        public double stacked_height;  // Where its centre sits once it's part of the snowman.
    }

    // Where the stick Bodhi fetches is.
    public enum StickPlace { ground, mouth, hand, gone }

    // A puff of snow kicked up this frame.
    public struct SnowPuff
    {
        public double x, y, z;
        public int amount;
    }

    // Everything on the snowfield at one moment.
    public struct SnowPlayFrame
    {
        public PlayPose em, bodhi;
        public Snowball bottom, middle, head;
        public Snowball thrown;          // The snowball she throws for him (radius 0.06).
        public bool thrown_in_hand;      // True while it's still in her hand, about to be thrown.
        public double thrown_height;     // How high the thrown ball is, metres.
        public StickPlace stick;
        public double stick_x, stick_z;  // Where the stick lies while it's on the ground.
        public bool has_eyes, has_nose, has_left_arm, has_right_arm;
        public List<SnowPuff> puffs;     // Puffs that happen between the last frame and this one.
        public bool playing;             // False while building, true once the play loop runs.
    }

    public class SnowmanPlay
    {
        // The snowman's three balls, and how much each sinks into the one below.
        public const double bottom_radius = 0.36, middle_radius = 0.25, head_radius = 0.17;
        public const double first_radius = 0.07;  // A snowball when it's first patted together.
        const double settle = 0.86;                // Each ball sits this share of its radius into the next.
        public static readonly double bottom_height = bottom_radius * 0.92;
        public static readonly double middle_height = bottom_height + bottom_radius + middle_radius * settle;
        public static readonly double head_height = middle_height + middle_radius + head_radius * settle;
        const double hands_gap = 0.3;              // Between her and a ball she's pushing.

        // When things happen while building (seconds from the start).
        public const double bottom_made = 1.5, bottom_rolled = 8.5;
        public const double middle_started = 11.4, middle_made = 12.4, middle_rolled = 14.8;
        public const double middle_lifted = 15.5, middle_held = 16.4, middle_carried = 17.6, middle_placed = 18.6;
        public const double head_started = 18.6, head_packing = 19.6, head_packed = 20.6, head_placed = 21.6;
        public const double eyes_at = 23.0, nose_at = 24.0;
        public const double stick_picked = 24.6, stick_handed = 28.4, left_arm_at = 32.0, right_arm_at = 32.6;
        public static readonly (double x, double z) stick_lies = (-5.65, 3.52);

        // When things happen in each round of play (seconds from the round's start).
        public const double throw_starts = 1.0, throw_released = 1.45, ball_caught = 2.6, landed = 2.9;
        public static readonly (double x, double z, double height) catch_spot = (-3.3, 1.12, 1.05);

        readonly PlayTrack em_building, bodhi_building, em_playing, bodhi_playing;
        readonly List<(double seconds, SnowPuff puff)> building_puffs = new List<(double, SnowPuff)>();
        readonly List<(double seconds, SnowPuff puff)> playing_puffs = new List<(double, SnowPuff)>();
        double last_seconds = 0;

        // How long building takes, and how long one round of play lasts.
        public double building_seconds => em_building.length;
        public double round_seconds => em_playing.length;

        // Writes the choreography down.
        public SnowmanPlay()
        {
            em_building = new PlayTrack(-0.9, 2.3, 90)
                .stay(bottom_made, "crouch", LookAt.snowman)
                .go_to(2.2, "push", 0.68, 2.2, Ease.steady, LookAt.snowman)
                .circle(4.8, "push", 0.68, 1.1, -180, Ease.steady, LookAt.snowman)
                .stay(0.7, "stand", LookAt.snowman)
                .go_to(2.2, "move", 2.4, -1.4, Ease.start_and_stop, LookAt.ahead)
                .stay(middle_made - middle_started, "crouch", LookAt.snowman, -70)
                .go_to(middle_rolled - middle_made, "push", 1.35, -0.85, Ease.steady, LookAt.snowman)
                .stay(middle_lifted - middle_rolled, "crouch", LookAt.snowman)
                .stay(middle_held - middle_lifted, "lift", LookAt.snowman)
                .go_to(middle_carried - middle_held, "carry", 0.62, -0.05, Ease.start_and_stop, LookAt.snowman)
                .stay(middle_placed - middle_carried, "place", LookAt.snowman, -90)
                .stay(head_packing - head_started, "crouch", LookAt.snowman)
                .stay(head_packed - head_packing, "pack", LookAt.snowman)
                .stay(head_placed - head_packed, "place_high", LookAt.snowman)
                .stay(0.8, "stand", LookAt.snowman)
                .stay(1.0, "place", LookAt.snowman)
                .stay(1.0, "place", LookAt.snowman)
                .go_to(0.8, "move", 1.15, -0.55, Ease.start_and_stop, LookAt.snowman)
                .stay(stick_handed - 25.2, "admire", LookAt.snowman, -64)
                .stay(0.6, "crouch", LookAt.partner, -110)
                .stay(1.4, "pat", LookAt.partner)
                .stay(0.6, "stand", LookAt.snowman)
                .go_to(0.7, "move", 0.62, -0.18, Ease.start_and_stop, LookAt.snowman)
                .stay(1.2, "place", LookAt.snowman, -90)
                .go_to(0.7, "move", 0.95, -0.6, Ease.start_and_stop, LookAt.partner);

            bodhi_building = new PlayTrack(-1.9, 0.9, 35)
                .stay(2.2, "sit", LookAt.partner)
                .stay(1.0, "bow", LookAt.partner)
                .circle(3.2, "move", 0.2, 0.9, -300, Ease.start_and_stop)
                .stay(1.0, "bow", LookAt.partner, 45)
                .stay(1.8, "sit", LookAt.snowman)
                .go_to(1.0, "move", -0.55, -0.45, Ease.start_and_stop, LookAt.snowman)
                .stay(1.0, "sniff", LookAt.snowman)
                .go_to(1.0, "move", -3.2, 0.6, Ease.start)
                .circle(4.2, "move", 0.4, 0.6, -380, Ease.steady)
                .go_to(1.2, "move", -1.2, -0.6, Ease.stop)
                .stay(4.0, "sit", LookAt.partner, 73)
                .stay(0.6, "stand", LookAt.ahead, -50)
                .go_to(2.2, "move", -5.2, 3.2, Ease.start_and_stop)
                .stay(stick_picked + 0.6 - 24.4, "pick", LookAt.ahead)
                .go_to(3.0, "carry", 0.4, -0.95, Ease.start_and_stop)
                .stay(0.4, "stand", LookAt.partner, 62)
                .stay(1.8, "lean", LookAt.partner)
                .stay(2.5, "sit", LookAt.partner)
                .go_to(0.7, "move", 0.32, -0.95, Ease.start_and_stop, LookAt.partner);

            em_playing = new PlayTrack(em_building.last_x, em_building.last_z, -150)
                .stay(throw_starts, "crouch", LookAt.partner)
                .stay(0.9, "throw", LookAt.ahead, -65)
                .stay(8.0 - 1.9, "watch", LookAt.partner)
                .stay(1.4, "open_arms", LookAt.partner, -150)
                .stay(2.0, "hug", LookAt.partner)
                .stay(0.8, "stand", LookAt.partner)
                .circle(4.6, "move", 0, 0.1, 360, Ease.start_and_stop, LookAt.ahead)
                .stay(0.8, "stand", LookAt.partner, -150)
                .stay(6.4, "look_up", LookAt.sky, -20);

            bodhi_playing = new PlayTrack(bodhi_building.last_x, bodhi_building.last_z, -100)
                .stay(1.2, "bow", LookAt.partner)
                .go_to(1.1, "move", -2.5, 0.6, Ease.start)
                .leap_to(landed - 2.3, -4.1, 1.7, 0.55)
                .circle(5.1, "move", -0.7, 0.8, 464.8, Ease.steady)
                .go_to(1.2, "move", 0.32, -0.95, Ease.stop)
                .stay(2.2, "lean", LookAt.partner, 30)
                .stay(0.8, "bow", LookAt.partner)
                .circle(4.6, "move", 0, 0.1, 360, Ease.start_and_stop)
                .stay(1.0, "stand", LookAt.partner, 60)
                .stay(6.2, "sit", LookAt.partner);

            add_puff(building_puffs, 11.6, 2.1, 0.05, -1.25, 6);           // She kneels for the middle ball.
            add_puff(building_puffs, 17.4, -1.2, 0.05, -0.6, 10);          // He skids to a stop after zoomies.
            add_puff(building_puffs, middle_placed - 0.15, 0, middle_height - middle_radius, 0, 8);
            add_puff(building_puffs, head_placed - 0.15, 0, head_height - head_radius, 0, 6);
            add_puff(building_puffs, stick_picked, stick_lies.x, 0.05, stick_lies.z, 7);
            add_puff(playing_puffs, 0.3, 0.6, 0.05, -0.85, 5);               // She scoops snow.
            add_puff(playing_puffs, landed, -4.1, 0.05, 1.7, 16);            // He lands from the catch.
            add_puff(playing_puffs, 9.1, 0.3, 0.05, -1.0, 8);                // He stops in front of her.
        }

        // Notes a puff of snow at `seconds`.
        static void add_puff(List<(double, SnowPuff)> list, double seconds, double x, double y, double z, int amount)
        {
            list.Add((seconds, new SnowPuff { x = x, y = y, z = z, amount = amount }));
        }

        // Everything on the snowfield `seconds` after the title opened. Puffs are those since the
        // last call (call it once a frame, with time going forward). Broken times count as 0.
        public SnowPlayFrame frame_at(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            {
                seconds = 0;
            }
            var frame = new SnowPlayFrame { puffs = new List<SnowPuff>() };
            bool playing = seconds >= building_seconds;
            double round_time = playing ? (seconds - building_seconds) % round_seconds : 0;
            frame.playing = playing;
            frame.em = playing ? em_playing.pose_at(round_time) : em_building.pose_at(seconds);
            frame.bodhi = playing ? bodhi_playing.pose_at(round_time) : bodhi_building.pose_at(seconds);

            // While Em watches him, she turns to keep him in view.
            if (frame.em.action == "watch")
            {
                frame.em.facing = PlayTrack.facing_of(frame.bodhi.x - frame.em.x, frame.bodhi.z - frame.em.z, frame.em.facing);
            }

            set_balls(ref frame, seconds);
            set_stick_and_face(ref frame, seconds);
            if (playing)
            {
                set_thrown_ball(ref frame, round_time);
            }
            collect_puffs(frame.puffs, last_seconds, seconds);
            last_seconds = seconds;
            return frame;
        }

        // The three balls of the snowman at `seconds`.
        void set_balls(ref SnowPlayFrame frame, double seconds)
        {
            var em = frame.em;
            frame.bottom = new Snowball { stacked_height = bottom_height };
            frame.middle = new Snowball { stacked_height = middle_height };
            frame.head = new Snowball { stacked_height = head_height };

            // The bottom ball: patted together, then rolled round to the middle, growing.
            if (seconds < bottom_rolled)
            {
                double grown = Math.Max(0, Math.Min(1, (seconds - bottom_made) / (bottom_rolled - bottom_made)));
                double radius = seconds < bottom_made
                    ? first_radius + (0.12 - first_radius) * (seconds / bottom_made)
                    : 0.12 + (bottom_radius - 0.12) * Math.Pow(grown, 0.8);
                frame.bottom = ball_in_front(em, radius, BallState.ground, bottom_height);
                frame.bottom.roll_degrees = grown * 5.0 / 0.25 * 57.3;  // About five metres rolled.
            }
            else
            {
                frame.bottom = new Snowball { state = BallState.stacked, radius = bottom_radius, stacked_height = bottom_height };
            }

            // The middle ball: patted, rolled, lifted, carried and set on top.
            if (seconds < middle_started)
            {
                frame.middle.state = BallState.hidden;
            }
            else if (seconds < middle_rolled)
            {
                double grown = Math.Max(0, (seconds - middle_made) / (middle_rolled - middle_made));
                double radius = seconds < middle_made ? first_radius + 0.04 * ((seconds - middle_started) / (middle_made - middle_started)) : 0.11 + (middle_radius - 0.11) * grown;
                frame.middle = ball_in_front(em, radius, BallState.ground, middle_height);
                frame.middle.roll_degrees = grown * 1.2 / 0.18 * 57.3;
            }
            else if (seconds < middle_lifted)
            {
                frame.middle = ball_at_rest(em, middle_radius, middle_height, middle_rolled, seconds);
            }
            else if (seconds < middle_carried)
            {
                frame.middle = new Snowball { state = BallState.held, radius = middle_radius, stacked_height = middle_height };
            }
            else if (seconds < middle_placed)
            {
                double share = PlayTrack.eased(Ease.start_and_stop, (seconds - middle_carried) / (middle_placed - middle_carried));
                frame.middle = new Snowball { state = BallState.placing, radius = middle_radius, share = share, stacked_height = middle_height };
            }
            else
            {
                frame.middle = new Snowball { state = BallState.stacked, radius = middle_radius, stacked_height = middle_height };
            }

            // The head: scooped, packed in her hands, set on top.
            if (seconds < head_started + 0.4)
            {
                frame.head.state = BallState.hidden;
            }
            else if (seconds < head_packed)
            {
                double share = (seconds - head_started - 0.4) / (head_packed - head_started - 0.4);
                frame.head = new Snowball { state = BallState.held, radius = first_radius + (head_radius - first_radius) * share, stacked_height = head_height };
            }
            else if (seconds < head_placed)
            {
                double share = PlayTrack.eased(Ease.start_and_stop, (seconds - head_packed) / (head_placed - head_packed));
                frame.head = new Snowball { state = BallState.placing, radius = head_radius, share = share, stacked_height = head_height };
            }
            else
            {
                frame.head = new Snowball { state = BallState.stacked, radius = head_radius, stacked_height = head_height };
            }
        }

        // A ball on the snow just in front of Em, where her hands are as she pushes it.
        static Snowball ball_in_front(PlayPose em, double radius, BallState state, double stacked_height)
        {
            double angle = em.facing * Math.PI / 180;
            double reach = radius + hands_gap;
            return new Snowball
            {
                state = state,
                radius = radius,
                x = em.x + Math.Sin(angle) * reach,
                z = em.z + Math.Cos(angle) * reach,
                stacked_height = stacked_height,
            };
        }

        // The middle ball resting where she stopped rolling it, until she lifts it.
        Snowball ball_at_rest(PlayPose em_now, double radius, double stacked_height, double rolled_at, double seconds)
        {
            var em_then = em_building.pose_at(rolled_at - 0.001);
            var ball = ball_in_front(em_then, radius, BallState.ground, stacked_height);
            ball.roll_degrees = 1.2 / 0.18 * 57.3;
            return ball;
        }

        // The stick, and the snowman's face and arms, at `seconds`.
        static void set_stick_and_face(ref SnowPlayFrame frame, double seconds)
        {
            frame.stick_x = stick_lies.x;
            frame.stick_z = stick_lies.z;
            frame.stick = seconds < stick_picked ? StickPlace.ground
                        : seconds < stick_handed ? StickPlace.mouth
                        : seconds < right_arm_at ? StickPlace.hand
                        : StickPlace.gone;
            frame.has_eyes = seconds >= eyes_at;
            frame.has_nose = seconds >= nose_at;
            frame.has_left_arm = seconds >= left_arm_at;
            frame.has_right_arm = seconds >= right_arm_at;
        }

        // The snowball she throws for him: in her hand as she winds up, then flying in an arc to
        // where he catches it at the top of his leap.
        static void set_thrown_ball(ref SnowPlayFrame frame, double round_time)
        {
            frame.thrown = new Snowball { radius = 0.06 };
            frame.thrown_in_hand = round_time >= 0.2 && round_time < throw_released;
            if (round_time < throw_released || round_time >= ball_caught)
            {
                frame.thrown.state = BallState.hidden;
                return;
            }
            double share = (round_time - throw_released) / (ball_caught - throw_released);
            var em = frame.em;
            double start_x = em.x - 0.15, start_z = em.z + 0.05, start_height = 1.85;
            frame.thrown.state = BallState.ground;  // "In the open"; its height says it's flying.
            frame.thrown.x = start_x + (catch_spot.x - start_x) * share;
            frame.thrown.z = start_z + (catch_spot.z - start_z) * share;
            double straight = start_height + (catch_spot.height - start_height) * share;
            frame.thrown_height = straight + 4 * 1.2 * share * (1 - share);  // Rises 1.2 m above the straight line.
        }

        // The puffs whose time falls after `from` and up to `to`, in whichever part (building or
        // a round of play) those times fall.
        void collect_puffs(List<SnowPuff> into, double from, double to)
        {
            if (!(to > from))
            {
                return;
            }
            foreach (var (seconds, puff) in building_puffs)
            {
                if (seconds > from && seconds <= to)
                {
                    into.Add(puff);
                }
            }
            if (to < building_seconds)
            {
                return;
            }
            double round_start = building_seconds + Math.Floor((to - building_seconds) / round_seconds) * round_seconds;
            foreach (var (seconds, puff) in playing_puffs)
            {
                double when = round_start + seconds;
                if (when > from && when <= to)
                {
                    into.Add(puff);
                }
            }
        }
    }
}
