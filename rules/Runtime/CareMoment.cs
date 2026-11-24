// Purpose: the short, touch-driven care scenes (spec 3.5, R4.1 to R4.3, decision D6):
// towel-drying, feeding, bathing and grooming. The Unity scene turns finger movements into
// gestures ("towel_rub on area 2"); this class decides what counts, how far along the moment
// is, and what it changes. Care builds trust, but only up to a cap per chapter, so repeating a
// moment can't farm it. A moment can always be skipped, and skipping never punishes.
using System;

namespace Bodhi.Rules
{
    // Which care moment this is.
    public enum CareMomentKind
    {
        drying,    // Rub circles over the wet areas until each is dry.
        feeding,   // Pour the food into the bowl, then wait while he eats.
        bathing,   // Scrub, rinse, then step back for the shake-off.
        grooming,  // Brush along the coat to work out the tangles.
    }

    // One touch gesture, as the scene recognises it.
    public enum CareGesture
    {
        towel_rub,
        food_poured,
        scrub,
        rinse,
        step_back,
        brush_with_coat,
        brush_against_coat,
    }

    // Where a care moment stands.
    public enum CareMomentState
    {
        in_progress,
        finished,
        skipped,
    }

    // How much trust care may still add in this chapter. Make one per chapter and share it
    // between that chapter's care moments.
    public class CareTrustBudget
    {
        // The most trust care can add in one chapter.
        public const double trust_per_chapter = 0.08;

        // Trust care can still add in this chapter.
        public double remaining { get; private set; } = trust_per_chapter;

        // Takes up to `amount` from the budget and returns how much was actually given.
        public double take(double amount)
        {
            if (!double.IsFinite(amount) || amount <= 0.0)
            {
                return 0.0;
            }
            double given = Math.Min(amount, remaining);
            remaining -= given;
            return given;
        }
    }

    public class CareMoment
    {
        // How each moment is built. Starting values, to tune in playtests (aim: 30 to 120 seconds).
        public const int wet_area_count = 4;          // Drying: back, chest, legs, head.
        public const int rubs_per_wet_area = 3;
        public const double eating_seconds = 6.0;     // Feeding: how long he eats.
        public const int scrubs_needed = 8;           // Bathing.
        public const int rinses_needed = 4;
        public const int strokes_needed = 10;         // Grooming.
        public const double trust_per_gesture = 0.004;
        public const double grooming_calm = 0.05;     // How much grooming lowers his fear.

        readonly CareState care;
        readonly BondState bond;
        readonly CareTrustBudget budget;
        readonly bool food_from_bag;
        readonly int[] rubs_by_area = new int[wet_area_count];
        int scrubs_done = 0;
        int rinses_done = 0;
        int strokes_done = 0;
        bool food_is_poured = false;
        double seconds_eaten = 0.0;

        // Which moment this is.
        public CareMomentKind kind { get; }

        // In progress, finished or skipped.
        public CareMomentState state { get; private set; } = CareMomentState.in_progress;

        // Gestures refused as broken input (a value that isn't a real gesture, a missing area).
        public int refused_gesture_count { get; private set; }

        // The event to report to the chapter when this moment finishes, e.g. "dog_fed".
        public string finished_event_name
        {
            get
            {
                switch (kind)
                {
                    case CareMomentKind.drying: return "dog_dried";
                    case CareMomentKind.feeding: return "dog_fed";
                    case CareMomentKind.bathing: return "dog_bathed";
                    default: return "dog_groomed";
                }
            }
        }

        // Starts a care moment. `food_from_bag` is false for a meal at home.
        // Missing parts (null) are allowed: the moment still runs and simply changes less.
        public CareMoment(CareMomentKind kind, CareState care, BondState bond, CareTrustBudget budget, bool food_from_bag)
        {
            this.kind = kind;
            this.care = care;
            this.bond = bond;
            this.budget = budget;
            this.food_from_bag = food_from_bag;
        }

