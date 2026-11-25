// Purpose: one two-part limb of a 3D character (thigh and shin, upper arm and forearm, a dog's
// leg) that can be told "put your end there" and bends itself to reach, using LegReach's maths.
// Every bone is a Transform placed at its top joint, with its part of the body hanging down from
// it (along its own -y), so "straight down" is no turn at all.
using UnityEngine;

namespace Bodhi.Game
{
    public class Limb
    {
        public readonly Transform upper, lower;
        public readonly float upper_length, lower_length;
        readonly bool middle_forward;

        // A limb made of `upper` and `lower`. `middle_forward` true bends the middle joint
        // forward (a knee); false backward (an elbow, a dog's front leg).
        public Limb(Transform upper, Transform lower, float upper_length, float lower_length, bool middle_forward)
        {
            this.upper = upper;
            this.lower = lower;
            this.upper_length = upper_length;
            this.lower_length = lower_length;
            this.middle_forward = middle_forward;
        }

        // Bends the limb so the end of its lower part lands on `target`, given in `frame`'s own
        // space (the character's root: x right, y up, z the way they face). The limb swings in
        // the plane of the character's facing, and leans sideways a little if the spot is to one
        // side. `extra_twist` turns the whole limb about its own length (to aim a knee outward).
        public void reach(Transform frame, Vector3 target, float extra_twist = 0)
        {
            Vector3 top = frame.InverseTransformPoint(upper.position);
            Vector3 to_target = target - top;
            float sideways = Mathf.Atan2(to_target.x, -to_target.y) * Mathf.Rad2Deg;
            float down = -Mathf.Sqrt(to_target.y * to_target.y + to_target.x * to_target.x);
            var bend = LegReach.solve(to_target.z, down, upper_length, lower_length, middle_forward);
            var lean = frame.rotation * Quaternion.Euler(0, extra_twist, sideways);
            upper.rotation = lean * Quaternion.Euler(-(float)bend.upper_angle, 0, 0);
            lower.rotation = lean * Quaternion.Euler(-(float)bend.lower_angle, 0, 0);
        }

        // Makes the bones for a limb: `upper` at `joint` under `parent`, and `lower` at the end
        // of `upper`. Returns the pair, ready to have their body parts added.
        public static (Transform upper, Transform lower) make_bones(string name, Transform parent, Vector3 joint, float upper_length)
        {
            var upper = new GameObject(name + " upper").transform;
            upper.SetParent(parent, false);
            upper.localPosition = joint;
            var lower = new GameObject(name + " lower").transform;
            lower.SetParent(upper, false);
            lower.localPosition = new Vector3(0, -upper_length, 0);
            return (upper, lower);
        }

        // Makes one bone (a Transform) at `position` under `parent`.
        public static Transform bone(string name, Transform parent, Vector3 position)
        {
            var made = new GameObject(name).transform;
            made.SetParent(parent, false);
            made.localPosition = position;
            return made;
        }
    }
}
