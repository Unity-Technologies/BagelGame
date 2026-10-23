using System;
using UnityEngine;

namespace Bagel
{
    // The bagel's convex MeshCollider: a disc with a rounded rim, its axle along X. Physics keeps a
    // convex hull of at most 255 triangles, so it's built to fit: a ring of points around the rim for
    // each rim angle, and a flat face on each side (the hole never touches anything).
    [Serializable]
    public class BagelHullMeshGenerator : ProceduralMeshGenerator
    {
        public const int maxTriangles = 255;

        [Min(0f)] public float radius = 0.625f;
        [Min(0f)] public float thickness = 0.25f;
        [Tooltip("Angles around the rim's rounding, from its widest point: a ring of points for each.")]
        public float[] rimAngles = { -60f, -20f, 20f, 60f };
        [Min(3)] public int segments = 32;

        public int triangleCount => (rimAngles.Length - 1) * segments * 2 + (segments - 2) * 2;

        public override void Build(MeshBuilder builder)
        {
            // From one side to the other; around the rim goes right as seen from outside.
            int rings = rimAngles.Length;
            int first = builder.vertexCount;
            for (int ring = 0; ring < rings; ++ring)
            {
                float angle = rimAngles[ring] * Mathf.Deg2Rad;
                for (int i = 0; i <= segments; ++i)
                {
                    var direction = Direction(i);
                    var normal = new Vector3(Mathf.Sin(angle), direction.y * Mathf.Cos(angle), direction.z * Mathf.Cos(angle));
                    builder.AddVertex(RimPoint(angle, i), normal, new Vector2((float)i / segments, (float)ring / (rings - 1)));
                }
            }
            builder.AddGrid(first, segments, rings - 1);

            AddSide(builder, rimAngles[0] * Mathf.Deg2Rad, Vector3.left);
            AddSide(builder, rimAngles[rings - 1] * Mathf.Deg2Rad, Vector3.right);
        }

        // The rounding is a half circle across the thickness, placed so the widest ring has the radius.
        Vector3 RimPoint(float angle, int i)
        {
            float rounding = thickness * 0.5f;
            float widest = 0f;
            foreach (var rimAngle in rimAngles)
                widest = Mathf.Max(widest, Mathf.Cos(rimAngle * Mathf.Deg2Rad));
            float distance = radius - rounding * (widest - Mathf.Cos(angle));
            return new Vector3(Mathf.Sin(angle) * rounding, 0f, 0f) + Direction(i) * distance;
        }

        // A flat face over the first or last ring, fanned out from its first point.
        void AddSide(MeshBuilder builder, float angle, Vector3 normal)
        {
            int first = builder.vertexCount;
            for (int i = 0; i < segments; ++i)
                builder.AddVertex(RimPoint(angle, i), normal, new Vector2(0.5f + Direction(i).z * 0.5f, 0.5f + Direction(i).y * 0.5f));

            // Seen from +X, the angle grows counter-clockwise.
            for (int i = 1; i < segments - 1; ++i)
            {
                if (normal.x > 0f)
                    builder.AddTriangle(first, first + i + 1, first + i);
                else
                    builder.AddTriangle(first, first + i, first + i + 1);
            }
        }

        Vector3 Direction(int i)
        {
            float angle = 2f * Mathf.PI * i / segments;
            return new Vector3(0f, Mathf.Sin(angle), Mathf.Cos(angle));
        }
    }
}
