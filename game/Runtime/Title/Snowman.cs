// Purpose: everything Em and Bodhi make or play with on the title screen: the snowman's three
// snowballs (rolling and growing as she pushes them, in her arms as she carries them, stacked
// once placed), its coal eyes and buttons, carrot nose and stick arms, the stick Bodhi fetches,
// and the snowball she throws for him. Where each thing is comes from SnowmanPlay; things held
// sit at Em's hands or in Bodhi's mouth, wherever those are this frame.
using UnityEngine;

namespace Bodhi.Game
{
    public class Snowman
    {
        static readonly Color snow = new Color(0.9f, 0.93f, 0.99f);
        static readonly Color coal = new Color(0.06f, 0.06f, 0.07f);
        static readonly Color carrot = new Color(0.93f, 0.45f, 0.12f);
        static readonly Color wood = new Color(0.3f, 0.2f, 0.12f);

        readonly Transform parent;
        readonly Transform bottom, middle, head, thrown;
        readonly GameObject eyes, nose, left_arm, right_arm, stick;

        // Builds every piece under `parent`, all hidden until the choreography shows them.
        public Snowman(Transform parent)
        {
            this.parent = parent;
            var snow_material = WorldLook.solid(rim: 0.15f, sparkle: 0.6f, wrap: 0.55f);
            var plain = WorldLook.solid(rim: 0.2f);
            var ball_mesh = make_ball_mesh();
            bottom = show_ball("bottom ball", ball_mesh, snow_material);
            middle = show_ball("middle ball", ball_mesh, snow_material);
            head = show_ball("head ball", ball_mesh, snow_material);
            thrown = show_ball("thrown snowball", ball_mesh, snow_material);

            // The face sits on the side toward the camera (-z).
            var face = new MeshKit();
            double eye_height = SnowmanPlay.head_height + 0.035;
            for (int side = -1; side <= 1; side += 2)
            {
                face.add_ball(new Vector3(side * 0.055f, (float)eye_height, -0.15f), Vector3.one * 0.02f, coal, 6);
            }
            for (int dot = 0; dot < 5; dot++)  // A coal smile.
            {
                float across = (dot - 2) * 0.03f;
                face.add_ball(new Vector3(across, (float)SnowmanPlay.head_height - 0.06f + Mathf.Abs(across) * 0.6f, -0.16f), Vector3.one * 0.011f, coal, 4);
            }
            for (int button = 0; button < 3; button++)
            {
                float height = (float)SnowmanPlay.middle_height + 0.12f - button * 0.1f;
                face.add_ball(new Vector3(0, height, -0.235f + Mathf.Abs(button - 1) * 0.012f), Vector3.one * 0.022f, coal, 6);
            }
            eyes = MeshKit.show("snowman face", face.build("face"), plain, parent, true);

            var carrot_kit = new MeshKit();
            carrot_kit.add_round_shape(new Vector3(0, (float)SnowmanPlay.head_height, -0.14f), Quaternion.Euler(-90, 0, 0), new[]
            {
                new MeshKit.Ring(0, 0.024f, carrot),
                new MeshKit.Ring(0.05f, 0.016f, carrot),
                new MeshKit.Ring(0.12f, 0, carrot),
            }, 8, Vector3.one);
            nose = MeshKit.show("carrot nose", carrot_kit.build("carrot"), plain, parent, true);

            left_arm = MeshKit.show("left stick arm", stick_mesh(-1), plain, parent, true);
            right_arm = MeshKit.show("right stick arm", stick_mesh(1), plain, parent, true);
            var branch = new MeshKit();
            branch.add_tube(new Vector3(-0.24f, 0, 0), new Vector3(0.24f, 0, 0), 0.016f, 0.011f, wood, wood, 6);
            branch.add_tube(new Vector3(0.08f, 0, 0), new Vector3(0.16f, 0, 0.06f), 0.009f, 0.006f, wood, wood, 5);
            stick = MeshKit.show("stick", branch.build("stick"), plain, parent, true);
        }

        // One stick arm, poking out of the middle ball to one side and up, with a twig hand.
        static Mesh stick_mesh(int side)
        {
            var kit = new MeshKit();
            var start = new Vector3(side * 0.18f, (float)SnowmanPlay.middle_height + 0.04f, 0);
            var end = start + new Vector3(side * 0.34f, 0.2f, -0.03f);
            kit.add_tube(start, end, 0.014f, 0.009f, wood, wood, 6);
            kit.add_tube(end - new Vector3(side * 0.06f, 0.035f, 0), end + new Vector3(side * 0.05f, 0.09f, -0.01f), 0.007f, 0.005f, wood, wood, 5);
            kit.add_tube(end, end + new Vector3(side * 0.1f, -0.02f, 0), 0.007f, 0.005f, wood, wood, 5);
            return kit.build("stick arm");
        }

        // A ball of snow one metre across, knobbly like packed snow; each snowball scales it.
        static Mesh make_ball_mesh()
        {
            var kit = new MeshKit();
            kit.add_ball(Vector3.zero, Vector3.one, snow, 14, 0.035f, 0.28f);
            return kit.build("snowball");
        }

        // One snowball object, hidden.
        Transform show_ball(string name, Mesh mesh, Material material)
        {
            var ball = MeshKit.show(name, mesh, material, parent, true).transform;
            ball.gameObject.SetActive(false);
            return ball;
        }

