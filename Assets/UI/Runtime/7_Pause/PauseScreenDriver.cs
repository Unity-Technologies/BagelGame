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

        PauseScreenManager.Elements m_Elements;

        void OnEnable()
        {
            m_PlayManager.State.OnPauseStateChanged += State_OnPauseStateChanged;
            m_PlayManager.PlayInputBindings.OnPauseAction += PlayInputBindings_OnPauseAction;

            m_Elements = PauseScreenManager.BindUI(
                GetComponent<UIDocument>().rootVisualElement,
                new PauseScreenManager.Callbacks
                {
                    onResume = m_PlayManager.State.Resume,
                    onRestart = m_PlayManager.State.GoToPlay,
                    onMainMenu = m_PlayManager.State.GoToMainMenu
                }
            );

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
                m_Elements.manager.GoToPausePane();
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
