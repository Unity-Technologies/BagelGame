using UnityEngine.UIElements;

namespace Bagel
{
    // The callout over the bagel, in screen space: "Ready..." then "Go!" when a run starts. Each one pops in (a
    // UIAnimationClip) and goes away by itself.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class BagelCalloutManager : BagelPane
    {
        public static readonly UniqueStyleString hiddenUssClassName = new("b-callout--hidden");
        public static readonly UniqueStyleString popUssClassName = new("b-callout--pop");

        const long k_ShowMs = 1200;
        const long k_ReadyMs = 700;

        Label m_Label;
        IVisualElementScheduledItem m_Hide;
        IVisualElementScheduledItem m_Go;

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
            m_Go = schedule.Execute(() => Show(BagelLocalization.Get("toast.go", "Go!")));
            m_Go.Pause();
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            if (newState != null)
                CountIn();
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property != PlayManagerState.currentStatePath)
                return;
            if (state.isPlaying)
                CountIn();
            else
                Hide();
        }

        // A run starts: "Ready..." and, a moment later, "Go!".
        void CountIn()
        {
            if (!state.isPlaying || state.tracker.elapsed > 0f || panel == null)
                return;
            Show(BagelLocalization.Get("toast.ready", "Ready..."));
            m_Go?.ExecuteLater(k_ReadyMs);
        }

        public void Show(string text)
        {
            if (label == null)
                return;

            label.text = text;
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
