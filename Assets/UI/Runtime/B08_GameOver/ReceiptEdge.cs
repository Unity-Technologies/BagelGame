using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The torn bottom edge of a till receipt: a row of paper teeth, drawn with Painter2D in the element's
    // color (the paper's, from USS). It sits under the receipt, so the paper looks torn off the roll.
    [UxmlElement(libraryPath = "Bagel/Common")]
    public partial class ReceiptEdge : VisualElement
    {
        public static readonly string ussClassName = "b-receipt-edge";

        float m_ToothWidth = 12f;

        // How wide each tooth is, in pixels.
        [UxmlAttribute]
        public float toothWidth
        {
            get => m_ToothWidth;
            set
            {
                m_ToothWidth = Mathf.Max(2f, value);
                MarkDirtyRepaint();
            }
        }

        public ReceiptEdge()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (!(rect.width > 2f) || !(rect.height > 1f))
                return;

            var painter = context.painter2D;
            painter.fillColor = resolvedStyle.color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            var teeth = Mathf.Max(1, Mathf.RoundToInt(rect.width / m_ToothWidth));
            var width = rect.width / teeth;
            for (int i = 0; i < teeth; i++)
            {
                painter.LineTo(new Vector2(rect.xMin + width * (i + 0.5f), rect.yMax));
                painter.LineTo(new Vector2(rect.xMin + width * (i + 1f), rect.yMin));
            }
            painter.ClosePath();
            painter.Fill();
        }
    }
}
