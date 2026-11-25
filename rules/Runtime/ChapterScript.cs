// Purpose: a chapter written as data: the things you can use, the places you can walk into, and
// the story rules that run when something happens. ChapterRun plays any ChapterScript, so all
// twelve chapters share one tested engine. These are the same fields as a chapter in the browser
// game (prototype/web/js/levels.js), minus the drawing-only ones (poses, camera, sounds, water),
// which belong to the Unity scene.
using System;

namespace Bodhi.Rules
{
    // What looking after Bodhi a thing does when it's used.
    public enum CareKind
    {
        none,
        meal,   // A meal in his bowl: he's fed, and the bag isn't used.
        treat,  // Something he found (an apple): half his hunger goes.
        bath,   // All the mud comes off.
    }

    // Who has to be inside a zone for it to count.
    public enum ZoneWho
    {
        human,  // Her, while the player controls her.
        dog,    // Bodhi, while the player controls him.
        both,   // Both of them, whoever the player controls.
    }

    // Whose words a story line is.
    public enum StoryVoice
    {
        her,   // Her thoughts and speech (the default).
        dog,   // Bodhi's thoughts, short and simple.
        note,  // A written note or sign.
    }

    // What a rule does to a film scene (a cutscene).
    public enum CutsceneChange
    {
        none,   // Leaves it as it is.
        start,  // Starts one: the controls step aside until it ends or is skipped.
        end,    // Ends the one playing.
    }

    // Something one of them can use: a lever, a bowl, a keepsake.
    public sealed class ChapterThing
    {
        public string event_name = "";                        // Fired when it's used, once.
        public Character who = Character.human;               // Who can use it.
        public string label = "";                             // The prompt, e.g. "Pull the sheets off the bed".
        public string[] requires = Array.Empty<string>();     // Events that must have happened first.
        public string[] until = Array.Empty<string>();        // Events that close it for good.
        public bool needs_dog = false;                        // Bodhi must be right there (a bath, a bowl).
        public bool hidden = false;                           // Only shows once Bodhi has sniffed it out.
        public string keepsake = "";                          // A keepsake's name, or "" if it isn't one.
        public string memory_text = "";                       // The line she says when the keepsake is found.
        public CareKind care = CareKind.none;                 // How it looks after him, if at all.
        public int food = 0;                                  // Meals it puts in the bag.
        public double x = 0;                                  // Where it is, in the chapter's 2D layout.
        public double y = 0;

        // The name it's remembered by once sniffed out: its keepsake name, or else its event.
        public string reveal_name => string.IsNullOrEmpty(keepsake) ? event_name : keepsake;
    }

    // An area that fires an event when the right character walks into it.
    public sealed class ChapterZone
    {
        public string event_name = "";
        public ZoneWho who = ZoneWho.human;
        public string[] requires = Array.Empty<string>();     // Events that must have happened first.
        public string[] unless = Array.Empty<string>();       // Events that switch it off.
        public double x = 0;                                  // Its box in the chapter's 2D layout.
        public double y = 0;
        public double width = 0;
        public double height = 0;
    }

    // One story rule: when its event happens (or once all its events have), its changes happen.
    // Every change is optional; the defaults change nothing.
    public sealed class StoryRule
    {
        public string on = "";                                 // The event that runs it.
        public string[] when_all = Array.Empty<string>();      // Or: runs once all of these have happened.
        public string[] unless = Array.Empty<string>();        // If any of these happened, it's used up but does nothing.
        public string[] lines = Array.Empty<string>();         // Story lines to show, in order.
        public StoryVoice voice = StoryVoice.her;
        public string objective = null;                        // The new goal; null leaves it, "" clears it.
        public ChapterBeat? beat = null;                       // Moves the story to this beat.
        public Character? control = null;                      // The player now controls this character.
        public bool? switching = null;                         // Whether the player may switch characters.
        public bool? follow = null;                            // Whether Bodhi follows her.
        public bool? fetch = null;                             // Whether the ball game is on.
        public double schedule_seconds = 0;                    // After this long...
        public string schedule_event = "";                     // ...this event fires ("" for none).
        public bool reunion = false;                           // Starts the reunion.
        public bool complete = false;                          // Ends the chapter.
        public double fear_change = 0;                         // Added to his fear (kept between 0 and 1).
        public double closeness_change = 0;                    // Added to his need to stay close.
        public double? care_hunger = null;                     // Sets his hunger, if given.
        public double? care_dirt = null;                       // Sets how muddy he is, if given.
        public int? care_food = null;                          // Sets the meals in the bag, if given.
        public CutsceneChange cutscene = CutsceneChange.none;
        public string cutscene_skip_to = "";                   // The event Skip fires to reach the scene's end.
        public string[] remove = Array.Empty<string>();        // Solid things that go away (a collapsed stair).
        public string[] add = Array.Empty<string>();           // Solid things that appear (a plank laid across).
    }

    // A whole chapter.
    public sealed class ChapterScript
    {
        public string id = "";                                 // For example "the_storm".
        public string title = "";
        public string eyebrow = "";                            // The small line above the title.
        public string place = "";
        public double start_fear = 0.05;                       // How he feels as it starts.
        public double start_closeness = 0.3;
        public double? start_hunger = null;                    // Care to start from; null carries on from before.
        public double? start_dirt = null;
        public int? start_food = null;
        public bool has_ball = false;                          // A ball to play fetch with.
        public ChapterThing[] things = Array.Empty<ChapterThing>();
        public ChapterZone[] zones = Array.Empty<ChapterZone>();
        public StoryRule[] rules = Array.Empty<StoryRule>();
        public string[] aftermath = Array.Empty<string>();     // Lines for the screen after the chapter.
    }

    // Fills the placeholders in a story line. Bodhi is he: {them} is "him", {their} is "his".
    public static class StoryText
    {
        // Returns the line with {dog}, {them} and {their} filled in. Missing text gives "".
        public static string personalize(string text, string dog_name)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            return text.Replace("{dog}", dog_name ?? "").Replace("{them}", "him").Replace("{their}", "his");
        }
    }
}
