using System;
using UnityEngine.UIElements;

namespace Bagel
{
    // The main menu, chalked on the blackboard: each button is a word in a chalk item (with its underlines and
    // arrow), which shows the focus for it when the gamepad or keyboard moves there.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class MainMenuPaneManager : BagelPane
    {
        public static readonly UniqueStyleString itemFocusUssClassName = new("b-chalk-item--focus");

        Button m_PlayButton;
        Button m_SettingsButton;
        Button m_LeaderboardButton;
        Button m_ExitButton;

        public Button playButton => m_PlayButton ??= this.Q<Button>("play-button");
        public Button settingsButton => m_SettingsButton ??= this.Q<Button>("settings-button");
        public Button leaderboardButton => m_LeaderboardButton ??= this.Q<Button>("leaderboard-button");
        public Button exitButton => m_ExitButton ??= this.Q<Button>("exit-button");

        public struct Callbacks
        {
            public Action onPlay;
            public Action onSettings;
            public Action onLeaderboard;
            public Action onExit;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (playButton != null && callbacks.onPlay != null)
                playButton.clicked += callbacks.onPlay;
            if (settingsButton != null && callbacks.onSettings != null )
                settingsButton.clicked += callbacks.onSettings;
            if (leaderboardButton != null && callbacks.onLeaderboard != null )
                leaderboardButton.clicked += callbacks.onLeaderboard;
            if (exitButton != null && callbacks.onExit != null )
                exitButton.clicked += callbacks.onExit;
        }

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            BindUI(new Callbacks
            {
                onPlay = () => state?.GoToBagelSelection(),
                onSettings = () => state?.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings),
                onLeaderboard = () => state?.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard)
            });

            foreach (var button in new[] { playButton, settingsButton, leaderboardButton, exitButton })
                ShowFocusOnItem(button);
        }

        static void ShowFocusOnItem(Button button)
        {
            if (button?.parent == null)
                return;
            var item = button.parent;
            button.RegisterCallback<FocusInEvent>(_ => item.AddToClassList(itemFocusUssClassName));
            button.RegisterCallback<FocusOutEvent>(_ => item.RemoveFromClassList(itemFocusUssClassName));
        }
    }
}
