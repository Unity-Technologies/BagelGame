using System;
using UnityEngine;

namespace Bagel
{
    // A folded paper card standing on its two edges, like a bakery's name card: two faces leaning against
    // each other at the fold on top. The front faces -Z (the side a world-space panel faces), and both
    // sides of the paper show. Its feet are at y = 0, its fold above the origin.
    [Serializable]
    public class TentCardMeshGenerator : ProceduralMeshGenerator
    {
        [Min(0f)] public float width = 1.6f;
        [Tooltip("Each face's height, from its foot to the fold.")]
        [Min(0f)] public float faceHeight = 1.5f;
        [Tooltip("How far each face leans back from upright, in degrees.")]
        [Range(0f, 80f)] public float lean = 16f;
        [Min(0f)] public float thickness = 0.01f;

        // Where the front face's middle is, and which way it faces, for whatever goes on it.
        public Vector3 frontCenter => new Vector3(0f, foldHeight * 0.5f, -halfSpread * 0.5f);
        public float foldHeight => faceHeight * Mathf.Cos(lean * Mathf.Deg2Rad);
        float halfSpread => faceHeight * Mathf.Sin(lean * Mathf.Deg2Rad);

        public override void Build(MeshBuilder builder)
        {
            var size = new Vector2(width, faceHeight);
            var one = Vector2Int.one;
            var front = (new Vector3(0f, foldHeight, halfSpread)).normalized;
            var back = (new Vector3(0f, foldHeight, -halfSpread)).normalized;
            var inward = Vector3.forward * thickness * 0.5f;

            // The front face: outside towards -Z, inside towards the fold's hollow.
            var frontMiddle = frontCenter;
            builder.AddPlane(frontMiddle, Vector3.right, front, size, one, Vector2.one, Vector2.zero);
            builder.AddPlane(frontMiddle + inward, Vector3.left, front, size, one, new Vector2(-1f, 1f), new Vector2(1f, 0f));

            // The back face, the same seen from behind.
            var backMiddle = new Vector3(0f, foldHeight * 0.5f, halfSpread * 0.5f);
            builder.AddPlane(backMiddle, Vector3.left, back, size, one, Vector2.one, Vector2.zero);
            builder.AddPlane(backMiddle - inward, Vector3.right, back, size, one, new Vector2(-1f, 1f), new Vector2(1f, 0f));
        }
    }
}
