using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Shows the game over screen on the television on the bakery's far wall, all the time: the last run and the
    // leaderboard. At game over it takes the focus for its buttons, and from the main menu's Leaderboard button
    // for its Back button; the camera looks at it then (PlayCamera).
    [RequireComponent(typeof(PanelRenderer))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        PlayManagerState m_State;
        GameOverScreenManager m_GameOverScreenManager;

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

            var gameOverScreenManager = rootElement.Q<GameOverScreenManager>();
            if (gameOverScreenManager == null)
                return; // The UXML is still importing; the next reload brings it.
            m_UIVersion = version;

            m_GameOverScreenManager = gameOverScreenManager;
            m_GameOverScreenManager.SetState(m_State);

            // On a fresh scene load the UI arrives after the first state change, so catch up here.
            FocusIfInUse();
        }

        void State_OnStateChange(object sender, PlayManagerState.State state) => FocusIfInUse();

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName == PlayManagerState.mainMenuPanePath)
                FocusIfInUse();
        }

        // World-space panels only get gamepad and keyboard navigation once something in them has focus.
        void FocusIfInUse()
        {
            if (m_GameOverScreenManager == null)
                return;

            VisualElement focus = null;
            if (m_State.isGameOver)
                focus = m_GameOverScreenManager.gameOverPaneManager.mainMenuButton;
            else if (m_GameOverScreenManager.isLeaderboardOpen)
                focus = m_GameOverScreenManager.backButton;
            if (focus == null)
                return;

            // Wait for the buttons to show: hidden elements can't take focus, and a button the screen's mode shows
            // only shows once the panel's styles are updated, which can be after the next scheduled update. So it's
            // tried each update until something on the screen has the focus, or the screen's no longer in use.
            var screen = m_GameOverScreenManager;
            screen.schedule.Execute(() =>
                {
                    if (focus.canGrabFocus)
                        focus.Focus();
                })
                .Every(0)
                .Until(() => HasTheFocus(screen) || !InUse());
            BagelAccessibility.ShowScreen(m_GameOverScreenManager);
        }

        bool InUse() => m_State.isGameOver || m_GameOverScreenManager.isLeaderboardOpen;

        static bool HasTheFocus(VisualElement screen) => screen.focusController?.focusedElement is VisualElement focused && screen.Contains(focused);
    }
}
