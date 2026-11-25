// Purpose: looking after Bodhi, ported from the web prototype's care.js. He gets hungry as time
// passes and dirty in water and mud; he can be fed from the bag and towelled clean. None of it
// ever ends the game or blocks the story (spec R4.3): a hungry or muddy Bodhi only behaves a
// little differently, and looking after him calms him.
using System;

namespace Bodhi.Rules
{
    // What Bodhi is standing in right now.
    public enum Surface
    {
        dry_ground,
        water,
        mud,
    }

    // Which kind of care the game offers on the "use" button right now, if any.
    public enum CareOffer
    {
        none,
        feed,
        towel,
    }

    public class CareState
    {
        // How fast his needs change, the same values as care.js.
        public const double minutes_from_fed_to_hungry = 8.0;  // For a grown dog; a puppy is sooner.
        public const double dirt_per_second_in_water = 0.08;   // Muddy within about ten seconds.
        public const double dirt_per_second_in_mud = 0.2;      // Much faster.
        public const double hungry_from = 0.7;                 // From here he slows a little and noses the bag.
        public const double offer_food_from = 0.45;            // "Feed" is offered from here, so it doesn't nag.
        public const double dirty_from = 0.55;                 // From here his paws and legs show the mud.
        public const int largest_food_carried = 9;             // Meals the bag holds.
        public const double hungry_speed_scale = 0.88;         // A hungry dog follows a little slower.

        // Longer steps than an hour are treated as a bug, not as time passing.
        public const double longest_update_seconds = 3600.0;

        // 0 is just fed, 1 is very hungry.
        public double hunger { get; private set; }

        // 0 is clean, 1 is covered in mud.
        public double dirt { get; private set; }

        // Meals in the bag, 0 to 9.
        public int food { get; private set; }

        // A new game: a little hungry, clean, two meals in the bag.
        public CareState() : this(0.3, 0.0, 2)
        {
        }

        // Starts from given values, for example from a saved file. Anything damaged is made safe:
        // a value that isn't a number becomes the new-game value, and the rest are kept in range.
        public CareState(double hunger, double dirt, int food)
        {
            this.hunger = Math.Clamp(double.IsFinite(hunger) ? hunger : 0.3, 0.0, 1.0);
            this.dirt = Math.Clamp(double.IsFinite(dirt) ? dirt : 0.0, 0.0, 1.0);
            this.food = Math.Clamp(food, 0, largest_food_carried);
        }

        // True when he's hungry enough to show it.
        public bool is_hungry()
        {
            return hunger >= hungry_from;
        }

        // How fast he follows compared with normal: a little slower when hungry, never stopped.
        public double speed_scale()
        {
            return is_hungry() ? hungry_speed_scale : 1.0;
        }

        // Moves his needs on by one step of time. `size_scale` is his size (a puppy is about 0.5).
        // Scripted scenes pass `in_scripted_scene` true, which pauses his needs.
        public void update(double delta_seconds, Surface surface, double size_scale, bool in_scripted_scene)
        {
            bool time_is_usable = double.IsFinite(delta_seconds) && delta_seconds >= 0.0 && delta_seconds <= longest_update_seconds;
            if (!time_is_usable || in_scripted_scene)
            {
                return;
            }
            // A puppy gets hungry about a quarter faster than a grown dog.
            double safe_size = Math.Clamp(double.IsFinite(size_scale) ? size_scale : 1.0, 0.3, 1.0);
            double appetite = 1.5 - 0.5 * safe_size;
            hunger = Math.Clamp(hunger + delta_seconds / (minutes_from_fed_to_hungry * 60.0) * appetite, 0.0, 1.0);

            if (surface == Surface.water)
            {
                dirt = Math.Clamp(dirt + dirt_per_second_in_water * delta_seconds, 0.0, 1.0);
            }
            else if (surface == Surface.mud)
            {
                dirt = Math.Clamp(dirt + dirt_per_second_in_mud * delta_seconds, 0.0, 1.0);
            }
        }

        // What care to offer now. Only her, only when they're together, never during a scene.
        public CareOffer offered_care(bool together, Character controlled, bool in_scripted_scene)
        {
            if (!together || controlled != Character.human || in_scripted_scene)
            {
                return CareOffer.none;
            }
            if (hunger >= offer_food_from && food > 0)
            {
                return CareOffer.feed;
            }
            if (dirt >= dirty_from)
            {
                return CareOffer.towel;
            }
            return CareOffer.none;
        }

        // Feeds him. From the bag it uses a meal; a meal at home doesn't. Eating calms him a little.
        // Returns false, and changes nothing, if the bag is empty.
        public bool feed(bool from_bag, BondState bond)
        {
            if (from_bag && food <= 0)
            {
                return false;
            }
            if (from_bag)
            {
                food -= 1;
            }
            hunger = 0.0;
            if (bond != null)
            {
                bond.fear = Math.Max(0.0, bond.fear - 0.1);
            }
            return true;
        }

        // Towels him (most of the mud comes off) or bathes him (all of it comes off).
        public void clean(bool bath)
        {
            dirt = bath ? 0.0 : Math.Max(0.0, dirt - 0.75);
        }

        // Picks up found food. Returns how many meals went into the bag; the rest is left behind.
        public int pick_up_food(double meals)
        {
            if (!double.IsFinite(meals) || meals <= 0.0)
            {
                return 0;
            }
            int room = largest_food_carried - food;
            int taken = (int)Math.Min(room, Math.Round(meals));
            food += taken;
            return taken;
        }
    }
}
