using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        MainMenuPaneManager m_MainMenuPaneManager;

        void OnEnable()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;

            var uiDocument = GetComponent<UIDocument>();
            var mainMenuScreenManager = uiDocument.rootVisualElement.Q<MainMenuScreenManager>();
            m_MainMenuPaneManager = uiDocument.rootVisualElement.Q<MainMenuPaneManager>();
            var settingsPaneManager = mainMenuScreenManager.Q<SettingsPaneManager>();

            settingsPaneManager.BindSettingsCallbacks(m_PlayManager.playSettingsObject);

            mainMenuScreenManager.BindUI(new MainMenuScreenManager.Callbacks
            {
                playManagerState = m_PlayManager.State
            });

            m_MainMenuPaneManager.BindUI(new MainMenuPaneManager.Callbacks
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
            if (m_MainMenuPaneManager.playButton == null)
                return;

            if (state == PlayManagerState.State.MainMenu)
                m_MainMenuPaneManager.playButton.Focus();
        }
    }
}
