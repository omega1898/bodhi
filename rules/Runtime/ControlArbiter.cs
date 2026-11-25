// Purpose: decides who the player controls (spec 3.3, R5.1 and R5.2). It offers a switch when
// the active character is stuck and the other one isn't, or any time in sections where free
// switching is on; a story beat can force one. A timer never switches. During the camera blend
// (under a second) button presses are held, then given to the new character once each, in order.
using System;
using System.Collections.Generic;

namespace Bodhi.Rules
{
    // A button press, as the game's input code reports it.
    public enum PlayerInput
    {
        move_left,
        move_right,
        jump,
        act,   // Use, pick up, pull.
        call,  // Her whistle, his bark.
    }

    public class ControlArbiter
    {
        // How long the switch's camera blend takes. Spec R5.2 asks for under 1 second.
        public const double blend_seconds = 0.8;

        // Most presses held during one blend. A flood beyond this is dropped and counted.
        public const int held_input_limit = 8;

        readonly bool free_switching;
        readonly Queue<PlayerInput> held_inputs = new Queue<PlayerInput>();
        Character blend_target;
        double blend_elapsed_seconds = 0.0;

        // Who the player controls now. During a blend it's still the old character.
        public Character active_character { get; private set; }

        // True while the camera blends from one character to the other.
        public bool is_blending { get; private set; }

        // Called with each press and who it goes to. The Unity side attaches its movement code here.
        public Action<Character, PlayerInput> on_input;

        // Presses dropped because too many arrived during one blend.
        public int dropped_input_count { get; private set; }

        // Requests refused as broken input (a character or press that doesn't exist).
        public int refused_input_count { get; private set; }

        // Starts with this character in control. `free_switching` allows a switch at any time.
        public ControlArbiter(Character starting_character, bool free_switching)
        {
            active_character = Enum.IsDefined(typeof(Character), starting_character) ? starting_character : Character.human;
            this.free_switching = free_switching;
        }

        // True when a switch should be offered: the active character has no move and the other has one.
        public bool switch_offered(bool active_has_move, bool other_has_move)
        {
            return !is_blending && !active_has_move && other_has_move;
        }

        // The same, worked out from a puzzle in play ("has a move" comes from the puzzle data).
        public bool switch_offered(PuzzleRun run)
        {
            if (run == null || run.is_solved)
            {
                return false;
            }
            bool active_has_move = run.available_actions(active_character).Length > 0;
            bool other_has_move = run.available_actions(other_than(active_character)).Length > 0;
            return switch_offered(active_has_move, other_has_move);
        }

        // The player taps the other character's portrait. Allowed when a switch is offered, or
        // any time with free switching. Returns true if the blend started.
        public bool request_switch(bool active_has_move, bool other_has_move)
        {
            if (is_blending)
            {
                return false;
            }
            if (!free_switching && !switch_offered(active_has_move, other_has_move))
            {
                return false;
            }
            start_blend(other_than(active_character));
            return true;
        }

        // A story beat hands control to a character, whether or not anyone is stuck.
        public bool force_switch(Character to_character)
        {
            if (!Enum.IsDefined(typeof(Character), to_character))
            {
                refused_input_count += 1;
                return false;
            }
            if (is_blending || to_character == active_character)
            {
                return false;
            }
            start_blend(to_character);
            return true;
        }

        // The game reports a button press. It goes to the active character now, or is held
        // during a blend and given to the new character when the blend ends.
        public void submit_input(PlayerInput press)
        {
            if (!Enum.IsDefined(typeof(PlayerInput), press))
            {
                refused_input_count += 1;
                return;
            }
            if (!is_blending)
            {
                on_input?.Invoke(active_character, press);
                return;
            }
            if (held_inputs.Count >= held_input_limit)
            {
                dropped_input_count += 1;
                return;
            }
            held_inputs.Enqueue(press);
        }

        // Moves the blend on by one frame. Time on its own never starts a switch.
        public void update(double delta_seconds)
        {
            if (!is_blending || !double.IsFinite(delta_seconds) || delta_seconds < 0.0)
            {
                return;
            }
            blend_elapsed_seconds += delta_seconds;
            if (blend_elapsed_seconds < blend_seconds)
            {
                return;
            }
            is_blending = false;
            active_character = blend_target;
            while (held_inputs.Count > 0)
            {
                on_input?.Invoke(active_character, held_inputs.Dequeue());
            }
        }

        // Starts the camera blend toward a character.
        void start_blend(Character to_character)
        {
            blend_target = to_character;
            blend_elapsed_seconds = 0.0;
            is_blending = true;
        }

        // The other one of the two.
        static Character other_than(Character who)
        {
            return who == Character.human ? Character.dog : Character.human;
        }
    }
}
