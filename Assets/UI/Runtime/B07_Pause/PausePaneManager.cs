using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class PausePaneManager : BagelPane
    {
        Button m_ResumeButton;
        Button m_SettingsButton;
        LongPressButton m_RestartButton;
        LongPressButton m_MainMenuButton;

        public Button resumeButton => m_ResumeButton ??= this.Q<Button>("resume-button");
        public Button settingsButton => m_SettingsButton ??= this.Q<Button>("settings-button");
        public LongPressButton restartButton => m_RestartButton ??= this.Q<LongPressButton>("restart-button");
        public LongPressButton mainMenuButton => m_MainMenuButton ??= this.Q<LongPressButton>("main-menu-button");

        public struct Callbacks
        {
            public Action onResume;
            public Action onSettings;
            public Action onRestart;
            public Action onMainMenu;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (resumeButton != null && callbacks.onResume != null)
                resumeButton.clicked += callbacks.onResume;
            if (settingsButton != null && callbacks.onSettings != null )
                settingsButton.clicked += callbacks.onSettings;
            if (restartButton != null && callbacks.onRestart != null )
                restartButton.clicked += callbacks.onRestart;
            if (mainMenuButton != null && callbacks.onMainMenu != null )
                mainMenuButton.clicked += callbacks.onMainMenu;
        }

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            // The page title comes from the Page template, so its binding is added here.
            BagelLocalization.Bind(this.Q<Label>("page-title"), "text", "pause.title");

            BindUI(new Callbacks
            {
                onResume = () => state?.Resume(),
                onSettings = () => { if (state != null) state.pausePane = PlayManagerState.PausePane.Settings; },
                onRestart = () => state?.GoToPlay(),
                onMainMenu = () => state?.GoToMainMenu()
            });
        }
    }
}
