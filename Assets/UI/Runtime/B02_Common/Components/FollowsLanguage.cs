using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // UI Component: the element, and everything inside it, follows the selected language's direction. In a
    // language written right to left (Arabic), its text runs right to left (languageDirection: the Advanced Text
    // Generator orders and shapes it) and b-rtl mirrors the layout in USS. Every screen's top pane follows the
    // language by itself (BagelPane); add it in UXML (<Bagel.FollowsLanguage/>) to UI with no pane.
    [VisualElementComponent]
    [NoAutoStaticsCleanup] // the followers leave the set as they detach
    public partial struct FollowsLanguage
    {
        public static readonly UniqueStyleString rtlUssClassName = new("b-rtl");

        static readonly HashSet<VisualElement> s_Followers = new();

        [OnComponentAdded]
        static void OnAdded(ref FollowsLanguage self, VisualElement owner)
        {
            if (owner.panel != null)
                Follow(owner);
        }

        [RegisterCallback(typeof(AttachToPanelEvent))]
        static void OnAttach(ref FollowsLanguage self, VisualElement owner, AttachToPanelEvent evt) => Follow(owner);

        [RegisterCallback(typeof(DetachFromPanelEvent))]
        static void OnDetach(ref FollowsLanguage self, VisualElement owner, DetachFromPanelEvent evt) => s_Followers.Remove(owner);

        [OnComponentRemoved]
        static void OnRemoved(ref FollowsLanguage self, VisualElement owner)
        {
            s_Followers.Remove(owner);
            Apply(owner, false);
        }

        static void Follow(VisualElement owner)
        {
            s_Followers.Add(owner);
            BagelLocalization.localeChanged -= ApplyToFollowers;
            BagelLocalization.localeChanged += ApplyToFollowers;
            Apply(owner, BagelLocalization.isRightToLeft);
        }

        static void ApplyToFollowers()
        {
            var rightToLeft = BagelLocalization.isRightToLeft;
            foreach (var follower in s_Followers)
                Apply(follower, rightToLeft);
        }

        // Sets the element's direction: right to left, or left to right.
        public static void Apply(VisualElement element, bool rightToLeft)
        {
            element.languageDirection = rightToLeft ? LanguageDirection.RTL : LanguageDirection.LTR;
            element.EnableInClassList(rtlUssClassName, rightToLeft);
        }
    }
}
