using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // UI Component: a squishy press for any element, without subclassing it. Add it in UXML
    // (<Bagel.PressSquish pressed-scale="0.9"/>), in UI Builder, or with AddComponent from C#.
    [VisualElementComponent]
    public partial struct PressSquish
    {
        const long k_SubmitSquishMs = 120;

        [UxmlAttribute] public float pressedScale;

        [UxmlCreateInstanceMethod]
        internal static PressSquish CreateDefault() => new PressSquish { pressedScale = 0.9f };

        // TrickleDown: Buttons stop PointerDown in the bubble phase.
        [RegisterCallback(typeof(PointerDownEvent), CallbackOptions.TrickleDown)]
        static void OnPointerDown(ref PressSquish self, VisualElement owner, PointerDownEvent evt)
        {
            Squish(self, owner);
        }

        [RegisterCallback(typeof(PointerUpEvent), CallbackOptions.TrickleDown)]
        [RegisterCallback(typeof(PointerLeaveEvent))]
        [RegisterCallback(typeof(PointerCancelEvent))]
        [RegisterCallback(typeof(PointerCaptureOutEvent))]
        static void OnRelease(ref PressSquish self, VisualElement owner, EventBase evt)
        {
            owner.style.scale = StyleKeyword.Null;
        }

        // Gamepad and keyboard presses get a short squish too.
        [RegisterCallback(typeof(NavigationSubmitEvent), CallbackOptions.TrickleDown)]
        static void OnSubmit(ref PressSquish self, VisualElement owner, NavigationSubmitEvent evt)
        {
            Squish(self, owner);
            owner.schedule.Execute(() => owner.style.scale = StyleKeyword.Null).StartingIn(k_SubmitSquishMs);
        }

        [OnComponentRemoved]
        static void OnRemoved(ref PressSquish self, VisualElement owner)
        {
            owner.style.scale = StyleKeyword.Null;
        }

        static void Squish(in PressSquish self, VisualElement owner)
        {
            var scale = self.pressedScale > 0f ? self.pressedScale : 0.9f;
            owner.style.scale = new Scale(new Vector3(scale + 0.06f, scale, 1f));
        }
    }
}
