using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A kitchen gauge riding along with the bagel, drawn with Painter2D on a face styled in USS (its
    // enamel, rim and sheen are the element's background, border and gradient). Each says what it shows
    // without a word:
    // - Timer: a kitchen timer, its colored wedge running out from green to red, around a bagel that
    //   steams while it's fresh and goes grey-green as it goes stale.
    // - Speedometer: ticks and a red zone, and a needle that's its own element, turned with a style
    //   rotation: the needle moves every frame without drawing the gauge again.
    // - Seeds: a ring of seeds, one going out for every few toppings lost; the count in the middle.
    // - Spin: a circling arrow that goes further round the faster the bagel spins, around a little bagel
    //   that spins along (its own element, turned with a style rotation like the needle).
    // - Pain: a comic "pow" burst that grows bigger, spikier and redder the harder the last knock was.
    // value and maxValue bind (<DataBinding property="value" data-source-path="speed"/>). Its colors are
    // USS custom properties: --b-gauge-line, --b-gauge-accent, --b-gauge-good, --b-gauge-warn, --b-gauge-seed.
    [UxmlElement(libraryPath = "Bagel/Tracker")]
    public partial class BagelGauge : VisualElement
    {
        public enum Kind
        {
            Timer,
            Speedometer,
            Seeds,
            Spin,
            Pain
        }

        public static readonly string ussClassName = "b-gauge-dial";
        public static readonly string needleUssClassName = ussClassName + "__needle";
        public static readonly string hubUssClassName = ussClassName + "__hub";
        public static readonly string spinnerUssClassName = ussClassName + "__spinner";

        static readonly CustomStyleProperty<Color> s_LineProperty = new("--b-gauge-line");
        static readonly CustomStyleProperty<Color> s_AccentProperty = new("--b-gauge-accent");
        static readonly CustomStyleProperty<Color> s_GoodProperty = new("--b-gauge-good");
        static readonly CustomStyleProperty<Color> s_WarnProperty = new("--b-gauge-warn");
        static readonly CustomStyleProperty<Color> s_SeedProperty = new("--b-gauge-seed");

        const int k_SeedCount = 32;
        const float k_SweepDegrees = 270f;
        const float k_SpinSweepDegrees = 300f;
        // How fast the little bagel turns when the spin is at its most.
        const float k_SpinnerDegreesPerSecond = 720f;

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
        readonly VisualElement m_Spinner;
        float m_SpinnerAngle;

        [UxmlAttribute]
        public Kind kind
        {
            get => m_Kind;
            set
            {
                m_Kind = value;
                m_Needle.style.display = value == Kind.Speedometer ? DisplayStyle.Flex : DisplayStyle.None;
                m_Hub.style.display = value == Kind.Speedometer ? DisplayStyle.Flex : DisplayStyle.None;
                m_Spinner.style.display = value == Kind.Spin ? DisplayStyle.Flex : DisplayStyle.None;
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
            m_Spinner = new VisualElement { name = "spinner", pickingMode = PickingMode.Ignore };
            m_Spinner.AddToClassList(spinnerUssClassName);
            m_Spinner.generateVisualContent += DrawSpinner;
            Add(m_Spinner);
            kind = Kind.Timer;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            schedule.Execute(TurnSpinner).Every(0);
        }

        // The little bagel of the spin gauge turns as fast as the bagel spins: a rotation, nothing drawn again.
        void TurnSpinner(TimerState timer)
        {
            if (m_Kind != Kind.Spin || panel == null)
                return;
            m_SpinnerAngle = Mathf.Repeat(m_SpinnerAngle + fraction * k_SpinnerDegreesPerSecond * timer.deltaTime / 1000f, 360f);
            m_Spinner.style.rotate = new Rotate(m_SpinnerAngle);
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
            m_Spinner.MarkDirtyRepaint();
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
                Kind.Spin => Mathf.CeilToInt(f * 60f),
                Kind.Pain => Mathf.CeilToInt(f * 30f),
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
                case Kind.Spin:
                    DrawSpin(painter, center, radius);
                    break;
                case Kind.Pain:
                    DrawPain(painter, center, radius);
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

            // The bagel itself, golden while fresh and grey-green when stale, steaming while there's time left.
            var crust = Color.Lerp(new Color(0.55f, 0.6f, 0.45f), new Color(0.86f, 0.6f, 0.27f), f);
            var bagel = center + new Vector2(0f, radius * 0.08f);
            Bagel(painter, bagel, radius * 0.3f, crust);
            if (f > 0.4f)
            {
                var alpha = Mathf.InverseLerp(0.4f, 1f, f);
                painter.strokeColor = new Color(m_Seed.r, m_Seed.g, m_Seed.b, alpha);
                painter.lineWidth = Mathf.Max(1f, radius * 0.06f);
                painter.lineCap = LineCap.Round;
                for (int i = -1; i <= 1; i += 2)
                {
                    var x = bagel.x + i * radius * 0.1f;
                    var bottom = bagel.y - radius * 0.3f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, bottom));
                    painter.BezierCurveTo(new Vector2(x - radius * 0.1f, bottom - radius * 0.12f), new Vector2(x + radius * 0.1f, bottom - radius * 0.2f), new Vector2(x, bottom - radius * 0.32f));
                    painter.Stroke();
                }
            }
        }

        // A bagel seen from above: a ring of crust around its hole, the gauge's face showing through the hole.
        void Bagel(Painter2D painter, Vector2 center, float radius, Color crust)
        {
            painter.strokeColor = crust;
            painter.lineWidth = radius * 0.62f;
            painter.BeginPath();
            painter.Arc(center, radius * 0.69f, 0f, 360f);
            painter.ClosePath();
            painter.Stroke();
            painter.strokeColor = new Color(m_Line.r, m_Line.g, m_Line.b, 0.6f);
            painter.lineWidth = Mathf.Max(0.8f, radius * 0.08f);
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Stroke();
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

        void DrawSpin(Painter2D painter, Vector2 center, float radius)
        {
            var ring = radius * 0.78f;
            var width = radius * 0.13f;

            // The way round, faint, then the arrow as far round as the spin goes.
            painter.strokeColor = new Color(m_Line.r, m_Line.g, m_Line.b, 0.15f);
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(center, ring, -90f, -90f + k_SpinSweepDegrees);
            painter.Stroke();

            var sweep = Mathf.Max(24f, k_SpinSweepDegrees * fraction);
            painter.strokeColor = Color.Lerp(m_Line, m_Accent, fraction);
            painter.BeginPath();
            painter.Arc(center, ring, -90f, -90f + sweep);
            painter.Stroke();

            // Its head, pointing on round.
            var tip = Polar(center, ring, sweep + 14f);
            var back = Polar(center, ring, sweep - 6f);
            var across = Polar(center, ring + width * 1.4f, sweep - 6f) - back;
            painter.fillColor = painter.strokeColor;
            painter.BeginPath();
            painter.MoveTo(tip);
            painter.LineTo(back + across);
            painter.LineTo(back - across);
            painter.ClosePath();
            painter.Fill();
        }

        // The spin gauge's little bagel, a few seeds on its crust so its turning shows.
        void DrawSpinner(MeshGenerationContext context)
        {
            var rect = m_Spinner.contentRect;
            if (!(rect.width > 2f))
                return;
            var painter = context.painter2D;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            Bagel(painter, rect.center, radius * 0.95f, new Color(0.86f, 0.6f, 0.27f));
            painter.fillColor = m_Seed;
            for (int i = 0; i < 3; i++)
            {
                painter.BeginPath();
                painter.Arc(Polar(rect.center, radius * 0.66f, i * 120f), radius * 0.13f, 0f, 360f);
                painter.Fill();
            }
        }

        void DrawPain(Painter2D painter, Vector2 center, float radius)
        {
            var f = fraction;

            // A burst with more points the harder the knock, a little one even at rest.
            var points = 8 + Mathf.RoundToInt(f * 6f);
            var outer = radius * Mathf.Lerp(0.38f, 0.86f, f);
            var inner = outer * Mathf.Lerp(0.72f, 0.45f, f);
            var turn = 360f / points;
            painter.fillColor = f > 0f ? Color.Lerp(m_Warn, m_Accent, f) : new Color(m_Line.r, m_Line.g, m_Line.b, 0.15f);
            painter.strokeColor = m_Line;
            painter.lineWidth = Mathf.Max(1f, radius * 0.05f);
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (int i = 0; i < points; i++)
            {
                // Uneven points, like an inked pow, the same unevenness every time.
                var wobble = 1f + 0.12f * Mathf.Sin(i * 2.7f);
                var spike = Polar(center, outer * wobble, i * turn);
                var notch = Polar(center, inner, i * turn + turn * 0.5f);
                if (i == 0)
                    painter.MoveTo(spike);
                else
                    painter.LineTo(spike);
                painter.LineTo(notch);
            }
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();

            // Hard knocks crack it: lines from the middle out.
            if (f > 0.5f)
            {
                painter.lineWidth = Mathf.Max(1f, radius * 0.06f);
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                for (int i = 0; i < 3; i++)
                {
                    var angle = 40f + i * 120f;
                    painter.MoveTo(Polar(center, inner * 0.15f, angle));
                    painter.LineTo(Polar(center, inner * 0.6f, angle + 12f));
                    painter.LineTo(Polar(center, inner * 0.85f, angle - 6f));
                }
                painter.Stroke();
            }
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
