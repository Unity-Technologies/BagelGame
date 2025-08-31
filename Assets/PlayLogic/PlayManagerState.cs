using System;

namespace Bagel
{
    public class PlayManagerState : IPlayManager
    {
        enum State {
            MainMenu,
            Playing,
            GameOver
        }

        State m_State;
        bool m_IsPaused;

        public event EventHandler<bool> OnPauseStateChanged;

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
            m_State = State.MainMenu;
            OnPauseStateChanged?.Invoke(this, false);
        }

        public void GoToPlay()
        {
            m_State = State.Playing;
            OnPauseStateChanged?.Invoke(this, false);
        }

        public void Pause()
        {
            m_IsPaused = true;
            OnPauseStateChanged?.Invoke(this, true);
        }

        public void Resume()
        {
            m_IsPaused = false;
            OnPauseStateChanged?.Invoke(this, false);
        }

        public void GoToGameOver()
        {
            m_State = State.GameOver;
            OnPauseStateChanged?.Invoke(this, false);
        }
    }
}
