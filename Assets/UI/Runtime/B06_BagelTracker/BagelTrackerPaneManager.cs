using System;
using UnityEngine.UIElements;

namespace Bagel
{
    // Root of the gauges that ride along with the bagel. Everything inside binds to tracker.*, except
    // the toppings count: it changes every frame while toppings fall off, so it's written with SetText
    // from a stack buffer instead of allocating a new string each time.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelTrackerPaneManager : BagelPane
    {
        BagelGauge m_ToppingsGauge;
        BagelGauge m_FreshnessGauge;
        BagelGauge m_SpeedGauge;
        Label m_ToppingsCount;
        ProgressBar m_SpinBar;
        ProgressBar m_PainBar;

        public BagelGauge toppingsGauge => m_ToppingsGauge ??= this.Q<BagelGauge>("toppings-gauge");
        public BagelGauge freshnessGauge => m_FreshnessGauge ??= this.Q<BagelGauge>("freshness-gauge");
        public BagelGauge speedGauge => m_SpeedGauge ??= this.Q<BagelGauge>("speed-gauge");
        public Label toppingsCount => m_ToppingsCount ??= this.Q<Label>("toppings-count");
        public ProgressBar spinBar => m_SpinBar ??= this.Q<ProgressBar>("spin-bar");
        public ProgressBar painBar => m_PainBar ??= this.Q<ProgressBar>("pain-bar");

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            // Nothing here is interactive: never pick it, which saves raycasts against world-space panels.
            this.Query().ForEach(element => element.pickingMode = PickingMode.Ignore);
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateToppingsCount();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == BagelTrackerData.toppingsCountPath)
                UpdateToppingsCount();
        }

        protected override void OnLocaleChanged() => UpdateToppingsCount();

        void UpdateToppingsCount()
        {
            var label = toppingsCount;
            if (label == null || state == null || label.panel == null)
                return;

            Span<char> buffer = stackalloc char[16];
            if (state.tracker.toppingsCount.TryFormat(buffer, out var length, "N0", BagelLocalization.culture))
            {
                BagelLocalization.LocalizeDigits(buffer.Slice(0, length));
                label.SetText(buffer.Slice(0, length));
            }
        }
    }
}
