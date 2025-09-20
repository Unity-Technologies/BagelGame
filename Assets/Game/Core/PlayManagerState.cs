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

        State m_State;
        bool m_IsPaused;
        BagelType m_LastBagelType;
        BagelTrackerData m_LastBagelTrackerData;

        public BagelType LastBagelType => m_LastBagelType;
        public BagelTrackerData LastBagelTrackerData => m_LastBagelTrackerData;
        public bool IsBagelSelection => m_State == State.BagelSelection;
        public bool IsPlaying => m_State == State.Playing;
        public bool IsPaused => m_IsPaused;
        public bool IsGameOver => m_State == State.GameOver;

        public event EventHandler<State> OnStateChange;
        public event EventHandler<bool> OnPauseStateChanged;
        public event EventHandler<BagelType> OnSetBagelType;
        public event EventHandler<BagelTrackerData> OnSetBagelTrackerData;

        public void SetBagelType(BagelType bagelType)
        {
            m_LastBagelType = bagelType;
            OnSetBagelType?.Invoke(this, bagelType);
        }

        public void SetBagelTrackerData(BagelTrackerData bagelTrackerData)
        {
            m_LastBagelTrackerData = bagelTrackerData;
            OnSetBagelTrackerData?.Invoke(this, bagelTrackerData);
        }

        public void GoToMainMenu()
        {
            Resume();
            m_State = State.MainMenu;
            OnStateChange?.Invoke(this, State.MainMenu);
        }

        public void GoToBagelSelection()
        {
            Resume();
            m_State = State.BagelSelection;
            OnStateChange?.Invoke(this, State.BagelSelection);
        }

        public void GoToPlay()
        {
            Resume();
            m_State = State.Playing;
            OnStateChange?.Invoke(this, State.Playing);
            SetBagelType(m_LastBagelType);
        }

        public void GoToGameOver()
        {
            Resume();
            m_State = State.GameOver;
            OnStateChange?.Invoke(this, State.GameOver);
        }

        public void Pause()
        {
            if (m_IsPaused)
                return;

            if (!IsPlaying)
                return;

            m_IsPaused = true;
            OnPauseStateChanged?.Invoke(this, true);
        }

        public void Resume()
        {
            if (!m_IsPaused)
                return;

            if (!IsPlaying)
                return;

            m_IsPaused = false;
            OnPauseStateChanged?.Invoke(this, false);
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
