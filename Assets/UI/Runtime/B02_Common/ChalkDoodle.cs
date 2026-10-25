using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A little drawing on the board, drawn in code with Painter2D: a bagel, its steam, a sparkle, an
    // arrow, a word's underline. Every stroke wobbles like a hand drew it, the same way every time (the seed picks the
    // hand). It's drawn in the element's color, so chalk on the blackboard and ink on paper alike, and
    // its strokes are --b-doodle-width pixels wide (a USS number: --b-doodle-width: 3).
    [UxmlElement(libraryPath = "Bagel/Common")]
    public partial class ChalkDoodle : VisualElement
    {
        public enum Kind
        {
            Bagel,
            Steam,
            Sparkle,
            Arrow,
            Underline
        }

        public static readonly string ussClassName = "b-doodle";

        static readonly CustomStyleProperty<float> s_WidthProperty = new("--b-doodle-width");

        Kind m_Kind;
        int m_Seed = 1;
        float m_Width = 3f;
        readonly List<Vector2> m_Points = new();

        [UxmlAttribute]
        public Kind kind
        {
            get => m_Kind;
            set
            {
                m_Kind = value;
                MarkDirtyRepaint();
            }
        }

        // Which hand drew it: each seed wobbles its own way.
        [UxmlAttribute]
        public int seed
        {
            get => m_Seed;
            set
            {
                m_Seed = value;
                MarkDirtyRepaint();
            }
        }

        public ChalkDoodle()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_WidthProperty, out var width) && !Mathf.Approximately(width, m_Width))
            {
                m_Width = width;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (!(rect.width > 2f) || !(rect.height > 2f))
                return;

            var painter = context.painter2D;
            painter.strokeColor = resolvedStyle.color;
            painter.lineWidth = m_Width;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            var random = new System.Random(m_Seed * 7919 + (int)m_Kind);
            var size = Mathf.Min(rect.width, rect.height);
            var center = rect.center;
            switch (m_Kind)
            {
                case Kind.Bagel:
                    // A bagel at an angle: its crust, its hole, a few seeds, drawn twice like chalk goes over a line again.
                    for (int pass = 0; pass < 2; pass++)
                    {
                        Ellipse(painter, random, center, rect.width * 0.46f, rect.height * 0.3f, size * 0.025f);
                        Ellipse(painter, random, center + new Vector2(0f, -rect.height * 0.04f), rect.width * 0.14f, rect.height * 0.08f, size * 0.015f);
                    }
                    // Its side, below the top.
                    Arc(painter, random, center + new Vector2(0f, rect.height * 0.08f), rect.width * 0.46f, rect.height * 0.3f, 10f, 170f, size * 0.02f);
                    for (int i = 0; i < 7; i++)
                    {
                        var angle = (i / 7f) * Mathf.PI * 2f + (float)random.NextDouble() * 0.4f;
                        var radius = 0.62f + (float)random.NextDouble() * 0.2f;
                        var seed = center + new Vector2(Mathf.Cos(angle) * rect.width * 0.3f * radius, Mathf.Sin(angle) * rect.height * 0.19f * radius);
                        var slant = new Vector2(Mathf.Cos(angle + 1.4f), Mathf.Sin(angle + 1.4f)) * size * 0.035f;
                        Line(painter, random, seed - slant, seed + slant, 0f);
                    }
                    break;
                case Kind.Steam:
                    // Three curls rising: just out of the toaster.
                    for (int i = 0; i < 3; i++)
                    {
                        var x = rect.xMin + rect.width * (0.25f + 0.25f * i);
                        m_Points.Clear();
                        for (int step = 0; step <= 16; step++)
                        {
                            var t = step / 16f;
                            m_Points.Add(new Vector2(x + Mathf.Sin(t * Mathf.PI * 2.2f + i) * rect.width * 0.08f, rect.yMax - t * rect.height * (0.85f - 0.1f * (i % 2))));
                        }
                        Stroke(painter, random, m_Points, size * 0.015f);
                    }
                    break;
                case Kind.Sparkle:
                    // A four-pointed sparkle, with a little one beside it.
                    Sparkle(painter, random, center, size * 0.45f);
                    Sparkle(painter, random, center + new Vector2(size * 0.38f, -size * 0.32f), size * 0.15f);
                    break;
                case Kind.Arrow:
                    // A curvy arrow, pointing right.
                    m_Points.Clear();
                    for (int step = 0; step <= 16; step++)
                    {
                        var t = step / 16f;
                        m_Points.Add(new Vector2(rect.xMin + t * rect.width * 0.9f, center.y + Mathf.Sin(t * Mathf.PI) * rect.height * 0.3f - t * rect.height * 0.15f));
                    }
                    Stroke(painter, random, m_Points, size * 0.02f);
                    var tip = m_Points[m_Points.Count - 1];
                    var back = (m_Points[m_Points.Count - 3] - tip).normalized * size * 0.35f;
                    Line(painter, random, tip, tip + Rotate(back, 35f), size * 0.02f);
                    Line(painter, random, tip, tip + Rotate(back, -35f), size * 0.02f);
                    break;
                case Kind.Underline:
                    // A quick stroke the width of the element, a little uphill and not quite straight.
                    m_Points.Clear();
                    for (int step = 0; step <= 20; step++)
                    {
                        var t = step / 20f;
                        m_Points.Add(new Vector2(rect.xMin + t * rect.width, center.y + rect.height * (0.2f - 0.4f * t) + Mathf.Sin(t * Mathf.PI * 1.5f) * rect.height * 0.15f));
                    }
                    Stroke(painter, random, m_Points, Mathf.Min(rect.height * 0.08f, 1f));
                    break;
            }
        }

        void Ellipse(Painter2D painter, System.Random random, Vector2 center, float rx, float ry, float wobble) =>
            Arc(painter, random, center, rx, ry, 0f, 372f, wobble);

        // Part of an ellipse, in degrees: a hand's line, a little off and overshooting where it started.
        void Arc(Painter2D painter, System.Random random, Vector2 center, float rx, float ry, float from, float to, float wobble)
        {
            m_Points.Clear();
            var start = from + (float)random.NextDouble() * 40f;
            var steps = 28;
            for (int step = 0; step <= steps; step++)
            {
                var angle = (start + (to - from) * step / steps) * Mathf.Deg2Rad;
                m_Points.Add(center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry));
            }
            Stroke(painter, random, m_Points, wobble);
        }

        void Sparkle(Painter2D painter, System.Random random, Vector2 center, float radius)
        {
            Line(painter, random, center + new Vector2(0f, -radius), center + new Vector2(0f, radius), radius * 0.05f);
            Line(painter, random, center + new Vector2(-radius * 0.8f, 0f), center + new Vector2(radius * 0.8f, 0f), radius * 0.05f);
            Line(painter, random, center + new Vector2(-radius * 0.35f, -radius * 0.35f), center + new Vector2(radius * 0.35f, radius * 0.35f), radius * 0.05f);
            Line(painter, random, center + new Vector2(radius * 0.35f, -radius * 0.35f), center + new Vector2(-radius * 0.35f, radius * 0.35f), radius * 0.05f);
        }

        void Line(Painter2D painter, System.Random random, Vector2 from, Vector2 to, float wobble)
        {
            m_Points.Clear();
            m_Points.Add(from);
            m_Points.Add(Vector2.Lerp(from, to, 0.5f));
            m_Points.Add(to);
            Stroke(painter, random, m_Points, wobble);
        }

        // One stroke through the points, each nudged a little: the wobble of a hand.
        static void Stroke(Painter2D painter, System.Random random, List<Vector2> points, float wobble)
        {
            painter.BeginPath();
            for (int i = 0; i < points.Count; i++)
            {
                var nudge = new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 2f * wobble;
                if (i == 0)
                    painter.MoveTo(points[i] + nudge);
                else
                    painter.LineTo(points[i] + nudge);
            }
            painter.Stroke();
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
