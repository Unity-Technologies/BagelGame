using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A kitchen gauge riding along with the bagel, drawn with Painter2D on a face styled in USS (its
    // enamel, rim and sheen are the element's background, border and gradient):
    // - Timer: a kitchen timer, its colored wedge running out from green to red: how fresh the bagel is.
    // - Speedometer: ticks and a red zone, and a needle that's its own element, turned with a style
    //   rotation: the needle moves every frame without drawing the gauge again.
    // - Seeds: a ring of seeds, one going out for every few toppings lost; the count in the middle.
    // value and maxValue bind (<DataBinding property="value" data-source-path="speed"/>). Its colors are
    // USS custom properties: --b-gauge-line, --b-gauge-accent, --b-gauge-good, --b-gauge-warn, --b-gauge-seed.
    [UxmlElement(libraryPath = "Bagel/Tracker")]
    public partial class BagelGauge : VisualElement
    {
        public enum Kind
        {
            Timer,
            Speedometer,
            Seeds
        }

        public static readonly string ussClassName = "b-gauge-dial";
        public static readonly string needleUssClassName = ussClassName + "__needle";
        public static readonly string hubUssClassName = ussClassName + "__hub";

        static readonly CustomStyleProperty<Color> s_LineProperty = new("--b-gauge-line");
        static readonly CustomStyleProperty<Color> s_AccentProperty = new("--b-gauge-accent");
        static readonly CustomStyleProperty<Color> s_GoodProperty = new("--b-gauge-good");
        static readonly CustomStyleProperty<Color> s_WarnProperty = new("--b-gauge-warn");
        static readonly CustomStyleProperty<Color> s_SeedProperty = new("--b-gauge-seed");

        const int k_SeedCount = 32;
        const float k_SweepDegrees = 270f;

        Kind m_Kind;
        float m_Value;
        float m_MaxValue = 1f;
        int m_DrawnStep = -1;

        Color m_Line = new(0.23f, 0.12f, 0.08f);
        Color m_Accent = new(0.87f, 0.28f, 0.17f);
        Color m_Good = new(0.42f, 0.66f, 0.33f);
        Color m_Warn = new(0.91f, 0.66f, 0.22f);
        Color m_Seed = new(0.98f, 0.95f, 0.87f);

        readonly VisualElement m_Needle;
        readonly VisualElement m_Hub;

        [UxmlAttribute]
        public Kind kind
        {
            get => m_Kind;
            set
            {
                m_Kind = value;
                m_Needle.style.display = value == Kind.Speedometer ? DisplayStyle.Flex : DisplayStyle.None;
                m_Hub.style.display = value == Kind.Seeds ? DisplayStyle.None : DisplayStyle.Flex;
                Refresh(force: true);
            }
        }

        [UxmlAttribute, CreateProperty]
        public float value
        {
            get => m_Value;
            set
            {
                if (m_Value == value)
                    return;
                m_Value = value;
                Refresh();
            }
        }

        [UxmlAttribute, CreateProperty]
        public float maxValue
        {
            get => m_MaxValue;
            set
            {
                if (m_MaxValue == value)
                    return;
                m_MaxValue = value;
                Refresh();
            }
        }

        // How full the gauge is, 0 to 1. A speed backwards shows as fast as forwards.
        public float fraction => m_MaxValue != 0f ? Mathf.Clamp01(Mathf.Abs(m_Value / m_MaxValue)) : 0f;

        public BagelGauge()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;

            m_Needle = new VisualElement { name = "needle", pickingMode = PickingMode.Ignore };
            m_Needle.AddToClassList(needleUssClassName);
            Add(m_Needle);
            m_Hub = new VisualElement { name = "hub", pickingMode = PickingMode.Ignore };
            m_Hub.AddToClassList(hubUssClassName);
            Add(m_Hub);
            kind = Kind.Timer;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(s_LineProperty, out var line))
                m_Line = line;
            if (style.TryGetValue(s_AccentProperty, out var accent))
                m_Accent = accent;
            if (style.TryGetValue(s_GoodProperty, out var good))
                m_Good = good;
            if (style.TryGetValue(s_WarnProperty, out var warn))
                m_Warn = warn;
            if (style.TryGetValue(s_SeedProperty, out var seed))
                m_Seed = seed;
            Refresh(force: true);
        }

        // The needle turns; the drawing is only made again when it would look different: the timer in
        // steps of 1%, the ring when a seed goes out.
        void Refresh(bool force = false)
        {
            var f = fraction;
            if (m_Kind == Kind.Speedometer)
                m_Needle.style.rotate = new Rotate(-k_SweepDegrees * 0.5f + k_SweepDegrees * f);

            var step = m_Kind switch
            {
                Kind.Timer => Mathf.CeilToInt(f * 100f),
                Kind.Seeds => Mathf.CeilToInt(f * k_SeedCount),
                _ => 0
            };
            if (force || step != m_DrawnStep)
            {
                m_DrawnStep = step;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (!(rect.width > 4f) || !(rect.height > 4f))
                return;

            var painter = context.painter2D;
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            switch (m_Kind)
            {
                case Kind.Timer:
                    DrawTimer(painter, center, radius);
                    break;
                case Kind.Speedometer:
                    DrawSpeedometer(painter, center, radius);
                    break;
                case Kind.Seeds:
                    DrawSeeds(painter, center, radius);
                    break;
            }
        }

        // Angles start at the top and go clockwise, like a clock's.
        static Vector2 Polar(Vector2 center, float radius, float degrees)
        {
            var radians = (degrees - 90f) * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        void Ticks(Painter2D painter, Vector2 center, float outer, float inner, float from, float sweep, int count, float width)
        {
            painter.strokeColor = m_Line;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            for (int i = 0; i < count; i++)
            {
                var angle = from + sweep * i / (count - 1);
                var isMajor = i % 2 == 0;
                painter.MoveTo(Polar(center, outer, angle));
                painter.LineTo(Polar(center, isMajor ? inner : (inner + outer) * 0.5f, angle));
            }
            painter.Stroke();
        }

        void DrawTimer(Painter2D painter, Vector2 center, float radius)
        {
            var f = fraction;
            var color = f > 0.5f ? Color.Lerp(m_Warn, m_Good, (f - 0.5f) * 2f) : Color.Lerp(m_Accent, m_Warn, f * 2f);

            // The time left: a wedge from the top, shrinking clockwise as the bagel gets older.
            if (f > 0f)
            {
                painter.fillColor = color;
                painter.BeginPath();
                painter.MoveTo(center);
                painter.Arc(center, radius * 0.72f, -90f, -90f + 360f * f);
                painter.ClosePath();
                painter.Fill();
            }

            Ticks(painter, center, radius * 0.94f, radius * 0.78f, 0f, 330f, 12, Mathf.Max(1.2f, radius * 0.05f));
        }

        void DrawSpeedometer(Painter2D painter, Vector2 center, float radius)
        {
            var from = -k_SweepDegrees * 0.5f;

            // The red zone: the last fifth.
            painter.strokeColor = m_Accent;
            painter.lineWidth = radius * 0.12f;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();
            painter.Arc(center, radius * 0.8f, from + k_SweepDegrees * 0.8f - 90f, from + k_SweepDegrees - 90f);
            painter.Stroke();

            Ticks(painter, center, radius * 0.92f, radius * 0.72f, from, k_SweepDegrees, 11, Mathf.Max(1.2f, radius * 0.05f));
        }

        void DrawSeeds(Painter2D painter, Vector2 center, float radius)
        {
            var lit = Mathf.CeilToInt(fraction * k_SeedCount);
            var ring = radius * 0.8f;
            var length = radius * 0.13f;
            for (int i = 0; i < k_SeedCount; i++)
            {
                var angle = 360f * i / k_SeedCount;
                var position = Polar(center, ring, angle);
                // A sesame seed lies along the ring.
                var along = Polar(Vector2.zero, length, angle + 90f);
                var isLit = i < lit;
                painter.strokeColor = isLit ? m_Line : new Color(m_Line.r, m_Line.g, m_Line.b, 0.18f);
                painter.lineWidth = radius * 0.11f;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(position - along * 0.5f);
                painter.LineTo(position + along * 0.5f);
                painter.Stroke();
                if (!isLit)
                    continue;
                painter.strokeColor = m_Seed;
                painter.lineWidth = radius * 0.06f;
                painter.BeginPath();
                painter.MoveTo(position - along * 0.4f);
                painter.LineTo(position + along * 0.4f);
                painter.Stroke();
            }
        }
    }
}
