using System;
using UnityEngine;

namespace Bagel
{
    // A flat gingham cloth facing up, centered on the origin: a check of columns x rows squares, each in one
    // of three submeshes, so three plain materials weave the pattern with no texture. Submesh 0 is where
    // no stripe crosses (the cloth's own color), 1 where one stripe does, 2 where two cross (the darkest).
    [Serializable]
    public class GinghamMeshGenerator : ProceduralMeshGenerator
    {
        public Vector2 size = new(10f, 10f);
        public Vector2Int checks = new(20, 20);

        public override void Build(MeshBuilder builder)
        {
            var cell = new Vector2(size.x / checks.x, size.y / checks.y);
            for (int z = 0; z < checks.y; ++z)
            {
                for (int x = 0; x < checks.x; ++x)
                {
                    builder.submesh = (x % 2) + (z % 2);
                    var center = new Vector3(-size.x * 0.5f + cell.x * (x + 0.5f), 0f, -size.y * 0.5f + cell.y * (z + 0.5f));
                    var uvOffset = new Vector2((float)x / checks.x, (float)z / checks.y);
                    builder.AddPlane(center, Vector3.right, Vector3.forward, cell, Vector2Int.one, new Vector2(1f / checks.x, 1f / checks.y), uvOffset);
                }
            }
        }
    }
}
