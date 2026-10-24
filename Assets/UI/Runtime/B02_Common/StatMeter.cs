using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A stat as a row of pips, like the dots on a bakery's price tag: five pips, as many filled as the
    // value is high (0 to 1), the last one partly. Drawn with Painter2D in the element's color (its
    // outlines) and --b-meter-color (its fill). The value binds: <DataBinding property="value" .../>.
    [UxmlElement(libraryPath = "Bagel/Common")]
    public partial class StatMeter : VisualElement
    {
        public static readonly string ussClassName = "b-stat-meter";

        static readonly CustomStyleProperty<Color> s_FillProperty = new("--b-meter-color");

        float m_Value;
        int m_PipCount = 5;
        Color m_Fill = new(0.87f, 0.28f, 0.17f);

        [UxmlAttribute, CreateProperty]
        public float value
        {
            get => m_Value;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_Value, value))
                    return;
                m_Value = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public int pipCount
        {
            get => m_PipCount;
            set
            {
                m_PipCount = Mathf.Max(1, value);
                MarkDirtyRepaint();
            }
        }

        public StatMeter()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(s_FillProperty, out var fill))
                {
                    m_Fill = fill;
                    MarkDirtyRepaint();
                }
            });
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (!(rect.width > 2f) || !(rect.height > 2f))
                return;

            var painter = context.painter2D;
            var step = rect.width / m_PipCount;
            var radius = Mathf.Min(step * 0.38f, rect.height * 0.45f);
            var line = Mathf.Max(1f, radius * 0.28f);
            var filled = m_Value * m_PipCount;

            for (int i = 0; i < m_PipCount; i++)
            {
                var center = new Vector2(rect.xMin + step * (i + 0.5f), rect.center.y);
                var amount = Mathf.Clamp01(filled - i);

                // A partly filled pip fills from the bottom, like a glass.
                if (amount > 0f)
                {
                    painter.fillColor = m_Fill;
                    painter.BeginPath();
                    if (amount >= 1f)
                    {
                        painter.Arc(center, radius, 0f, 360f);
                    }
                    else
                    {
                        var level = Mathf.Lerp(radius, -radius, amount);
                        var half = Mathf.Acos(Mathf.Clamp(level / radius, -1f, 1f)) * Mathf.Rad2Deg;
                        painter.Arc(center, radius, 90f - half, 90f + half);
                    }
                    painter.ClosePath();
                    painter.Fill();
                }

                painter.strokeColor = resolvedStyle.color;
                painter.lineWidth = line;
                painter.BeginPath();
                painter.Arc(center, radius, 0f, 360f);
                painter.ClosePath();
                painter.Stroke();
            }
        }
    }
}
