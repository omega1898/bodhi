// Purpose: one character's moves on the title screen, written as a list of beats ("sit here for
// two seconds", "gallop round the snowman", "leap to there"), and the answer to "where is she,
// which way is she facing, how fast is she going and what is she doing at this moment?". Each
// beat starts exactly where the one before ended, so a character can never jump from place to
// place. Plain C#, no Unity: tested with dotnet.
//
// Places are in metres on the snow, seen from above: x to the right of the camera, z away from
// it; the snowman stands at (0, 0). Facing is in degrees: 0 faces away from the camera (+z), 90
// faces right (+x), the same way Unity turns things.
using System;
using System.Collections.Generic;

namespace Bodhi.Game
{
    // How a move speeds up and slows down.
    public enum Ease
    {
        start_and_stop,   // From standing to standing: speeds up, then slows to a stop.
        start,            // From standing, arriving at full speed (into another move).
        stop,             // Arriving at full speed from another move, slowing to a stop.
        steady,           // The same speed all the way (one move flowing into the next).
    }

    // Whom or what a character looks at during a beat.
    public enum LookAt
    {
        ahead,     // Where they're going.
        partner,   // The other one: Em looks at Bodhi, Bodhi at Em.
        snowman,   // The snowman (or the snowball being made).
        sky,       // Up at the aurora.
        camera,    // Toward the player.
    }

    // One character at one moment.
    public struct PlayPose
    {
        public double x, z;            // Where their feet are.
        public double facing;          // Which way they face, in degrees (see the top of the file).
        public double speed;           // How fast they're moving over the snow, metres a second.
        public double height;          // How far their feet are off the snow (a leap).
        public string action;          // What they're doing: "stand", "run", "push", "sit"...
        public double action_seconds;  // How long they've been doing it.
        public double action_share;    // How far through it, 0 to 1.
        public LookAt look;            // What they're looking at.
    }

    public class PlayTrack
    {
        enum Path { stay, line, arc, leap }

        // One beat: a stretch of time doing one thing along one path.
        struct Beat
        {
            public double from_seconds, to_seconds;
            public string action;
            public LookAt look;
            public Path path;
            public Ease ease;
            public double from_x, from_z, to_x, to_z;
            public double centre_x, centre_z, radius, from_angle, sweep;  // Arcs: angles in radians.
            public double facing;       // For beats that stay put.
            public double leap_height;
        }

        readonly List<Beat> beats = new List<Beat>();
        double end_seconds, end_x, end_z, end_facing;

        // How long the whole track lasts, in seconds.
        public double length => end_seconds;

        // Where the track ends, and which way the character faces there.
        public double last_x => end_x;
        public double last_z => end_z;
        public double last_facing => end_facing;

        // Starts a track with the character standing at (x, z) facing `facing`.
        public PlayTrack(double x, double z, double facing)
        {
            end_x = x;
            end_z = z;
            end_facing = facing;
        }

        // Stays where they are for `seconds`, doing `action`, turned to `facing` (or as they were,
        // if `facing` is left out).
        public PlayTrack stay(double seconds, string action, LookAt look, double facing = double.NaN)
        {
            double turned = double.IsNaN(facing) ? end_facing : facing;
            add(new Beat { path = Path.stay, action = action, look = look, ease = Ease.steady, facing = turned }, seconds, end_x, end_z, turned);
            return this;
        }

        // Moves in a straight line to (x, z), doing `action` ("walk", "run", "push"...).
        public PlayTrack go_to(double seconds, string action, double x, double z, Ease ease, LookAt look = LookAt.ahead)
        {
            double heading = facing_of(x - end_x, z - end_z, end_facing);
            add(new Beat { path = Path.line, action = action, look = look, ease = ease, to_x = x, to_z = z }, seconds, x, z, heading);
            return this;
        }

        // Moves round a circle centred on (centre_x, centre_z), starting from where they stand,
        // through `sweep_degrees` (positive is anticlockwise seen from above).
        public PlayTrack circle(double seconds, string action, double centre_x, double centre_z, double sweep_degrees, Ease ease, LookAt look = LookAt.ahead)
        {
            double radius = Math.Sqrt(Square(end_x - centre_x) + Square(end_z - centre_z));
            double from_angle = Math.Atan2(end_z - centre_z, end_x - centre_x);
            double sweep = sweep_degrees * Math.PI / 180;
            double to_angle = from_angle + sweep;
            double x = centre_x + radius * Math.Cos(to_angle);
            double z = centre_z + radius * Math.Sin(to_angle);
            double heading = facing_of(-Math.Sin(to_angle) * Math.Sign(sweep), Math.Cos(to_angle) * Math.Sign(sweep), end_facing);
            add(new Beat { path = Path.arc, action = action, look = look, ease = ease, centre_x = centre_x, centre_z = centre_z, radius = radius, from_angle = from_angle, sweep = sweep, to_x = x, to_z = z }, seconds, x, z, heading);
            return this;
        }

