using System;
using UnityEngine;

namespace Bagel
{
    // A restaurant's menu lying open: two boards of its cover, hinged at the spine, a block of pages on
    // each, and a ribbon to mark the page, coming out from between them at the bottom and over the edge.
    // The pages' tops are left clear for whatever's written on them.
    // Open along X, its top towards +Z, its pages facing up; it lies on y = 0, the spine on the origin.
    // Submeshes: 0 the cover, 1 the pages, 2 the ribbon.
    [Serializable]
    public class MenuBookMeshGenerator : ProceduralMeshGenerator
    {
        [Tooltip("One page's width (X) and height (Z).")]
        public Vector2 pageSize = new(3.9f, 5.5f);
        [Tooltip("How far the cover reaches past the pages, all round.")]
        [Min(0f)] public float coverMargin = 0.2f;
        [Min(0f)] public float coverThickness = 0.06f;
        [Min(0f)] public float pagesThickness = 0.08f;
        [Tooltip("The gap between the two blocks of pages, at the spine.")]
        [Min(0f)] public float gutter = 0.1f;
        [Min(0f)] public float ribbonWidth = 0.18f;
        [Tooltip("How far the ribbon hangs past the bottom of the pages.")]
        [Min(0f)] public float ribbonOverhang = 0.7f;

        // The pages' top: where whatever's written on them goes.
        public float pagesTop => coverThickness + pagesThickness;

        public override void Build(MeshBuilder builder)
        {
            var cover = new Vector3(pageSize.x + coverMargin, coverThickness, pageSize.y + coverMargin * 2f);
            var pages = new Vector3(pageSize.x, pagesThickness, pageSize.y);

            for (int side = -1; side <= 1; side += 2)
            {
                // A board of the cover, from the spine out.
                builder.submesh = 0;
                var boardCenter = new Vector3(side * (cover.x * 0.5f + gutter * 0.25f), cover.y * 0.5f, 0f);
                builder.AddRoundedBox(boardCenter, cover, Mathf.Min(0.05f, cover.y * 0.5f), 2);

                // Its pages, a little in from its edges: rounded, so where the two blocks meet they dip
                // into the fold.
                builder.submesh = 1;
                var pagesCenter = new Vector3(side * (pages.x * 0.5f + gutter * 0.5f), cover.y + pages.y * 0.5f, 0f);
                builder.AddRoundedBox(pagesCenter, pages, pages.y * 0.45f, 3);
            }

            // The ribbon, out of the fold: down the pages' bottom edge, over the cover's, and out onto the
            // table. Each strip faces up, or out towards -Z on the edges.
            builder.submesh = 2;
            var x = 0f;
            var lift = 0.004f;
            var edge = -pageSize.y * 0.5f;
            var coverEdge = edge - coverMargin;
            var one = new Vector2Int(1, 1);
            Strip(builder, new Vector3(x, coverThickness + pagesThickness * 0.5f, edge - lift), Vector3.up, pagesThickness, one);
            Strip(builder, new Vector3(x, coverThickness + lift, edge - coverMargin * 0.5f), Vector3.forward, coverMargin, one);
            Strip(builder, new Vector3(x, coverThickness * 0.5f, coverEdge - lift), Vector3.up, coverThickness, one);
            Strip(builder, new Vector3(x, lift, coverEdge - ribbonOverhang * 0.5f), Vector3.forward, ribbonOverhang, one);
        }

        void Strip(MeshBuilder builder, Vector3 center, Vector3 along, float length, Vector2Int segments)
        {
            builder.AddPlane(center, Vector3.right, along, new Vector2(ribbonWidth, length), segments, Vector2.one, Vector2.zero);
        }
    }
}
