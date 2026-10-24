using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // A shape turned on a lathe: a profile, (radius, height) points from the middle of the bottom out and
    // up, spun around the upright axis. Plates, mugs, jars, salt shakers and bowls are all profiles. The
    // outside is on the right of the profile as it goes, so going up its outer side and back down its
    // inner side makes a thin-walled cup. A corner sharper than sharpAngle gets a crease; gentler ones
    // stay smooth.
    [Serializable]
    public class LatheMeshGenerator : ProceduralMeshGenerator
    {
        public List<Vector2> profile = new()
        {
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 1f)
        };
        [Min(3)] public int segments = 32;
        [Range(0f, 180f)] public float sharpAngle = 40f;

        public override void Build(MeshBuilder builder) => AddLathe(builder, profile, segments, sharpAngle);

        // The lathe for other generators too: a mug is a lathe and a handle.
        public static void AddLathe(MeshBuilder builder, IReadOnlyList<Vector2> profile, int segments, float sharpAngle)
        {
            if (profile == null || profile.Count < 2)
                return;

            var length = 0f;
            var total = 0f;
            for (int i = 1; i < profile.Count; ++i)
                total += Vector2.Distance(profile[i - 1], profile[i]);
            total = Mathf.Max(total, 1e-5f);

            // Each piece of the profile is a ring of quads, its own two rows of vertices, so a crease
            // between two pieces just takes each piece's own normal.
            for (int i = 0; i + 1 < profile.Count; ++i)
            {
                var a = profile[i];
                var b = profile[i + 1];
                var pieceLength = Vector2.Distance(a, b);
                if (pieceLength < 1e-6f)
                    continue;

                var normalA = JointNormal(profile, i, i, sharpAngle);
                var normalB = JointNormal(profile, i + 1, i, sharpAngle);
                var vA = length / total;
                length += pieceLength;
                var vB = length / total;

                int first = builder.vertexCount;
                AddRing(builder, a, normalA, vA, segments);
                AddRing(builder, b, normalB, vB, segments);
                builder.AddGrid(first, segments, 1);
            }
        }

        // The outward normal of piece i: the right of its direction, in (radius, height).
        static Vector2 PieceNormal(IReadOnlyList<Vector2> profile, int piece)
        {
            var direction = profile[piece + 1] - profile[piece];
            return new Vector2(direction.y, -direction.x).normalized;
        }

        // The normal at a profile point, for the piece it belongs to: smooth across a gentle corner,
        // the piece's own across a sharp one.
        static Vector2 JointNormal(IReadOnlyList<Vector2> profile, int point, int piece, float sharpAngle)
        {
            var own = PieceNormal(profile, piece);
            var other = point == piece ? piece - 1 : piece + 1;
            if (other < 0 || other + 1 >= profile.Count)
                return own;
            if ((profile[other + 1] - profile[other]).sqrMagnitude < 1e-12f)
                return own;

            var neighbor = PieceNormal(profile, other);
            return Vector2.Angle(own, neighbor) > sharpAngle ? own : (own + neighbor).normalized;
        }

        static void AddRing(MeshBuilder builder, Vector2 point, Vector2 normal, float v, int segments)
        {
            // Around, the angle grows to the right as seen from outside (like the cylinder's).
            for (int j = 0; j <= segments; ++j)
            {
                var angle = 2f * Mathf.PI * j / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                builder.AddVertex(direction * point.x + Vector3.up * point.y, direction * normal.x + Vector3.up * normal.y, new Vector2((float)j / segments, v));
            }
        }
    }
}
