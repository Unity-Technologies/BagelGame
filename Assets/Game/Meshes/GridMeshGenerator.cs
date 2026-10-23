using System;
using UnityEngine;

namespace Bagel
{
    // A flat, subdivided rectangle: the quad, the ground plane, the sidewalk and the hedge.
    [Serializable]
    public class GridMeshGenerator : ProceduralMeshGenerator
    {
        public enum Facing { Up, Down, Left, Right, Forward, Back }

        public Vector2 size = Vector2.one;
        public Vector2Int segments = Vector2Int.one;
        public Facing facing = Facing.Back;
        [Tooltip("Meters per UV tile, centered on the grid. 0 stretches the UVs from 0 to 1.")]
        [Min(0f)] public float tileSize;
        [Tooltip("Adds a back face, so it can be seen from both sides.")]
        public bool twoSided;

        public override void Build(MeshBuilder builder)
        {
            // As seen from the visible side: up the screen, then right.
            var (right, up) = facing switch
            {
                Facing.Up => (Vector3.right, Vector3.forward),
                Facing.Down => (Vector3.left, Vector3.forward),
                Facing.Left => (Vector3.back, Vector3.up),
                Facing.Right => (Vector3.forward, Vector3.up),
                Facing.Forward => (Vector3.left, Vector3.up),
                _ => (Vector3.right, Vector3.up),
            };

            var uvSize = tileSize > 0f ? size / tileSize : Vector2.one;
            var uvOffset = tileSize > 0f ? -uvSize * 0.5f : Vector2.zero;
            builder.AddPlane(Vector3.zero, right, up, size, segments, uvSize, uvOffset);
            if (twoSided)
                builder.AddPlane(Vector3.zero, -right, up, size, segments, uvSize, uvOffset);
        }
    }
}
