// Purpose: one on-screen touch button (GameHud makes them). It presses a game button in
// PlayerControls while a finger is on it and lets go when the finger lifts or slides off.
// It has a file of its own because Unity expects each MonoBehaviour in a file of the same name.
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bodhi.Game
{
    // A touch button: tells PlayerControls while a finger is on it.
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public PlayerControls controls;
        public GameButton button;

        // A finger came down on it.
        public void OnPointerDown(PointerEventData data)
        {
            controls?.press_touch(button);
        }

        // The finger lifted.
        public void OnPointerUp(PointerEventData data)
        {
            controls?.release_touch(button);
        }

        // The finger slid off it: let go, so nobody walks forever.
        public void OnPointerExit(PointerEventData data)
        {
            controls?.release_touch(button);
        }

        // Let go if the button goes away mid-press.
        void OnDisable()
        {
            controls?.release_touch(button);
        }
    }
}
