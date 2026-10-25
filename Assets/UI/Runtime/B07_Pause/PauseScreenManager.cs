using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class PauseScreenManager : BagelPane
    {
        public static readonly UniqueStyleString inactivePaneClassName = new UniqueStyleString("b-inactive-pane");

        // The margin Settings keep from the screen's edges, in pixels.
        const float k_FitMargin = 24f;

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

            RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);

            RegisterCallback<GeometryChangedEvent>(_ => FitSettings());
            settingsPane?.RegisterCallback<GeometryChangedEvent>(_ => FitSettings());
        }

        // Settings are the blackboard's window of tabs, at the pause buttons' size: on a screen too small for it,
        // the whole window shrinks to fit (a scale, which leaves its layout alone), so nothing ever scrolls.
        void FitSettings()
        {
            var pane = settingsPane;
            if (pane == null || !(pane.layout.width > 0f) || !(pane.layout.height > 0f))
                return;
            var fit = Mathf.Min(1f, (layout.width - k_FitMargin * 2f) / pane.layout.width, (layout.height - k_FitMargin * 2f) / pane.layout.height);
            fit = Mathf.Max(0.1f, fit);
            pane.style.scale = new Scale(new Vector3(fit, fit, 1f));
        }

        // The gamepad's back button leaves Settings, then resumes. Escape is also the Pause key,
        // and the Pause action already takes care of it.
        void OnNavigationCancelEvent(NavigationCancelEvent evt)
        {
            if (state == null || Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            Back();
            evt.StopPropagation();
        }

        public void Back()
        {
            if (state.pausePane == PlayManagerState.PausePane.Settings)
                GoToPausePane();
            else
                state.Resume();
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

            // Keep gamepad and keyboard focus on the pane that's showing, once it's shown.
            if (state == null || !state.isPaused)
                return;
            schedule.Execute(() =>
            {
                if (pane == PlayManagerState.PausePane.Pause)
                    pausePaneManager.resumeButton.Focus();
                else
                    settingsPaneManager.FocusTabs();
            });

            if (BagelAccessibility.screen == this)
                BagelAccessibility.ShowScreen(this);
        }
    }
}
