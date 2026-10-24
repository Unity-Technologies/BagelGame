using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The callout over the bagel, in screen space: "Ready..." then "Go!" when a run starts, the toppings
    // lost in a bad knock, and a warning when the bagel is going stale. Each one pops in (a UIAnimationClip)
    // and goes away by itself.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelCalloutManager : BagelPane
    {
        public static readonly UniqueStyleString hiddenUssClassName = new("b-callout--hidden");
        public static readonly UniqueStyleString popUssClassName = new("b-callout--pop");
        public static readonly UniqueStyleString warningUssClassName = new("b-callout--warning");

        const long k_ShowMs = 1200;
        const long k_ReadyMs = 700;
        const float k_StaleSoon = 0.25f;
        // A knock that sheds this many toppings at once gets a callout.
        const int k_BigLoss = 20;
        // Toppings falling within this long of each other count as one knock.
        const float k_KnockSeconds = 0.4f;

        Label m_Label;
        IVisualElementScheduledItem m_Hide;
        IVisualElementScheduledItem m_Go;

        int m_KnockStart;
        int m_LastToppings;
        float m_LastLossTime = float.NegativeInfinity;
        bool m_WarnedStale;

        public Label label => m_Label ??= this.Q<Label>("callout-label");

        // A message stays until the next one: for a preview (the Bagel All UI window, UI Builder).
        [UxmlAttribute]
        public bool stays { get; set; }

        // What it says now, or null when nothing shows.
        public string shownText => ClassListContains(hiddenUssClassName) ? null : label?.text;

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            pickingMode = PickingMode.Ignore;
            AddToClassList(hiddenUssClassName);
            m_Hide = schedule.Execute(() => AddToClassList(hiddenUssClassName));
            m_Hide.Pause();
            m_Go = schedule.Execute(() => Show(BagelLocalization.Get("toast.go", "Go!"), false));
            m_Go.Pause();
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            if (newState != null)
                StartWatching();
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.currentStatePath)
            {
                if (state.isPlaying)
                    StartWatching();
                else
                    Hide();
            }
            else if (property == BagelTrackerData.toppingsCountPath)
            {
                WatchToppings();
            }
            else if (property == BagelTrackerData.freshnessPath)
            {
                WatchFreshness();
            }
        }

        // A run starts: "Ready..." and, a moment later, "Go!".
        void StartWatching()
        {
            var tracker = state.tracker;
            m_LastToppings = m_KnockStart = tracker.toppingsCount;
            m_LastLossTime = float.NegativeInfinity;
            m_WarnedStale = false;

            if (!state.isPlaying || tracker.elapsed > 0f || panel == null)
                return;
            Show(BagelLocalization.Get("toast.ready", "Ready..."), false);
            m_Go?.ExecuteLater(k_ReadyMs);
        }

        void WatchToppings()
        {
            if (!state.isPlaying)
                return;

            var toppings = state.tracker.toppingsCount;
            var now = Time.time;
            if (toppings < m_LastToppings)
            {
                // A new knock, or more of the same one.
                if (now - m_LastLossTime > k_KnockSeconds)
                    m_KnockStart = m_LastToppings;
                m_LastLossTime = now;

                var lost = m_KnockStart - toppings;
                if (lost >= k_BigLoss)
                    Show(string.Format(BagelLocalization.culture, BagelLocalization.Get("toast.lost", "{0} toppings!"), "−" + lost.ToString("N0", BagelLocalization.culture)), true);
            }
            m_LastToppings = toppings;
        }

        void WatchFreshness()
        {
            if (!state.isPlaying || m_WarnedStale || state.tracker.freshness > k_StaleSoon)
                return;
            m_WarnedStale = true;
            Show(BagelLocalization.Get("toast.stale-soon", "Going stale!"), true);
        }

        public void Show(string text, bool warning)
        {
            if (label == null)
                return;

            label.text = text;
            EnableInClassList(warningUssClassName, warning);
            RemoveFromClassList(hiddenUssClassName);

            // A one-shot clip plays again when its class comes back a frame later.
            RemoveFromClassList(popUssClassName);
            schedule.Execute(() => AddToClassList(popUssClassName)).ExecuteLater(16);
            if (stays)
                m_Hide?.Pause();
            else
                m_Hide?.ExecuteLater(k_ShowMs);
        }

        public void Hide()
        {
            m_Go?.Pause();
            m_Hide?.Pause();
            AddToClassList(hiddenUssClassName);
        }
    }
}
