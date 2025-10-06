using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class PausePaneManager : VisualElement
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
    }
}
