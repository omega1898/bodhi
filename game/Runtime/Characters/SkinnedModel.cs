// Purpose: puts a proper 3D model (made in Blender, with a skeleton and skin that bends smoothly)
// on a character whose movement is worked out by one of the code-built figures (DogFigure,
// HumanFigure). The figure keeps doing all the movement maths, hidden, and every frame this
// points each of the model's bones the same way as the matching part of the figure. So all the
// walking, galloping, tail wagging and ear swinging carries over to the real model unchanged.
// Animators call this "retargeting".
//
// How a bone is pointed: when the model is first set up (standing in its rest pose), we note
// which way along the bone it points ("along", toward its child bone) and which way is the
// character's right in the bone's own terms ("across"). Each frame we turn the bone so "along"
// points from the figure's matching joint to the next one, and "across" lines up with that
// figure part's right.
using System.Collections.Generic;
using UnityEngine;

namespace Bodhi.Game
{
    public class SkinnedModel
    {
        // One bone of the model, and the two points on the figure it should point between.
        class Link
        {
            public Transform bone;
            public Transform from;            // The figure's joint where the bone starts.
            public Transform to;              // The figure's joint where it ends...
            public Vector3 end_offset;        // ...or this far along `from`'s own axes, if `to` is empty.
            public Vector3 along, across;     // In the bone's own terms, noted in the rest pose.
        }

        readonly Transform model;
        readonly Transform figure_root;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly List<Link> links = new List<Link>();
        Transform hips;
        Transform hips_follows;
        Vector3 hips_offset;
        Quaternion facing_fix = Quaternion.identity;  // Turns a model made facing the other way.

        // The model's root, which follows the figure's root.
        public Transform root => model;

        // Makes a copy of `prefab` (the imported model) beside `figure_root`, paints it with
        // `material`, and turns it round if it was made facing backwards.
        public SkinnedModel(GameObject prefab, Transform figure_root, Material material)
        {
            this.figure_root = figure_root;
            model = Object.Instantiate(prefab, figure_root.parent).transform;
            model.name = prefab.name + " (model)";
            model.localPosition = figure_root.localPosition;
            model.localRotation = figure_root.localRotation;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                bones[bone.name] = bone;
            }
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var materials = new Material[skin.sharedMaterials.Length];
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }
                skin.sharedMaterials = materials;
                skin.updateWhenOffscreen = true;  // Its bounds move as it runs; never cut it off by mistake.
                skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        // Turns the model round if its `front` bone is behind its `back` bone (a model can come in
        // facing backwards, depending on how the 3D program and Unity each define "forward").
        // Call before any link().
        public void face_forward(Transform front, Transform back)
        {
            if (front == null || back == null)
            {
                return;
            }
            if (model.InverseTransformPoint(front.position).z < model.InverseTransformPoint(back.position).z)
            {
                facing_fix = Quaternion.Euler(0, 180, 0);
                model.localRotation = figure_root.localRotation * facing_fix;
            }
        }

        // The model's bone called `name` (Blender adds ".L"/".R" for left and right), or null.
        public Transform bone(string name)
        {
            return bones.TryGetValue(name, out var found) ? found : null;
        }

        // Of the two bones `name`.L and `name`.R, the one on the character's `side` (-1 left,
        // 1 right), worked out from where it actually sits, since programs disagree on which way
        // is left once a model is turned round on import.
        public Transform bone_on_side(string name, int side)
        {
            var left = bone(name + ".L");
            var right = bone(name + ".R");
            if (left == null || right == null)
            {
                return left ?? right;
            }
            float left_x = model.InverseTransformPoint(left.position).x;
            return (left_x < 0) == (side < 0) ? left : right;
        }

        // Says that the model's `bone`, which runs toward its child bone called `next` (a bone
        // can branch, like the hips into the spine, legs and tail, so the chain's next bone is
        // named), should point from the figure's `from` toward `to` (or, if `to` is left out,
        // toward a point `end_offset` along `from`'s own axes). Call these in order from the body
        // outward. A missing bone is skipped, so a model missing a part still works.
        public void link(Transform bone, string next, Transform from, Transform to, Vector3 end_offset = default)
        {
            if (bone == null || from == null)
            {
                return;
            }
            // Which way the bone points: toward its next bone; or, for a bone with no next bone
            // in line (a head whose only children are its ears), the way the figure's matching
            // part points right now, since both stand in nearly the same pose when set up.
            var child = bone.Find(next);
            var figure_end = to != null ? to.position : from.TransformPoint(end_offset);
            var pointing = child != null ? child.position - bone.position : figure_end - from.position;
            if (pointing.sqrMagnitude < 1e-8)
            {
                return;
            }
            links.Add(new Link
            {
                bone = bone,
                from = from,
                to = to,
                end_offset = end_offset,
                along = Quaternion.Inverse(bone.rotation) * pointing.normalized,
                across = Quaternion.Inverse(bone.rotation) * figure_root.right,
            });
        }

        // Says the model's `bone` (its hips) moves up and down with the figure's `follows`, for
        // the bob of each step and sitting down.
        public void hips_follow(Transform bone, Transform follows)
        {
            hips = bone;
            hips_follows = follows;
            if (bone != null && follows != null)
            {
                hips_offset = figure_root.InverseTransformPoint(bone.position) - figure_root.InverseTransformPoint(follows.position);
            }
        }

        // Moves the model to match the figure: the root to the figure's root, the hips to its hips,
        // then every linked bone pointed the way its figure part points. Call after the figure
        // has posed itself for this frame.
        public void follow()
        {
            model.localPosition = figure_root.localPosition;
            model.localRotation = figure_root.localRotation * facing_fix;
            if (hips != null && hips_follows != null)
            {
                hips.position = figure_root.TransformPoint(figure_root.InverseTransformPoint(hips_follows.position) + hips_offset);
            }
            foreach (var link in links)
            {
                var end = link.to != null ? link.to.position : link.from.TransformPoint(link.end_offset);
                var pointing = end - link.from.position;
                if (pointing.sqrMagnitude < 1e-8)
                {
                    continue;
                }
                var right = Vector3.ProjectOnPlane(link.from.right, pointing);
                var turn_in_world = Quaternion.LookRotation(pointing.normalized, right.sqrMagnitude > 1e-6 ? right : Vector3.up);
                var turn_in_bone = Quaternion.LookRotation(link.along, link.across);
                link.bone.rotation = turn_in_world * Quaternion.Inverse(turn_in_bone);
            }
        }
    }
}
