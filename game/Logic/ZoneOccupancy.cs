// Purpose: keeps track of who is standing in one story zone. Each character can have several
// colliders (body, feet), so the zone counts them, and someone is inside from their first
// collider in until their last collider out. Unity can report a collider leaving without having
// reported it entering (for example when it is switched off inside the zone), so a count never
// goes below zero.
using Bodhi.Rules;

namespace Bodhi.Game
{
    public class ZoneOccupancy
    {
        int human_colliders_inside = 0;
        int dog_colliders_inside = 0;

        // True while any of her colliders is in the zone.
        public bool human_inside => human_colliders_inside > 0;

        // True while any of Bodhi's colliders is in the zone.
        public bool dog_inside => dog_colliders_inside > 0;

        // One of this character's colliders came in.
        public void entered(Character who)
        {
            if (who == Character.human) human_colliders_inside += 1;
            else dog_colliders_inside += 1;
        }

        // One of this character's colliders went out.
        public void left(Character who)
        {
            if (who == Character.human) human_colliders_inside = System.Math.Max(0, human_colliders_inside - 1);
            else dog_colliders_inside = System.Math.Max(0, dog_colliders_inside - 1);
        }

        // Forgets everyone, for example when the scene moves both characters at once.
        public void clear()
        {
            human_colliders_inside = 0;
            dog_colliders_inside = 0;
        }
    }
}
