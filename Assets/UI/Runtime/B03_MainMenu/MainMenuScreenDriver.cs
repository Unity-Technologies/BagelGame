using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        MainMenuPaneManager m_MainMenuPaneManager;

        int m_UIVersion;

        void OnEnable()
        {
            m_PlayManager.state.onStateChange += State_OnStateChange;

            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            m_PlayManager.state.onStateChange -= State_OnStateChange;

            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;
            m_UIVersion = version;

            var mainMenuScreenManager = rootElement.Q<MainMenuScreenManager>();
            m_MainMenuPaneManager = rootElement.Q<MainMenuPaneManager>();
            var settingsPaneManager = mainMenuScreenManager.Q<SettingsPaneManager>();
            settingsPaneManager.BindSettingsCallbacks(m_PlayManager.playSettingsObject);
            mainMenuScreenManager.BindUI(new MainMenuScreenManager.Callbacks
            {
                playManagerState = m_PlayManager.state
            });
            m_MainMenuPaneManager.BindUI(new MainMenuPaneManager.Callbacks
            {
                onPlay = m_PlayManager.state.GoToBagelSelection,
#if UNITY_EDITOR
                onExit = UnityEditor.EditorApplication.ExitPlaymode
#else
                onExit = Application.Quit
#endif
            });
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (m_MainMenuPaneManager.playButton == null)
                return;

            if (state != PlayManagerState.State.MainMenu)
                return;

            m_MainMenuPaneManager.playButton.Focus();
            PlayIntroAnimation();
        }

        void PlayIntroAnimation()
        {
            var animation = GetComponent<Animation>();
            if (animation == null)
                return;

            animation.Rewind();
            animation.Play();
        }
    }
}