        // Leaps to (x, z), rising `height` metres at the top of the arc ("leap").
        public PlayTrack leap_to(double seconds, double x, double z, double height, LookAt look = LookAt.ahead)
        {
            double heading = facing_of(x - end_x, z - end_z, end_facing);
            add(new Beat { path = Path.leap, action = "leap", look = look, ease = Ease.steady, to_x = x, to_z = z, leap_height = height }, seconds, x, z, heading);
            return this;
        }

        // Adds a beat that starts where the last one ended. A beat of no time (or a broken time)
        // is skipped, so the track can't break.
        void add(Beat beat, double seconds, double x, double z, double facing_after)
        {
            if (!(seconds > 0) || double.IsInfinity(seconds))
            {
                return;
            }
            beat.from_seconds = end_seconds;
            beat.to_seconds = end_seconds + seconds;
            beat.from_x = end_x;
            beat.from_z = end_z;
            beats.Add(beat);
            end_seconds = beat.to_seconds;
            end_x = x;
            end_z = z;
            end_facing = facing_after;
        }

        // The character at `seconds` into the track. Before the start they're at the start; after
        // the end they stay as the last beat left them. Broken times give the start.
        public PlayPose pose_at(double seconds)
        {
            if (beats.Count == 0)
            {
                return new PlayPose { x = end_x, z = end_z, facing = end_facing, action = "stand" };
            }
            if (double.IsNaN(seconds) || seconds < 0)
            {
                seconds = 0;
            }
            int index = 0;
            while (index < beats.Count - 1 && seconds >= beats[index].to_seconds)
            {
                index += 1;
            }
            var beat = beats[index];
            double duration = beat.to_seconds - beat.from_seconds;
            double share = Math.Min(1, Math.Max(0, (seconds - beat.from_seconds) / duration));
            double progress = eased(beat.ease, share);
            double rate = eased_rate(beat.ease, share) / duration;  // Progress per second.
            var pose = new PlayPose
            {
                action = beat.action,
                look = beat.look,
                action_seconds = Math.Min(duration, Math.Max(0, seconds - beat.from_seconds)),
                action_share = share,
            };
            switch (beat.path)
            {
                case Path.stay:
                    pose.x = beat.from_x;
                    pose.z = beat.from_z;
                    pose.facing = beat.facing;
                    break;
                case Path.arc:
                    double angle = beat.from_angle + beat.sweep * progress;
                    pose.x = beat.centre_x + beat.radius * Math.Cos(angle);
                    pose.z = beat.centre_z + beat.radius * Math.Sin(angle);
                    pose.facing = facing_of(-Math.Sin(angle) * Math.Sign(beat.sweep), Math.Cos(angle) * Math.Sign(beat.sweep), 0);
                    pose.speed = Math.Abs(beat.sweep) * beat.radius * rate;
                    break;
                default:  // A line or a leap.
                    pose.x = beat.from_x + (beat.to_x - beat.from_x) * progress;
                    pose.z = beat.from_z + (beat.to_z - beat.from_z) * progress;
                    double distance = Math.Sqrt(Square(beat.to_x - beat.from_x) + Square(beat.to_z - beat.from_z));
                    pose.facing = facing_of(beat.to_x - beat.from_x, beat.to_z - beat.from_z, previous_facing(index));
                    pose.speed = distance * rate;
                    if (beat.path == Path.leap)
                    {
                        pose.height = 4 * beat.leap_height * share * (1 - share);  // A parabola, like a thrown ball.
                    }
                    break;
            }
            return pose;
        }

        // Which way the character faced at the end of the beat before `index` (for a move with
        // no distance, which has no direction of its own).
        double previous_facing(int index)
        {
            for (int earlier = index - 1; earlier >= 0; earlier--)
            {
                if (beats[earlier].path == Path.stay)
                {
                    return beats[earlier].facing;
                }
            }
            return 0;
        }

        // How far through a move the character is, 0 to 1, when `share` of its time has passed.
        public static double eased(Ease ease, double share)
        {
            switch (ease)
            {
                case Ease.start_and_stop: return share * share * (3 - 2 * share);
                case Ease.start: return share * share;
                case Ease.stop: return 1 - (1 - share) * (1 - share);
                default: return share;
            }
        }

        // How quickly `eased` is changing at `share` (its slope), which turns into speed.
        static double eased_rate(Ease ease, double share)
        {
            switch (ease)
            {
                case Ease.start_and_stop: return 6 * share * (1 - share);
                case Ease.start: return 2 * share;
                case Ease.stop: return 2 * (1 - share);
                default: return 1;
            }
        }

        // The facing, in degrees, of a direction (dx, dz) on the snow. A direction of no length
        // keeps `otherwise`.
        public static double facing_of(double dx, double dz, double otherwise)
        {
            if (Math.Abs(dx) < 1e-9 && Math.Abs(dz) < 1e-9)
            {
                return otherwise;
            }
            return Math.Atan2(dx, dz) * 180 / Math.PI;
        }

        // A number times itself.
        static double Square(double value)
        {
            return value * value;
        }
    }
}
