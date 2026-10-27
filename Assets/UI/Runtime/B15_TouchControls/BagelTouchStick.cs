using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // A thumb stick for touch screens. Its knob follows a finger pressed anywhere on it, out to its rim, and springs
    // back to the middle once the finger lifts. Its value is where the knob is, from (-1, -1) to (1, 1), with right
    // and up positive like a gamepad's stick. It keeps the finger that pressed it (each finger is a pointer of its
    // own, captured), so another can press something else meanwhile, and it never takes the focus: it's for touch.
    [UxmlElement(libraryPath = "Bagel/Common")]
    public partial class BagelTouchStick : VisualElement, INotifyValueChanged<Vector2>
    {
        public static readonly string ussClassName = "b-touch-stick";
        public static readonly string knobUssClassName = ussClassName + "__knob";
        public static readonly UniqueStyleString heldUssClassName = new(ussClassName + "--held");

        static readonly BindingId s_ValueProperty = nameof(value);

        readonly VisualElement m_Knob;
        int m_PointerId = PointerId.invalidPointerId;
        Vector2 m_Value;

        public VisualElement knob => m_Knob;
        public bool isHeld => m_PointerId != PointerId.invalidPointerId;

        [CreateProperty]
        public Vector2 value
        {
            get => m_Value;
            set
            {
                value = Vector2.ClampMagnitude(value, 1f);
                if (m_Value == value)
                    return;

                var previous = m_Value;
                SetValueWithoutNotify(value);
                using (var evt = ChangeEvent<Vector2>.GetPooled(previous, m_Value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
                NotifyPropertyChanged(s_ValueProperty);
            }
        }

        public BagelTouchStick()
        {
            AddToClassList(ussClassName);
            focusable = false;

            m_Knob = new VisualElement { name = "knob", pickingMode = PickingMode.Ignore };
            m_Knob.AddToClassList(knobUssClassName);
            Add(m_Knob);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            RegisterCallback<PointerCaptureOutEvent>(_ => Release());
            RegisterCallback<GeometryChangedEvent>(_ => PlaceKnob());
        }

        public void SetValueWithoutNotify(Vector2 newValue)
        {
            m_Value = Vector2.ClampMagnitude(newValue, 1f);
            PlaceKnob();
        }

        // Lets go of the finger, and the knob goes back to the middle: when the finger lifts, or the stick's hidden
        // from under it.
        public void Release()
        {
            if (!isHeld)
                return;

            var pointerId = m_PointerId;
            m_PointerId = PointerId.invalidPointerId;
            if (this.HasPointerCapture(pointerId))
                this.ReleasePointer(pointerId);
            RemoveFromClassList(heldUssClassName);
            value = Vector2.zero;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (isHeld)
                return;

            m_PointerId = evt.pointerId;
            this.CapturePointer(m_PointerId);
            AddToClassList(heldUssClassName);
            Follow(evt.localPosition);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId == m_PointerId)
                Follow(evt.localPosition);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == m_PointerId)
                Release();
        }

        void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == m_PointerId)
                Release();
        }

        // Where the finger is, from the middle, out of how far the knob goes.
        void Follow(Vector2 localPosition)
        {
            var reach = Reach();
            if (!(reach > 0f))
                return;

            var offset = (localPosition - layout.size * 0.5f) / reach;
            value = new Vector2(offset.x, -offset.y);
        }

        // The knob's middle goes out past the rim's inside, a quarter of the knob over it.
        float Reach() => layout.width * 0.5f - m_Knob.layout.width * 0.25f;

        void PlaceKnob()
        {
            var reach = Reach();
            if (float.IsNaN(reach))
                return;
            m_Knob.style.translate = new Translate(m_Value.x * reach, -m_Value.y * reach);
        }
    }
}
