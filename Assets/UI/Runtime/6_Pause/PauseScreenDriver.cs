using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class PauseScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;

        public struct Elements
        {
            public VisualElement root;
            public Button resumeButton;
            public LongPressButton restartButton;
            public LongPressButton mainMenuButton;
        }

        public struct Callbacks
        {
            public Action onResume;
            public Action onRestart;
            public Action onMainMenu;
        }

        Elements m_Elements;

        public static Elements BindUI(VisualElement root, Callbacks callbacks)
        {
            var elements = new Elements
            {
                root = root,
                resumeButton = root.Q<Button>("resume-button"),
                restartButton = root.Q<LongPressButton>("restart-button"),
                mainMenuButton = root.Q<LongPressButton>("main-menu-button" )
            };

            if (elements.resumeButton != null && callbacks.onResume != null)
                elements.resumeButton.clicked += callbacks.onResume;
            if (elements.restartButton != null && callbacks.onRestart != null )
                elements.restartButton.clicked += callbacks.onRestart;
            if (elements.mainMenuButton != null && callbacks.onMainMenu != null )
                elements.mainMenuButton.clicked += callbacks.onMainMenu;

            return elements;
        }

        void OnEnable()
        {
            m_PlayManager.State.OnPauseStateChanged += State_OnPauseStateChanged;
            m_PlayManager.PlayInputBindings.OnPauseAction += PlayInputBindings_OnPauseAction;

            m_Elements = BindUI(GetComponent<UIDocument>().rootVisualElement, new Callbacks
            {
                onResume = m_PlayManager.State.Resume,
                onRestart = m_PlayManager.State.GoToPlay,
                onMainMenu = m_PlayManager.State.GoToMainMenu
            });

            SetPauseState(false);
        }

        void OnDisable()
        {
            m_PlayManager.State.OnPauseStateChanged -= State_OnPauseStateChanged;
            m_PlayManager.PlayInputBindings.OnPauseAction -= PlayInputBindings_OnPauseAction;
        }

        void State_OnPauseStateChanged(object sender, bool paused)
        {
            SetPauseState(paused);
        }

        void PlayInputBindings_OnPauseAction(object sender, EventArgs e)
        {
            m_PlayManager.State.TogglePause();
        }

        void SetPauseState(bool paused)
        {
            if (paused)
            {
                m_Elements.resumeButton.Focus();
                m_Elements.root.style.display = DisplayStyle.Flex;
                Time.timeScale = 0f;
            }
            else
            {
                m_Elements.root.style.display = DisplayStyle.None;
                Time.timeScale = 1f;
            }
        }
    }
}
