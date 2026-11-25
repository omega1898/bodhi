// Purpose: the player's buttons, read the same way from a keyboard, a game controller or the
// on-screen touch buttons. The keyboard and controller bindings are made in code with Unity's
// Input System, so there is no input asset file to set up. The touch buttons (GameHud) press
// and release the same buttons through press_touch and release_touch.
//
// Keyboard: A/D or arrows to walk, Space/W/Up to jump, E/Enter to use, S/Down to kneel or
// sniff, Q to call, Tab to switch, Esc to skip a film, P to pause.
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bodhi.Game
{
    // The buttons the game has.
    public enum GameButton
    {
        left,
        right,
        jump,
        use,
        low,     // Kneel (her) or sniff (him).
        call,    // She whistles, he barks.
        switch_character,
        skip,
        pause,
    }

    public class PlayerControls
    {
        readonly InputAction walk;
        readonly InputAction[] buttons;
        readonly bool[] touch_held = new bool[9];
        readonly bool[] touch_pressed = new bool[9];  // Pressed since the last end_frame.

        // Makes the bindings and turns them on.
        public PlayerControls()
        {
            walk = new InputAction("walk", InputActionType.Value);
            walk.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            walk.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            walk.AddBinding("<Gamepad>/leftStick/x");
            walk.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/dpad/left").With("Positive", "<Gamepad>/dpad/right");
            buttons = new InputAction[9];
            buttons[(int)GameButton.left] = new InputAction("left", InputActionType.Button);
            buttons[(int)GameButton.right] = new InputAction("right", InputActionType.Button);
            buttons[(int)GameButton.jump] = button("jump", "<Keyboard>/space", "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/buttonSouth");
            buttons[(int)GameButton.use] = button("use", "<Keyboard>/e", "<Keyboard>/enter", "<Gamepad>/buttonWest");
            buttons[(int)GameButton.low] = button("low", "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/buttonEast");
            buttons[(int)GameButton.call] = button("call", "<Keyboard>/q", "<Gamepad>/buttonNorth");
            buttons[(int)GameButton.switch_character] = button("switch", "<Keyboard>/tab", "<Gamepad>/rightShoulder");
            buttons[(int)GameButton.skip] = button("skip", "<Keyboard>/escape", "<Gamepad>/select");
            buttons[(int)GameButton.pause] = button("pause", "<Keyboard>/p", "<Gamepad>/start");
            walk.Enable();
            foreach (var action in buttons)
            {
                action.Enable();
            }
        }

        // Which way the player pushes: -1 left, 0 none, 1 right.
        public int direction
        {
            get
            {
                float push = walk.ReadValue<float>();
                int touch = (touch_held[(int)GameButton.right] ? 1 : 0) - (touch_held[(int)GameButton.left] ? 1 : 0);
                if (touch != 0)
                {
                    return touch;
                }
                return push > 0.35f ? 1 : push < -0.35f ? -1 : 0;
            }
        }

        // True while this button is held.
        public bool held(GameButton which)
        {
            return touch_held[(int)which] || buttons[(int)which].IsPressed();
        }

        // True on the frame this button went down.
        public bool pressed(GameButton which)
        {
            return touch_pressed[(int)which] || buttons[(int)which].WasPressedThisFrame();
        }

        // A touch button went down.
        public void press_touch(GameButton which)
        {
            touch_held[(int)which] = true;
            touch_pressed[(int)which] = true;
        }

        // A touch button came up.
        public void release_touch(GameButton which)
        {
            touch_held[(int)which] = false;
        }

        // Call at the end of each frame: forgets this frame's touch presses.
        public void end_frame()
        {
            for (int index = 0; index < touch_pressed.Length; index++)
            {
                touch_pressed[index] = false;
            }
        }

        // Turns the bindings off (when the scene closes).
        public void dispose()
        {
            walk.Dispose();
            foreach (var action in buttons)
            {
                action.Dispose();
            }
        }

        // A button bound to these keys and controller buttons.
        static InputAction button(string name, params string[] paths)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var path in paths)
            {
                action.AddBinding(path);
            }
            return action;
        }
    }
}