        // Puts everything where `frame` says, using Em's hands and Bodhi's mouth for held things.
        public void show(SnowPlayFrame frame, HumanFigure em, DogFigure bodhi)
        {
            var hands = parent.InverseTransformPoint(em.hands_middle);
            place_ball(bottom, frame.bottom, frame.em.facing, hands);
            place_ball(middle, frame.middle, frame.em.facing, hands);
            place_ball(head, frame.head, frame.em.facing, hands);
            eyes.SetActive(frame.has_eyes);
            nose.SetActive(frame.has_nose);
            left_arm.SetActive(frame.has_left_arm);
            right_arm.SetActive(frame.has_right_arm);

            stick.SetActive(frame.stick != StickPlace.gone);
            switch (frame.stick)
            {
                case StickPlace.ground:
                    stick.transform.localPosition = new Vector3((float)frame.stick_x, 0.02f, (float)frame.stick_z);
                    stick.transform.localRotation = Quaternion.Euler(0, 35, 0);
                    break;
                case StickPlace.mouth:
                    stick.transform.position = bodhi.mouth;
                    stick.transform.rotation = bodhi.mouth_turn;
                    break;
                case StickPlace.hand:
                    stick.transform.position = em.right_hand_point;
                    stick.transform.rotation = em.body.rotation * Quaternion.Euler(0, 70, 25);
                    break;
            }

            thrown.gameObject.SetActive(frame.thrown_in_hand || frame.thrown.state != BallState.hidden);
            thrown.localScale = Vector3.one * (float)frame.thrown.radius * 2;
            if (frame.thrown_in_hand)
            {
                thrown.position = em.right_hand_point;
            }
            else if (frame.thrown.state != BallState.hidden)
            {
                thrown.localPosition = new Vector3((float)frame.thrown.x, (float)frame.thrown_height, (float)frame.thrown.z);
            }
        }

        // Puts one snowball where its state says: rolling on the snow, in her arms, on its way
        // up, or stacked on the snowman.
        static void place_ball(Transform ball, Snowball state, double facing, Vector3 hands)
        {
            ball.gameObject.SetActive(state.state != BallState.hidden);
            float radius = (float)state.radius;
            ball.localScale = Vector3.one * radius * 2;
            var stacked = new Vector3(0, (float)state.stacked_height, 0);
            switch (state.state)
            {
                case BallState.ground:
                    ball.localPosition = new Vector3((float)state.x, radius * 0.93f, (float)state.z);
                    var across = Quaternion.Euler(0, (float)facing, 0) * Vector3.right;
                    ball.localRotation = Quaternion.AngleAxis((float)state.roll_degrees, across);
                    break;
                case BallState.held:
                    ball.localPosition = hands;
                    break;
                case BallState.placing:
                    float share = (float)state.share;
                    var arc = Vector3.up * Mathf.Sin(share * Mathf.PI) * 0.12f;
                    ball.localPosition = Vector3.Lerp(hands, stacked, share) + arc;
                    break;
                case BallState.stacked:
                    ball.localPosition = stacked;
                    break;
            }
        }
    }

    // Em's lantern, standing on the snow: the scene's one warm light close by (the art bible's
    // rule: cold blue dark with one warm light). The light itself is made by TitleWorld.
    public static class Lantern
    {
        // Builds the lantern standing at `spot` under `parent`.
        public static void build(Transform parent, Vector3 spot)
        {
            var metal = new Color(0.12f, 0.12f, 0.13f);
            var frame = new MeshKit();
            frame.add_box(spot + new Vector3(0, 0.02f, 0), new Vector3(0.16f, 0.04f, 0.16f), metal);
            frame.add_box(spot + new Vector3(0, 0.27f, 0), new Vector3(0.17f, 0.03f, 0.17f), metal);
            frame.add_round_shape(spot + new Vector3(0, 0.285f, 0), Quaternion.identity, new[]
            {
                new MeshKit.Ring(0, 0.09f, metal),
                new MeshKit.Ring(0.06f, 0.03f, metal),
                new MeshKit.Ring(0.07f, 0, metal),
            }, 8, Vector3.one);
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner % 2 == 0 ? -1 : 1) * 0.07f, z = (corner < 2 ? -1 : 1) * 0.07f;
                frame.add_tube(spot + new Vector3(x, 0.04f, z), spot + new Vector3(x, 0.26f, z), 0.008f, 0.008f, metal, metal, 4);
            }
            frame.add_tube(spot + new Vector3(-0.05f, 0.36f, 0), spot + new Vector3(0, 0.42f, 0), 0.007f, 0.007f, metal, metal, 4);
            frame.add_tube(spot + new Vector3(0, 0.42f, 0), spot + new Vector3(0.05f, 0.36f, 0), 0.007f, 0.007f, metal, metal, 4);
            MeshKit.show("lantern", frame.build("lantern"), WorldLook.solid(rim: 0.2f, gloss: 0.5f), parent, true);

            var glass = new MeshKit();
            glass.add_box(spot + new Vector3(0, 0.15f, 0), new Vector3(0.125f, 0.2f, 0.125f), Color.white);
            MeshKit.show("lantern glass", glass.build("glass"), WorldLook.glowing(PaletteColours.warm_amber, 4f), parent, false);
        }
    }
}
