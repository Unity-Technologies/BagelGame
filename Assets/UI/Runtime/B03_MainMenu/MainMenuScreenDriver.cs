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
        MainMenuScreenManager.Elements m_Elements;

        public MainMenuScreenManager.Elements elements => m_Elements;

        void OnEnable()
        {
            m_PlayManager.State.OnStateChange += State_OnStateChange;

            m_UIDocument = GetComponent<UIDocument>();
            var mainMenuManager = m_UIDocument.rootVisualElement.Q<MainMenuScreenManager>();
            var settingsPaneManager = mainMenuManager.Q<SettingsPaneManager>();

            settingsPaneManager.BindSettingsCallbacks(m_PlayManager.playSettingsObject);

            var root = m_UIDocument.rootVisualElement;
            m_Elements = MainMenuScreenManager.BindUI(root, new MainMenuScreenManager.Callbacks
            {
                playManagerState = m_PlayManager.State,
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
                m_Elements.playButton.Focus();
        }
    }
}