        // The scene reports a gesture. `area_index` says which wet area a towel rub was on.
        // Returns true if the gesture counted. A gesture that belongs to another step is simply
        // ignored, because fumbling is normal and never punished.
        public bool report_gesture(CareGesture gesture, int area_index = 0)
        {
            if (!Enum.IsDefined(typeof(CareGesture), gesture))
            {
                refused_gesture_count += 1;
                return false;
            }
            if (state != CareMomentState.in_progress)
            {
                return false;
            }
            bool counted = false;
            switch (kind)
            {
                case CareMomentKind.drying:
                    if (gesture == CareGesture.towel_rub)
                    {
                        if (area_index < 0 || area_index >= wet_area_count)
                        {
                            refused_gesture_count += 1;
                            return false;
                        }
                        if (rubs_by_area[area_index] < rubs_per_wet_area)
                        {
                            rubs_by_area[area_index] += 1;
                            counted = true;
                        }
                    }
                    break;
                case CareMomentKind.feeding:
                    if (gesture == CareGesture.food_poured && !food_is_poured)
                    {
                        food_is_poured = true;
                        counted = true;
                    }
                    break;
                case CareMomentKind.bathing:
                    if (gesture == CareGesture.scrub && scrubs_done < scrubs_needed)
                    {
                        scrubs_done += 1;
                        counted = true;
                    }
                    else if (gesture == CareGesture.rinse && scrubs_done >= scrubs_needed && rinses_done < rinses_needed)
                    {
                        rinses_done += 1;
                        counted = true;
                    }
                    else if (gesture == CareGesture.step_back && rinses_done >= rinses_needed)
                    {
                        counted = true;
                        finish();
                    }
                    break;
                case CareMomentKind.grooming:
                    if (gesture == CareGesture.brush_with_coat && strokes_done < strokes_needed)
                    {
                        strokes_done += 1;
                        counted = true;
                    }
                    break;
            }
            if (counted)
            {
                add_trust(trust_per_gesture);
                finish_if_complete();
            }
            return counted;
        }

        // Time passes during the moment. Only feeding uses it: he eats once the food is poured.
        public void pass_time(double delta_seconds)
        {
            bool time_is_usable = double.IsFinite(delta_seconds) && delta_seconds >= 0.0 && delta_seconds <= CareState.longest_update_seconds;
            if (!time_is_usable || state != CareMomentState.in_progress || kind != CareMomentKind.feeding || !food_is_poured)
            {
                return;
            }
            seconds_eaten = Math.Min(eating_seconds, seconds_eaten + delta_seconds);
            finish_if_complete();
        }

        // The player leaves the moment. Nothing is taken away: he just stays as he was.
        public void skip()
        {
            if (state == CareMomentState.in_progress)
            {
                state = CareMomentState.skipped;
            }
        }

        // How far along the moment is, from 0 to 1, for an on-screen hint.
        public double progress()
        {
            if (state == CareMomentState.finished)
            {
                return 1.0;
            }
            switch (kind)
            {
                case CareMomentKind.drying:
                    int rubs_done = 0;
                    foreach (int rubs in rubs_by_area)
                    {
                        rubs_done += rubs;
                    }
                    return (double)rubs_done / (wet_area_count * rubs_per_wet_area);
                case CareMomentKind.feeding:
                    return (food_is_poured ? 0.5 : 0.0) + 0.5 * seconds_eaten / eating_seconds;
                case CareMomentKind.bathing:
                    return (double)(scrubs_done + rinses_done) / (scrubs_needed + rinses_needed + 1);
                default:
                    return (double)strokes_done / strokes_needed;
            }
        }

        // Finishes the moment if its last step is done. (Bathing finishes on the step back itself.)
        void finish_if_complete()
        {
            bool complete;
            switch (kind)
            {
                case CareMomentKind.drying: complete = progress() >= 1.0; break;
                case CareMomentKind.feeding: complete = food_is_poured && seconds_eaten >= eating_seconds; break;
                case CareMomentKind.grooming: complete = strokes_done >= strokes_needed; break;
                default: complete = false; break;
            }
            if (complete)
            {
                finish();
            }
        }

        // Marks the moment finished and applies what it does for him.
        void finish()
        {
            if (state != CareMomentState.in_progress)
            {
                return;
            }
            state = CareMomentState.finished;
            switch (kind)
            {
                case CareMomentKind.drying:
                    care?.clean(false);
                    break;
                case CareMomentKind.feeding:
                    care?.feed(food_from_bag, bond);
                    break;
                case CareMomentKind.bathing:
                    care?.clean(true);
                    break;
                case CareMomentKind.grooming:
                    if (bond != null)
                    {
                        bond.fear = Math.Max(0.0, bond.fear - grooming_calm);
                    }
                    break;
            }
        }

        // Adds trust, but only what the chapter's budget still allows.
        void add_trust(double amount)
        {
            if (bond == null || budget == null)
            {
                return;
            }
            bond.trust = Math.Min(1.0, bond.trust + budget.take(amount));
        }
    }
}
