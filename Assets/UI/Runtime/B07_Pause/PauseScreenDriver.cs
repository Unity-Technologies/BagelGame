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

        VisualElement m_Root;
        PauseScreenManager m_PauseScreenManager;

        void OnEnable()
        {
            m_Root = GetComponent<UIDocument>().rootVisualElement;
            m_PauseScreenManager = m_Root.Q<PauseScreenManager>();

            m_PauseScreenManager.settingsPaneManager.BindSettingsCallbacks(m_PlayManager.playSettingsObject);

            m_PlayManager.state.onPauseStateChanged += State_OnPauseStateChanged;
            m_PlayManager.playInputBindings.onPauseAction += PlayInputBindings_OnPauseAction;

            m_PauseScreenManager.pausePaneManager.BindUI(
                new PausePaneManager.Callbacks
                {
                    onResume = m_PlayManager.state.Resume,
                    onRestart = m_PlayManager.state.GoToPlay,
                    onMainMenu = m_PlayManager.state.GoToMainMenu
                }
            );

            SetPauseState(false);
        }

        void OnDisable()
        {
            m_PlayManager.state.onPauseStateChanged -= State_OnPauseStateChanged;
            m_PlayManager.playInputBindings.onPauseAction -= PlayInputBindings_OnPauseAction;
        }

        void State_OnPauseStateChanged(object sender, bool paused)
        {
            SetPauseState(paused);
        }

        void PlayInputBindings_OnPauseAction(object sender, EventArgs e)
        {
            m_PlayManager.state.TogglePause();
        }

        void SetPauseState(bool paused)
        {
            if (paused)
            {
                m_PauseScreenManager.pausePaneManager.resumeButton.Focus();
                m_PauseScreenManager.GoToPausePane();
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
