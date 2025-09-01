using System;

namespace Bagel
{
    public class PlayManagerState : IPlayManager
    {
        enum State {
            MainMenu,
            BagelSelection,
            Playing,
            GameOver
        }

        State m_State;
        bool m_IsPaused;
        BagelType m_BagelType;
        BagelTrackerData m_LastGameOverBagelTrackerData;

        public BagelType BagelType => m_BagelType;
        public BagelTrackerData LastGameOverBagelTrackerData => m_LastGameOverBagelTrackerData;

        public event EventHandler<bool> OnPauseStateChanged;
        public event EventHandler<BagelTrackerData> OnGameOver;

        public bool IsPlaying()
        {
            return m_State == State.Playing;
        }

        public bool IsPaused()
        {
            return m_IsPaused;
        }

        public void GoToMainMenu()
        {
            Clear();
            m_State = State.MainMenu;
        }

        public void GoToBagelSelection()
        {
            Clear();
            m_State = State.BagelSelection;
        }

        public void SetBagelType(BagelType bagelType)
        {
            m_BagelType = bagelType;
        }

        public void GoToPlay()
        {
            Clear();
            m_State = State.Playing;
        }

        public void Pause()
        {
            m_IsPaused = true;
            OnPauseStateChanged?.Invoke(this, true);
        }

        public void Resume()
        {
            Clear();
            m_IsPaused = false;
        }

        public void GoToGameOver(BagelTrackerData bagelTrackerData)
        {
            Clear();
            m_State = State.GameOver;
            m_LastGameOverBagelTrackerData = bagelTrackerData;
            OnGameOver?.Invoke(this, bagelTrackerData);
        }

        void Clear()
        {
            OnPauseStateChanged?.Invoke(this, false);
        }
    }
}
