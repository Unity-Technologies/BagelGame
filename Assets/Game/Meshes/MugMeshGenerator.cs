using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // A coffee mug standing on the origin: a lathed cup with a thick wall, a rounded rim and a dark
    // coffee surface inside (submesh 1), and a handle swept along a half ring on its +X side.
    [Serializable]
    public class MugMeshGenerator : ProceduralMeshGenerator
    {
        [Min(0f)] public float radius = 0.45f;
        [Min(0f)] public float height = 1.1f;
        [Min(0f)] public float wall = 0.06f;
        [Tooltip("How full of coffee, from 0 (empty) to 1 (to the brim).")]
        [Range(0f, 1f)] public float coffee = 0.8f;
        [Min(3)] public int segments = 40;
        [Min(0f)] public float handleRadius = 0.3f;
        [Min(0f)] public float handleThickness = 0.08f;

        public override void Build(MeshBuilder builder)
        {
            var r = radius;
            var inner = radius - wall;
            var floor = wall * 1.5f;
            var level = Mathf.Lerp(floor, height - wall, coffee);

            // Up the outside, over the rim, down the inside to the coffee.
            var cup = new List<Vector2>
            {
                new(0f, 0f), new(r * 0.85f, 0f), new(r * 0.97f, 0.02f), new(r, 0.08f), new(r, height - wall * 0.5f),
                new(r - wall * 0.15f, height - wall * 0.1f), new(r - wall * 0.5f, height), new(inner + wall * 0.15f, height - wall * 0.1f),
                new(inner, height - wall * 0.5f), new(inner, level)
            };
            LatheMeshGenerator.AddLathe(builder, cup, segments, 50f);

            // The coffee: a flat disc in the cup.
            builder.submesh = 1;
            LatheMeshGenerator.AddLathe(builder, new List<Vector2> { new(inner, level), new(0f, level) }, segments, 50f);

            // The handle: a tube around a half ring standing on the +X side.
            builder.submesh = 0;
            AddHandle(builder, new Vector3(r - handleThickness * 0.3f, height * 0.52f, 0f));
        }

        void AddHandle(MeshBuilder builder, Vector3 center)
        {
            const int along = 16;
            const int around = 10;
            int first = builder.vertexCount;
            for (int i = 0; i <= along; ++i)
            {
                // From the top joint, out and down to the bottom joint: angles 90 to -90 around Z.
                var a = Mathf.Lerp(90f, -90f, (float)i / along) * Mathf.Deg2Rad;
                var spine = center + new Vector3(Mathf.Cos(a) * handleRadius, Mathf.Sin(a) * handleRadius * 1.15f, 0f);
                var outward = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                for (int j = 0; j <= around; ++j)
                {
                    var b = 2f * Mathf.PI * j / around;
                    var normal = outward * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    builder.AddVertex(spine + normal * handleThickness, normal, new Vector2((float)j / around, (float)i / along));
                }
            }
            // The rows run along the handle and the columns around it, wound to face out.
            for (int i = 0; i < along; ++i)
            {
                for (int j = 0; j < around; ++j)
                {
                    int v = first + i * (around + 1) + j;
                    builder.AddQuad(v, v + 1, v + around + 2, v + around + 1);
                }
            }
        }
    }
}
