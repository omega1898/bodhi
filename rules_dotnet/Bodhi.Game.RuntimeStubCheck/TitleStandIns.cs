// Purpose: a stand-in for the 3D title scene (game/Runtime/Title/TitleWorld.cs) with the same
// public shape, so the stand-in compile check can still check TitleScreens, which uses it. The
// real TitleWorld is left out of this check (see the .csproj) and compiled by real Unity instead.
using UnityEngine;

namespace Bodhi.Game
{
    public class TitleWorld
    {
        // Same as the real one: builds the scene under `parent`, hidden.
        public TitleWorld(Transform parent, Camera game_camera)
        {
        }

        // Same as the real one: shows or hides the scene.
        public void show(bool visible)
        {
        }

        // Same as the real one: moves the scene on to a moment of the opening.
        public void update(double seconds, OpeningFrame opening)
        {
        }
    }
}
