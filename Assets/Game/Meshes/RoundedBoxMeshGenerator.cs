using System;
using UnityEngine;

namespace Bagel
{
    // A box with every edge rounded, centered on the origin: butter, sugar cubes, cookbooks, a butter
    // dish's lid (rounded a lot), shelves.
    [Serializable]
    public class RoundedBoxMeshGenerator : ProceduralMeshGenerator
    {
        public Vector3 size = Vector3.one;
        [Min(0f)] public float radius = 0.1f;
        [Min(1)] public int roundingSegments = 3;

        public override void Build(MeshBuilder builder) => builder.AddRoundedBox(Vector3.zero, size, radius, roundingSegments);
    }
}
