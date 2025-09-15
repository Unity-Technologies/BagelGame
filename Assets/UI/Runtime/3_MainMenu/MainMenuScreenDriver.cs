using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;

        public struct Elements
        {
            public Button playButton;
            public LongPressButton exitButton;
        }

        public struct Callbacks
        {
            public Action onPlay;
            public Action onExit;
        }

        Elements m_Elements;

        public static Elements BindUI(VisualElement root, Callbacks callbacks)
        {
            var elements = new Elements
            {
                playButton = root.Q<Button>("play-button"),
                exitButton = root.Q<LongPressButton>("exit-button")
            };

            if (elements.playButton != null && callbacks.onPlay != null)
                elements.playButton.clicked += callbacks.onPlay;
            if (elements.exitButton != null && callbacks.onExit != null )
                elements.exitButton.clicked += callbacks.onExit;

            return elements;
        }

        void OnEnable()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;

            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            m_Elements = BindUI(root, new Callbacks
            {
                onPlay = m_PlayManager.State.GoToBagelSelection,
#if UNITY_EDITOR
                onExit = UnityEditor.EditorApplication.ExitPlaymode
#else
                onExit = Application.Quit
#endif
            });
        }

        void OnDisable()
        {
            m_PlayManager.State.OnStateChange -= State_OnStateChange;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (m_Elements.playButton == null)
                return;

            if (state == PlayManagerState.State.MainMenu)
            {
                m_Elements.playButton.Focus();
                m_PlayManager.PlayInputBindings.OnNavigateAction += PlayInputBindings_OnNavigateAction;
            }
            else
            {
                m_PlayManager.PlayInputBindings.OnNavigateAction -= PlayInputBindings_OnNavigateAction;
            }
        }

        void PlayInputBindings_OnNavigateAction(object sender, Vector2 vec)
        {
            m_Elements.playButton.Focus();
        }
    }
}
