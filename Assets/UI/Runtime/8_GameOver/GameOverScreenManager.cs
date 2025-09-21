
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class GameOverScreenManager : VisualElement
    {
        public struct Elements
        {
            public VisualElement root;
            public Label title;
            public TextField addToLeaderboardNameField;
            public Button addToLeaderboardButton;
            public Button restartButton;
            public Button mainMenuButton;
        }

        public struct Callbacks
        {
            public Action onAddToLeaderboard;
            public Action onRestart;
            public Action onMainMenu;
        }

        Elements m_Elements;

        public static Elements BindUI(VisualElement root, Callbacks callbacks)
        {
            var elements = new Elements
            {
                root = root,
                title = root.Q<Label>("title"),
                addToLeaderboardNameField = root.Q<TextField>("leaderboard-name-field"),
                addToLeaderboardButton = root.Q<Button>("add-to-leaderboard-button"),
                restartButton = root.Q<Button>("restart-button"),
                mainMenuButton = root.Q<Button>("main-menu-button" )
            };

            if (elements.addToLeaderboardButton != null && callbacks.onAddToLeaderboard != null)
                elements.addToLeaderboardButton.clicked += callbacks.onAddToLeaderboard;
            if (elements.restartButton != null && callbacks.onRestart != null )
                elements.restartButton.clicked += callbacks.onRestart;
            if (elements.mainMenuButton != null && callbacks.onMainMenu != null )
                elements.mainMenuButton.clicked += callbacks.onMainMenu;

            return elements;
        }

        public GameOverScreenManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            m_Elements = BindUI(this, new Callbacks
            {
                onAddToLeaderboard = AddNameToLeaderboard
            });
        }

        void AddNameToLeaderboard()
        {
            Debug.Log("Add to leaderboard: " + m_Elements.addToLeaderboardNameField.value);
        }
    }
}
