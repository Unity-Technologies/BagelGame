using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A level from above, as a strip along the counter: the gingham cloth from behind the start to just past the
    // toaster, the bagel's start, every piece's footprint in its own color, and the toaster at the end. The start's on
    // the left whatever the language, the toaster on the right, the bagel's left at the top. Its height is the cloth's
    // width; it's as wide as the level is long, so a longer level is a longer strip.
    [UxmlElement]
    public partial class BagelLevelMap : VisualElement
    {
        public static readonly string ussClassName = "b-level-map";
        public static readonly string clothUssClassName = ussClassName + "__cloth";
        public static readonly string pieceUssClassName = ussClassName + "__piece";
        public static readonly string roundPieceUssClassName = pieceUssClassName + "--round";
        public static readonly string coursePieceUssClassName = pieceUssClassName + "--course";
        public static readonly string startUssClassName = ussClassName + "__start";
        public static readonly string toasterUssClassName = ussClassName + "__toaster";
        public static readonly string toasterSlotUssClassName = ussClassName + "__toaster-slot";

        // The gingham's colors, from the cloth's materials: the cloth, and its red stripes, crossing darker.
        static readonly Color s_ClothColor = new(0.95f, 0.93f, 0.88f);
        static readonly Color s_StripeColor = new(0.86f, 0.16f, 0.12f, 0.5f);
        static readonly Color s_GridColor = new(0.35f, 0.2f, 0.12f, 0.35f);

        BagelLevel m_Level;
        BagelLevelLibrary m_Library;
        int m_Version = -1;
        float m_Length = -1f;
        readonly VisualElement m_Cloth;
        readonly VisualElement m_Start;
        readonly VisualElement m_Toaster;
        readonly List<VisualElement> m_Pieces = new();

        // The meter lines over the checks, for placing things.
        [UxmlAttribute]
        public bool showGrid { get; set; }

        public BagelLevel level
        {
            get => m_Level;
            set
            {
                if (m_Level == value)
                    return;
                Listen(false);
                m_Level = value;
                Listen(panel != null);
                m_Version = -1;
                Refresh();
            }
        }

        public BagelLevelLibrary library
        {
            get => m_Library;
            set
            {
                if (m_Library == value)
                    return;
                m_Library = value;
                m_Version = -1;
                Refresh();
            }
        }

        // Pixels a meter on the counter: the cloth's width fills the height.
        public float pixelsPerMeter
        {
            get
            {
                var height = resolvedStyle.height;
                return float.IsNaN(height) || height <= 0f ? 0f : height / (BagelLevel.halfWidth * 2f);
            }
        }

        // The pieces' elements, as the level lists them.
        public IReadOnlyList<VisualElement> pieceElements => m_Pieces;
        public VisualElement toaster => m_Toaster;

        float clothLength => (m_Level != null ? m_Level.length : BagelLevel.newLength) + BagelLevel.clothBeyond - BagelLevel.clothStart;

        public BagelLevelMap()
        {
            AddToClassList(ussClassName);
            m_Cloth = new VisualElement { name = "cloth", pickingMode = PickingMode.Ignore };
            m_Cloth.AddToClassList(clothUssClassName);
            m_Cloth.generateVisualContent += DrawCloth;
            Add(m_Cloth);
            m_Start = new VisualElement { name = "start", pickingMode = PickingMode.Ignore };
            m_Start.AddToClassList(startUssClassName);
            Add(m_Start);
            m_Toaster = new VisualElement { name = "toaster", pickingMode = PickingMode.Ignore };
            m_Toaster.AddToClassList(toasterUssClassName);
            for (int i = 0; i < 2; i++)
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                slot.AddToClassList(toasterSlotUssClassName);
                m_Toaster.Add(slot);
            }
            Add(m_Toaster);

            RegisterCallback<GeometryChangedEvent>(_ => Layout());
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Listen(true);
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => Listen(false));
        }

        void Listen(bool listen)
        {
            if (m_Level == null)
                return;
            m_Level.changed -= Level_OnChanged;
            if (listen)
                m_Level.changed += Level_OnChanged;
        }

        void Level_OnChanged(BagelLevel level) => Refresh();

        // From the map's own pixels to the counter's meters (x across, y along), and back.
        public Vector2 ToCounter(Vector2 local)
        {
            var ppm = pixelsPerMeter;
            if (ppm <= 0f)
                return Vector2.zero;
            return new Vector2(local.y / ppm - BagelLevel.halfWidth, local.x / ppm + BagelLevel.clothStart);
        }

        public Vector2 ToMap(Vector2 counter)
        {
            var ppm = pixelsPerMeter;
            return new Vector2((counter.y - BagelLevel.clothStart) * ppm, (counter.x + BagelLevel.halfWidth) * ppm);
        }

        // Shown again if the level's changed since, or anyway (what was moved about on it goes back).
        public void Refresh(bool force = false)
        {
            var version = m_Level != null ? m_Level.version : -2;
            if (!force && version == m_Version && (m_Level == null || Mathf.Approximately(m_Length, m_Level.length)))
                return;
            m_Version = version;
            m_Length = m_Level != null ? m_Level.length : -1f;

            var count = m_Level != null ? m_Level.pieceCount : 0;
            while (m_Pieces.Count > count)
            {
                m_Pieces[^1].RemoveFromHierarchy();
                m_Pieces.RemoveAt(m_Pieces.Count - 1);
            }
            while (m_Pieces.Count < count)
            {
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.AddToClassList(pieceUssClassName);
                Insert(IndexOf(m_Toaster), element);
                m_Pieces.Add(element);
            }
            for (int i = 0; i < count; i++)
            {
                var piece = m_Level.pieces[i];
                var entry = m_Library != null ? m_Library.Find(piece.kind) : null;
                var element = m_Pieces[i];
                element.name = piece.kind;
                element.userData = i;
                element.EnableInClassList(roundPieceUssClassName, entry != null && entry.round);
                element.EnableInClassList(coursePieceUssClassName, entry != null && entry.course);
                element.style.backgroundColor = entry != null ? entry.color : Color.gray;
            }
            Layout();
        }

        void Layout()
        {
            var ppm = pixelsPerMeter;
            var width = clothLength * ppm;
            if (ppm <= 0f)
                return;
            if (!Mathf.Approximately(resolvedStyle.width, width))
                style.width = width;

            m_Cloth.MarkDirtyRepaint();
            Place(m_Start, Vector2.zero, Vector2.one * (BagelLevel.startRadius * 2f), 0f);
            Place(m_Toaster, new Vector2(0f, m_Level != null ? m_Level.length : BagelLevel.newLength), BagelLevel.toasterHalfSize * 2f, 0f);
            for (int i = 0; i < m_Pieces.Count && m_Level != null && i < m_Level.pieceCount; i++)
            {
                var piece = m_Level.pieces[i];
                var entry = m_Library != null ? m_Library.Find(piece.kind) : null;
                Place(m_Pieces[i], piece.position, entry != null ? entry.size : Vector2.one, piece.angle);
            }
        }

        // An element over a footprint: its middle on the counter, its size across and along, turned.
        public void Place(VisualElement element, Vector2 middle, Vector2 size, float angle)
        {
            var ppm = pixelsPerMeter;
            var center = ToMap(middle);
            var width = size.y * ppm;
            var height = size.x * ppm;
            element.style.left = center.x - width * 0.5f;
            element.style.top = center.y - height * 0.5f;
            element.style.width = width;
            element.style.height = height;
            element.style.rotate = new Rotate(angle);

            // Round things (and the start) are as round as they're big.
            var radius = element == m_Start || element.ClassListContains(roundPieceUssClassName) ? new StyleLength(Mathf.Min(width, height) * 0.5f) : new StyleLength(StyleKeyword.Null);
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
        }

        // The gingham, as it's woven: red stripes along every other half-meter one way and the other, darker where they
        // cross; and the meter lines, when they're shown.
        void DrawCloth(MeshGenerationContext context)
        {
            var ppm = pixelsPerMeter;
            var rect = m_Cloth.contentRect;
            if (ppm <= 0f || rect.width <= 0f)
                return;
            var painter = context.painter2D;
            painter.fillColor = s_ClothColor;
            Fill(painter, rect);

            // As on the counter: the first check at the cloth's corner is the plain one.
            var cell = BagelLevel.cellSize * ppm;
            painter.fillColor = s_StripeColor;
            for (var x = cell; x < rect.width; x += cell * 2f)
                Fill(painter, new Rect(x, 0f, Mathf.Min(cell, rect.width - x), rect.height));
            for (var y = cell; y < rect.height; y += cell * 2f)
                Fill(painter, new Rect(0f, y, rect.width, Mathf.Min(cell, rect.height - y)));

            if (!showGrid)
                return;
            painter.strokeColor = s_GridColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            // Every meter along from the start, and across from the middle.
            for (var z = Mathf.Ceil(BagelLevel.clothStart); z <= clothLength + BagelLevel.clothStart; z += 1f)
            {
                var x = ToMap(new Vector2(0f, z)).x;
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, rect.height));
            }
            for (var across = -Mathf.Floor(BagelLevel.halfWidth); across <= BagelLevel.halfWidth; across += 1f)
            {
                var y = ToMap(new Vector2(across, 0f)).y;
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(rect.width, y));
            }
            painter.Stroke();
        }

        static void Fill(Painter2D painter, Rect rect)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
