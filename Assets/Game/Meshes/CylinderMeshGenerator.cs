using System;
using UnityEngine;

namespace Bagel
{
    // An upright cylinder centered on the origin, the built-in cylinder's size by default.
    [Serializable]
    public class CylinderMeshGenerator : ProceduralMeshGenerator
    {
        [Min(0f)] public float radius = 0.5f;
        [Min(0f)] public float height = 2f;
        [Min(3)] public int segments = 20;
        public bool caps = true;

        public override void Build(MeshBuilder builder)
        {
            float half = height * 0.5f;

            // Around the side, the angle grows to the right as seen from outside.
            int first = builder.vertexCount;
            for (int row = 0; row <= 1; ++row)
            {
                for (int i = 0; i <= segments; ++i)
                {
                    var direction = Direction(i);
                    builder.AddVertex(direction * radius + Vector3.up * (row == 0 ? -half : half), direction, new Vector2((float)i / segments, row));
                }
            }
            builder.AddGrid(first, segments, 1);

            if (!caps)
                return;

            AddCap(builder, half, Vector3.up);
            AddCap(builder, -half, Vector3.down);
        }

        void AddCap(MeshBuilder builder, float y, Vector3 normal)
        {
            int center = builder.AddVertex(Vector3.up * y, normal, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= segments; ++i)
            {
                var direction = Direction(i);
                builder.AddVertex(direction * radius + Vector3.up * y, normal, new Vector2(0.5f + direction.x * 0.5f, 0.5f + direction.z * 0.5f));
            }

            // Seen from above, the angle grows counter-clockwise.
            for (int i = 0; i < segments; ++i)
            {
                if (normal.y > 0f)
                    builder.AddTriangle(center, center + i + 2, center + i + 1);
                else
                    builder.AddTriangle(center, center + i + 1, center + i + 2);
            }
        }

        Vector3 Direction(int i)
        {
            float angle = 2f * Mathf.PI * i / segments;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
