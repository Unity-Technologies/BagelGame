using System;
using UnityEngine;

namespace Bagel
{
    public interface IPlayManager
    {
        event EventHandler<bool> OnPauseStateChanged;

        bool IsPlaying();
        bool IsPaused();

        void GoToMainMenu();
        void GoToBagelSelection();

        void SetBagelType(BagelType bagelType);
        void GoToPlay();
        void Pause();
        void Resume();
        void GoToGameOver();
    }
}
