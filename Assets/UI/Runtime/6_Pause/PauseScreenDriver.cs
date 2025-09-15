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
        VisualElement m_Root;

        Button m_ResumeButton;

        void OnEnable()
        {
            m_PlayManager.State.OnPauseStateChanged += State_OnPauseStateChanged;
            m_PlayManager.PlayInputBindings.OnPauseAction += PlayInputBindings_OnPauseAction;

            m_UIDocument = GetComponent<UIDocument>();
            m_Root = m_UIDocument.rootVisualElement;
            SetPauseState(false);

            if (m_PlayManager == null)
                return;

            m_ResumeButton = m_Root.Q<Button>("resume-button");
            if (m_ResumeButton != null)
                m_ResumeButton.clicked += m_PlayManager.State.Resume;

            var mainMenuButton = m_Root.Q<Button>("main-menu-button");
            if (mainMenuButton != null)
                mainMenuButton.clicked += m_PlayManager.State.GoToMainMenu;
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
            if (m_Root == null)
                return;

            if (paused)
            {
                m_ResumeButton.Focus();
                m_Root.style.display = DisplayStyle.Flex;
                Time.timeScale = 0f;
            }
            else
            {
                m_Root.style.display = DisplayStyle.None;
                Time.timeScale = 1f;
            }
        }
    }
}
