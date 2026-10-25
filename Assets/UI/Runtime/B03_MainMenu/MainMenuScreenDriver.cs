using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        PlayManagerState m_State;
        MainMenuScreenManager m_MainMenuScreenManager;

        int m_UIVersion;

        void OnEnable()
        {
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;
            m_State.propertyChanged += State_OnPropertyChanged;

            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.propertyChanged -= State_OnPropertyChanged;

            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);

            // Play mode can start without a domain reload, and the panel counts versions again from
            // the start: forget the last one so the next UI is always set up.
            m_UIVersion = 0;
        }

        void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (m_UIVersion == version)
                return;

            var mainMenuScreenManager = rootElement.Q<MainMenuScreenManager>();
            if (mainMenuScreenManager == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_MainMenuScreenManager = mainMenuScreenManager;
            m_MainMenuScreenManager.SetState(m_State);
            m_MainMenuScreenManager.Q<SettingsPaneManagerForUI>().appliesThemeToPanelSettings = true;
            m_MainMenuScreenManager.Q<SettingsPaneManagerForControls>().playInputBindings = m_PlayManager.playInputBindings;
            m_MainMenuScreenManager.mainMenuPaneManager.BindUI(new MainMenuPaneManager.Callbacks
            {
#if UNITY_EDITOR
                onExit = UnityEditor.EditorApplication.ExitPlaymode
#else
                onExit = Application.Quit
#endif
            });

            // On a fresh scene load the UI arrives after the first state change, so catch up here.
            FocusCurrentPane();
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state != PlayManagerState.State.MainMenu)
                return;

            FocusCurrentPane();
            PlayIntroAnimation();
        }

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName == PlayManagerState.mainMenuPanePath)
                FocusCurrentPane();
        }

        // World-space panels only get gamepad and keyboard navigation once something in them has focus.
        void FocusCurrentPane()
        {
            if (m_MainMenuScreenManager == null)
                return;

            // Wait for the pane to be shown: disabled elements can't take focus. The leaderboard is on the
            // television, which takes the focus itself.
            m_MainMenuScreenManager.schedule.Execute(() =>
            {
                if (!m_State.isMainMenu || m_State.mainMenuPane == PlayManagerState.MainMenuPane.Leaderboard)
                    return;

                BagelAccessibility.ShowScreen(m_MainMenuScreenManager);
                ReadTheScreenAfterTheIntro();

                switch (m_State.mainMenuPane)
                {
                    case PlayManagerState.MainMenuPane.Main:
                        m_MainMenuScreenManager.mainMenuPaneManager.playButton?.Focus();
                        break;
                    case PlayManagerState.MainMenuPane.Settings:
                        m_MainMenuScreenManager.settingsBackButton?.Focus();
                        break;
                }
            });
        }

        // The intro draws the buttons in from nothing, and screen readers only get what's shown: read the screen
        // again once the intro is done.
        void ReadTheScreenAfterTheIntro()
        {
            var animation = GetComponent<Animation>();
            if (animation == null || animation.clip == null || !animation.isPlaying)
                return;

            var intro = animation[animation.clip.name];
            var left = intro.length - intro.time;
            if (left <= 0f)
                return;

            m_MainMenuScreenManager.schedule.Execute(() =>
            {
                if (m_State.isMainMenu)
                    BagelAccessibility.ShowScreen(m_MainMenuScreenManager);
            }).StartingIn((long)(left * 1000f) + 1);
        }

        void PlayIntroAnimation()
        {
            var animation = GetComponent<Animation>();
            if (animation == null || m_State.settings.reduceMotion)
                return;

            animation.Rewind();
            animation.Play();
        }
    }
}
