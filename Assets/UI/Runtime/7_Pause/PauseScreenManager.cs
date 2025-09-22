using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class PauseScreenManager : VisualElement
    {
        public static readonly string inactivePaneClassName = "b-inactive-pane";

        public struct Elements
        {
            public VisualElement root;
            public PauseScreenManager manager;

            public VisualElement pausePane;
            public VisualElement settingsPane;

            public Button resumeButton;
            public Button settingsButton;
            public LongPressButton restartButton;
            public LongPressButton mainMenuButton;

            public Button settingsBackButton;
        }

        public struct Callbacks
        {
            public Action onResume;
            public Action onSettings;
            public Action onRestart;
            public Action onMainMenu;

            public Action onSettingsBack;
        }

        Elements m_Elements;

        public static Elements BindUI(VisualElement root, Callbacks callbacks)
        {
            var elements = new Elements
            {
                root = root,
                manager = root.Q<PauseScreenManager>(),

                pausePane = root.Q<VisualElement>("pause-pane"),
                settingsPane = root.Q<VisualElement>("settings-pane"),

                resumeButton = root.Q<Button>("resume-button"),
                settingsButton = root.Q<Button>("settings-button"),
                restartButton = root.Q<LongPressButton>("restart-button"),
                mainMenuButton = root.Q<LongPressButton>("main-menu-button" ),

                settingsBackButton = null
            };

            if (elements.settingsPane != null)
                elements.settingsBackButton = elements.settingsPane.Q<Button>("back-button");

            if (elements.resumeButton != null && callbacks.onResume != null)
                elements.resumeButton.clicked += callbacks.onResume;
            if (elements.settingsButton != null && callbacks.onSettings != null )
                elements.settingsButton.clicked += callbacks.onSettings;
            if (elements.restartButton != null && callbacks.onRestart != null )
                elements.restartButton.clicked += callbacks.onRestart;
            if (elements.mainMenuButton != null && callbacks.onMainMenu != null )
                elements.mainMenuButton.clicked += callbacks.onMainMenu;

            if (elements.settingsBackButton != null && callbacks.onSettingsBack != null)
                elements.settingsBackButton.clicked += callbacks.onSettingsBack;

            return elements;
        }

        public PauseScreenManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            m_Elements = BindUI(this, new Callbacks
            {
                onSettings = GoToSettingsPane,
                onSettingsBack = GoToPausePane
            });
        }

        public void GoToPausePane()
        {
            if (m_Elements.pausePane == null)
                return;

            m_Elements.pausePane.RemoveFromClassList(inactivePaneClassName);
            m_Elements.settingsPane.AddToClassList(inactivePaneClassName);
        }

        public void GoToSettingsPane()
        {
            m_Elements.pausePane.AddToClassList(inactivePaneClassName);
            m_Elements.settingsPane.RemoveFromClassList(inactivePaneClassName);
        }
    }
}
