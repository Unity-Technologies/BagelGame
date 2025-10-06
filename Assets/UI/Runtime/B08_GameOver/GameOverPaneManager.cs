
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class GameOverPaneManager : VisualElement
    {
        Label m_Title;
        IntegerField m_ToppingsNumberField;
        VisualElement m_LeaderboardForm;
        TextField m_AddToLeaderboardNameField;
        Button m_AddToLeaderboardButton;
        Button m_RestartButton;
        Button m_MainMenuButton;

        public Label title => m_Title ??= this.Q<Label>("title");
        public IntegerField toppingsNumberField => m_ToppingsNumberField ??= this.Q<IntegerField>("toppings-number-field");
        public VisualElement leaderboardForm => m_LeaderboardForm ??= this.Q<VisualElement>("leaderboard-form");
        public TextField addToLeaderboardNameField => m_AddToLeaderboardNameField ??= this.Q<TextField>("leaderboard-name-field");
        public Button addToLeaderboardButton => m_AddToLeaderboardButton ??= this.Q<Button>("add-to-leaderboard-button");
        public Button restartButton => m_RestartButton ??= this.Q<Button>("restart-button");
        public Button mainMenuButton => m_MainMenuButton ??= this.Q<Button>("main-menu-button");

        public struct Callbacks
        {
            public Action onAddToLeaderboard;
            public Action onRestart;
            public Action onMainMenu;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (addToLeaderboardButton != null && callbacks.onAddToLeaderboard != null)
                addToLeaderboardButton.clicked += callbacks.onAddToLeaderboard;
            if (restartButton != null && callbacks.onRestart != null )
                restartButton.clicked += callbacks.onRestart;
            if (mainMenuButton != null && callbacks.onMainMenu != null )
                mainMenuButton.clicked += callbacks.onMainMenu;
        }
    }
}
