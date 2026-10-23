using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class PauseScreenManager : BagelPane
    {
        public static readonly UniqueStyleString inactivePaneClassName = new UniqueStyleString("b-inactive-pane");

        VisualElement m_PausePane;
        VisualElement m_SettingsPane;
        PausePaneManager m_PausePaneManager;
        SettingsPaneManager m_SettingsPaneManager;

        public VisualElement pausePane => m_PausePane ??= this.Q<VisualElement>("pause-pane");
        public VisualElement settingsPane => m_SettingsPane ??= this.Q<VisualElement>("settings-pane");
        public PausePaneManager pausePaneManager => m_PausePaneManager ??= this.Q<PausePaneManager>();
        public SettingsPaneManager settingsPaneManager => m_SettingsPaneManager ??= this.Q<SettingsPaneManager>();

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            settingsPaneManager.BindUI(new SettingsPaneManager.Callbacks
            {
                onSettingsBack = GoToPausePane
            });
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            ShowPane(newState != null ? newState.pausePane : PlayManagerState.PausePane.Pause);
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.pausePanePath)
                ShowPane(state.pausePane);
        }

        public void GoToPausePane()
        {
            if (state != null)
                state.pausePane = PlayManagerState.PausePane.Pause;
        }

        public void GoToSettingsPane()
        {
            if (state != null)
                state.pausePane = PlayManagerState.PausePane.Settings;
        }

        void ShowPane(PlayManagerState.PausePane pane)
        {
            if (pausePane == null)
                return;

            pausePane.EnableInClassList(inactivePaneClassName, pane != PlayManagerState.PausePane.Pause);
            settingsPane.EnableInClassList(inactivePaneClassName, pane != PlayManagerState.PausePane.Settings);
        }
    }
}
