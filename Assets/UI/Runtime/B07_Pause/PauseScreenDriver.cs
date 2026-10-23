using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(PanelRenderer))]
    public class PauseScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        PlayManagerState m_State;
        VisualElement m_Root;
        PauseScreenManager m_PauseScreenManager;

        int m_UIVersion;

        void OnEnable()
        {
            // In Edit mode the screen only hides itself, so it doesn't cover the Game view.
            if (Application.isPlaying)
            {
                m_State = m_PlayManager.state;
                m_State.onPauseStateChanged += State_OnPauseStateChanged;
                m_PlayManager.playInputBindings.onPauseAction += PlayInputBindings_OnPauseAction;
            }

            // Registering calls OnUIReload right away when the UI is already loaded.
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;

            if (m_State == null)
                return;

            m_State.onPauseStateChanged -= State_OnPauseStateChanged;
            m_PlayManager.playInputBindings.onPauseAction -= PlayInputBindings_OnPauseAction;
            m_State = null;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var pauseScreenManager = rootElement.Q<PauseScreenManager>();
            if (pauseScreenManager == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_Root = rootElement;
            m_PauseScreenManager = pauseScreenManager;

            if (m_State != null)
            {
                m_PauseScreenManager.SetState(m_State);
                m_PauseScreenManager.settingsPaneManager.forUI.appliesThemeToPanelSettings = true;
            }

            SetPauseState(m_State != null && m_State.isPaused);
        }

        void State_OnPauseStateChanged(object sender, bool paused)
        {
            SetPauseState(paused);
        }

        void PlayInputBindings_OnPauseAction(object sender, EventArgs e)
        {
            m_State.TogglePause();
        }

        void SetPauseState(bool paused)
        {
            if (m_Root == null)
                return;

            m_Root.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;

            if (paused)
                m_PauseScreenManager.schedule.Execute(() => m_PauseScreenManager.pausePaneManager.resumeButton.Focus());

            if (Application.isPlaying)
                Time.timeScale = paused ? 0f : 1f;
        }
    }
}
