using System;

namespace Bagel
{
    public class PlayManagerState
    {
        public enum State {
            MainMenu,
            BagelSelection,
            Playing,
            GameOver
        }

        public enum MainMenuPaneMode {
            Primary,
            Secondary
        }

        State m_State;
        MainMenuPaneMode m_MainMenuPaneMode;
        bool m_IsPaused;
        BagelType m_LastBagelType;
        BagelTrackerData m_LastBagelTrackerData;

        public State currentState => m_State;
        public MainMenuPaneMode currentMainMenuPaneMode => m_MainMenuPaneMode;
        public BagelType lastBagelType => m_LastBagelType;
        public BagelTrackerData lastBagelTrackerData => m_LastBagelTrackerData;
        public bool isMainMenu => m_State == State.MainMenu;
        public bool isBagelSelection => m_State == State.BagelSelection;
        public bool isPlaying => m_State == State.Playing;
        public bool isPaused => m_IsPaused;
        public bool isGameOver => m_State == State.GameOver;

        public event EventHandler<State> onStateChange;
        public event EventHandler<bool> onPauseStateChanged;
        public event EventHandler<BagelType> onSetBagelType;
        public event EventHandler<BagelTrackerData> onSetBagelTrackerData;
        public event EventHandler<MainMenuPaneMode> onMainMenuPaneModeChange;

        public void SetBagelType(BagelType bagelType)
        {
            m_LastBagelType = bagelType;
            onSetBagelType?.Invoke(this, bagelType);
        }

        public void SetBagelTrackerData(BagelTrackerData bagelTrackerData)
        {
            m_LastBagelTrackerData = bagelTrackerData;
            onSetBagelTrackerData?.Invoke(this, bagelTrackerData);
        }

        public void SetMainMenuPaneMode(MainMenuPaneMode mode)
        {
            if (!isMainMenu || mode == m_MainMenuPaneMode)
                return;

            m_MainMenuPaneMode = mode;
            onMainMenuPaneModeChange?.Invoke(this, mode);
        }

        public void GoToMainMenu()
        {
            Resume();
            m_State = State.MainMenu;
            onStateChange?.Invoke(this, State.MainMenu);
        }

        public void GoToBagelSelection()
        {
            Resume();
            m_State = State.BagelSelection;
            onStateChange?.Invoke(this, State.BagelSelection);
        }

        public void GoToPlay()
        {
            Resume();
            m_State = State.Playing;
            onStateChange?.Invoke(this, State.Playing);
            SetBagelType(m_LastBagelType);
        }

        public void GoToGameOver()
        {
            Resume();
            m_State = State.GameOver;
            onStateChange?.Invoke(this, State.GameOver);
        }

        public void Pause()
        {
            if (m_IsPaused)
                return;

            if (!isPlaying)
                return;

            m_IsPaused = true;
            onPauseStateChanged?.Invoke(this, true);
        }

        public void Resume()
        {
            if (!m_IsPaused)
                return;

            if (!isPlaying)
                return;

            m_IsPaused = false;
            onPauseStateChanged?.Invoke(this, false);
        }

        public void TogglePause()
        {
            if (m_IsPaused)
                Resume();
            else
                Pause();
        }
    }
}
