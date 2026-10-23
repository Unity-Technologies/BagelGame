using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // A bagel lying flat with its base on the origin: a lumpy, puffy ring of dough with a flatter
    // bottom, sprinkled with toppings. Submesh 0 is the dough; each topping picks its own submesh.
    [Serializable]
    public class BagelMeshGenerator : ProceduralMeshGenerator
    {
        [Serializable]
        public struct Topping
        {
            [Min(0)] public int count;
            [Tooltip("Length, thickness and width, in meters.")]
            public Vector3 size;
            [Tooltip("Corners around each piece: 4 for a grain, 8 for a smooth seed.")]
            [Range(3, 12)] public int sides;
            [Min(1)] public int submesh;
        }

        [Header("Shape")]
        [Min(0f)] public float radius = 0.64f;
        [Min(0f)] public float holeRadius = 0.16f;
        [Min(0f)] public float height = 0.36f;
        [Tooltip("2 is a round tube; more is puffier and squarer.")]
        [Range(2f, 6f)] public float puffiness = 2.6f;
        [Tooltip("How flat the bottom is, so it sits on a counter.")]
        [Range(0f, 1f)] public float flatBottom = 0.6f;

        [Header("Detail")]
        [Range(0f, 0.2f)] public float lumpiness = 0.08f;
        public int seed = 1;
        [Min(3)] public int segments = 48;
        [Min(3)] public int sides = 24;

        [Header("Toppings")]
        [Tooltip("How far down the sides the toppings go: 0 is just the top, 1 is all over.")]
        [Range(0f, 1f)] public float toppingSpread = 0.4f;
        public List<Topping> toppings = new();

        const float k_Epsilon = 0.0005f;

        public override void Build(MeshBuilder builder)
        {
            int first = builder.vertexCount;
            for (int row = 0; row <= sides; ++row)
            {
                float phi = 2f * Mathf.PI * row / sides;
                for (int column = 0; column <= segments; ++column)
                {
                    float theta = 2f * Mathf.PI * column / segments;
                    builder.AddVertex(Surface(theta, phi), Normal(theta, phi), new Vector2((float)column / segments * 4f, (float)row / sides));
                }
            }

            // Around the ring goes right and around the tube goes up, as seen from outside.
            builder.AddGrid(first, segments, sides);

            for (int i = 0; i < toppings.Count; ++i)
                AddTopping(builder, toppings[i], i);
        }

        // theta goes around the hole, phi around the tube: 0 faces out, pi / 2 faces up.
        Vector3 Surface(float theta, float phi)
        {
            float tubeCenter = (radius + holeRadius) * 0.5f;
            float tubeWidth = (radius - holeRadius) * 0.5f;
            float halfHeight = height * 0.5f;

            float cos = Mathf.Cos(phi);
            float sin = Mathf.Sin(phi);
            float across = Superellipse(cos, puffiness);
            float up = Superellipse(sin, sin < 0f ? puffiness + flatBottom * 6f : puffiness);

            // Lumps grow from the top and fade out before the bottom, so it stays flat.
            float lump = 1f + Lump(theta, phi) * Mathf.Clamp01(0.5f + sin);
            float distance = tubeCenter + tubeWidth * across * lump;
            return new Vector3(Mathf.Cos(theta) * distance, halfHeight + halfHeight * up * lump, Mathf.Sin(theta) * distance);
        }

        Vector3 Normal(float theta, float phi)
        {
            // The surface's slopes both ways: across them is out.
            var alongTheta = (Surface(theta + k_Epsilon, phi) - Surface(theta - k_Epsilon, phi)) / (2f * k_Epsilon);
            var alongPhi = (Surface(theta, phi + k_Epsilon) - Surface(theta, phi - k_Epsilon)) / (2f * k_Epsilon);
            return Vector3.Cross(alongPhi, alongTheta).normalized;
        }

        static float Superellipse(float t, float exponent) => Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(t), 2f / exponent);

        // A few waves with whole numbers of bumps around the ring and the tube, so the ends meet up.
        float Lump(float theta, float phi)
        {
            float sum = 0f;
            for (int i = 0; i < 4; ++i)
            {
                int aroundRing = 2 + i * 2 + Mathf.FloorToInt(Hash01(seed, i * 4) * 3f);
                int aroundTube = 1 + Mathf.FloorToInt(Hash01(seed, i * 4 + 1) * 3f);
                float ringPhase = Hash01(seed, i * 4 + 2) * 2f * Mathf.PI;
                float tubePhase = Hash01(seed, i * 4 + 3) * 2f * Mathf.PI;
                sum += Mathf.Sin(aroundRing * theta + ringPhase) * Mathf.Cos(aroundTube * phi + tubePhase) / (i + 1);
            }
            return sum * lumpiness * 0.5f;
        }

        void AddTopping(MeshBuilder builder, Topping topping, int toppingIndex)
        {
            builder.submesh = topping.submesh;
            int pieceSides = Mathf.Clamp(topping.sides, 3, 12);
            int toppingSeed = seed * 7919 + toppingIndex * 104729;
            int draw = 0;

            for (int placed = 0; placed < topping.count && draw < topping.count * 20; ++draw)
            {
                float theta = Hash01(toppingSeed, draw * 4) * 2f * Mathf.PI;
                float phi = Mathf.PI * 0.5f + (Hash01(toppingSeed, draw * 4 + 1) - 0.5f) * toppingSpread * 2f * Mathf.PI;

                // The outside of the ring has more room than the inside: keep the spacing even.
                var position = Surface(theta, phi);
                float distance = new Vector2(position.x, position.z).magnitude;
                if (Hash01(toppingSeed, draw * 4 + 2) * radius > distance)
                    continue;

                var normal = Normal(theta, phi);
                float yaw = Hash01(toppingSeed, draw * 4 + 3) * 2f * Mathf.PI;
                var aroundRing = Vector3.Cross(Vector3.up, normal).sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, normal).normalized : Vector3.right;
                var length = Quaternion.AngleAxis(yaw * Mathf.Rad2Deg, normal) * aroundRing;
                AddPiece(builder, position, normal, length, topping.size, pieceSides);
                ++placed;
            }

            builder.submesh = 0;
        }

        // A low dome on the dough, its rim sunk in far enough that no gap shows underneath.
        static void AddPiece(MeshBuilder builder, Vector3 position, Vector3 normal, Vector3 length, Vector3 size, int pieceSides)
        {
            var width = Vector3.Cross(length, normal);
            var half = size * 0.5f;
            var center = position - normal * (half.y * 0.4f);

            int top = builder.AddVertex(center + normal * half.y, normal, new Vector2(0.5f, 0.5f));
            int ring = builder.vertexCount;
            for (int i = 0; i < pieceSides; ++i)
            {
                float angle = 2f * Mathf.PI * i / pieceSides;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                var offset = length * (x * half.x) + width * (z * half.z);
                var pieceNormal = (length * (x / Mathf.Max(half.x, 0.0001f)) + width * (z / Mathf.Max(half.z, 0.0001f))).normalized;
                builder.AddVertex(center + offset, (pieceNormal * 0.4f + normal).normalized, new Vector2(0.5f + x * 0.5f, 0.5f + z * 0.5f));
            }

            // length and width are right and up as seen from above: the angle grows counter-clockwise.
            for (int i = 0; i < pieceSides; ++i)
            {
                int a = ring + i;
                int b = ring + (i + 1) % pieceSides;
                builder.AddTriangle(top, b, a);
            }
        }
    }
}
