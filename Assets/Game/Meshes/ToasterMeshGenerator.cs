using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // The goal at the end of the track: a rounded toaster standing on its feet at the origin, its slots
    // running along Z and its lever and browning knob on the front (-Z). Submeshes: the shell, the
    // trim (lever, knob, feet), and the dark insides of the slots.
    [Serializable]
    public class ToasterMeshGenerator : ProceduralMeshGenerator
    {
        public const int shellSubmesh = 0;
        public const int trimSubmesh = 1;
        public const int slotSubmesh = 2;

        [Header("Body")]
        public Vector3 size = new(2.05f, 1.75f, 2.7f);
        [Min(0f)] public float cornerRadius = 0.35f;
        [Min(1)] public int roundingSegments = 4;
        [Min(0f)] public float feetHeight = 0.06f;

        [Header("Slots")]
        [Min(0)] public int slotCount = 2;
        [Tooltip("Across (X) and along (Z) the toaster.")]
        public Vector2 slotSize = new(0.3f, 1.8f);
        [Min(0f)] public float slotSpacing = 0.75f;
        [Min(0f)] public float slotDepth = 1f;

        [Header("Front")]
        public Vector3 leverSize = new(0.45f, 0.12f, 0.3f);
        [Tooltip("How far up the front the lever sits, from 0 (down) to 1 (top).")]
        [Range(0f, 1f)] public float leverPosition = 1f;
        [Min(0f)] public float knobRadius = 0.13f;
        [Min(0f)] public float knobLength = 0.12f;

        // Parts sink this far into the shell, so no gaps show where they meet.
        const float k_Overlap = 0.02f;

        public override void Build(MeshBuilder builder)
        {
            var half = size * 0.5f;
            float top = feetHeight + size.y;
            float front = -half.z;

            var openings = new List<Rect>();
            for (int i = 0; i < slotCount; ++i)
            {
                float x = (i - (slotCount - 1) * 0.5f) * slotSpacing;
                openings.Add(new Rect(x - slotSize.x * 0.5f, -slotSize.y * 0.5f, slotSize.x, slotSize.y));
            }

            builder.submesh = shellSubmesh;
            builder.AddRoundedBox(new Vector3(0f, feetHeight + half.y, 0f), size, cornerRadius, roundingSegments, openings);

            builder.submesh = slotSubmesh;
            foreach (var opening in openings)
                AddSlot(builder, opening, top, slotDepth);

            // The lever slides in a dark track on the left of the front, the knob turns on the right:
            // both inside the front's flat part.
            float leverX = -size.x * 0.15f;
            float trackBottom = feetHeight + size.y * 0.25f;
            float trackTop = feetHeight + size.y * 0.75f;
            builder.AddPlane(new Vector3(leverX, (trackBottom + trackTop) * 0.5f, front - 0.005f), Vector3.right, Vector3.up,
                new Vector2(leverSize.y * 0.6f, trackTop - trackBottom), Vector2Int.one, Vector2.one, Vector2.zero);

            builder.submesh = trimSubmesh;
            float leverY = Mathf.Lerp(trackBottom, trackTop, leverPosition) - leverSize.y * 0.5f;
            builder.AddRoundedBox(new Vector3(leverX, leverY, front - leverSize.z * 0.5f + k_Overlap), leverSize, leverSize.y * 0.4f, 2);

            var knob = new CylinderMeshGenerator { radius = knobRadius, height = knobLength + k_Overlap, segments = 16 };
            AddPart(builder, knob, new Vector3(size.x * 0.25f, feetHeight + size.y * 0.3f, front - knobLength * 0.5f + k_Overlap * 0.5f), Quaternion.Euler(-90f, 0f, 0f));

            var foot = new CylinderMeshGenerator { radius = 0.12f, height = feetHeight + k_Overlap, segments = 12 };
            var footInset = new Vector2(half.x - cornerRadius, half.z - cornerRadius) * 0.9f;
            for (int i = 0; i < 4; ++i)
            {
                var corner = new Vector3(i % 2 == 0 ? -footInset.x : footInset.x, (feetHeight + k_Overlap) * 0.5f, i < 2 ? -footInset.y : footInset.y);
                AddPart(builder, foot, corner, Quaternion.identity);
            }

            builder.submesh = shellSubmesh;
        }

        static void AddPart(MeshBuilder builder, ProceduralMeshGenerator part, Vector3 position, Quaternion rotation)
        {
            var transform = builder.transform;
            builder.transform = transform * Matrix4x4.TRS(position, rotation, Vector3.one);
            part.Build(builder);
            builder.transform = transform;
        }

        // Lines an opening in the top with four walls facing in and a floor.
        static void AddSlot(MeshBuilder builder, Rect opening, float top, float depth)
        {
            float y = top - depth * 0.5f;
            var center = opening.center;
            var wall = new Vector2(opening.height, depth);
            var end = new Vector2(opening.width, depth);
            builder.AddPlane(new Vector3(opening.xMin, y, center.y), Vector3.forward, Vector3.up, wall, Vector2Int.one, Vector2.one, Vector2.zero);
            builder.AddPlane(new Vector3(opening.xMax, y, center.y), Vector3.back, Vector3.up, wall, Vector2Int.one, Vector2.one, Vector2.zero);
            builder.AddPlane(new Vector3(center.x, y, opening.yMin), Vector3.left, Vector3.up, end, Vector2Int.one, Vector2.one, Vector2.zero);
            builder.AddPlane(new Vector3(center.x, y, opening.yMax), Vector3.right, Vector3.up, end, Vector2Int.one, Vector2.one, Vector2.zero);
            builder.AddPlane(new Vector3(center.x, top - depth, center.y), Vector3.right, Vector3.forward, opening.size, Vector2Int.one, Vector2.one, Vector2.zero);
        }
    }
}
