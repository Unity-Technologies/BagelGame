using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] PlayInputBindings m_PlayerInputBindings;

        PlayManagerState m_PlayManagerState = new PlayManagerState();

        public PlayManagerState State => m_PlayManagerState;

        void Awake()
        {
            m_PlayerInputBindings.OnPauseAction += PlayerInputBindings_OnPauseAction;
            State.OnPauseStateChanged += State_OnPauseStateChanged;
        }

        void OnEnable()
        {
            State.GoToMainMenu();
        }

        void PlayerInputBindings_OnPauseAction(object sender, EventArgs e)
        {
            State.TogglePause();
        }

        void State_OnPauseStateChanged(object sender, bool paused)
        {
            if (paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
        }
    }
}
