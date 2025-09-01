using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] PlayInputBindings m_PlayerInputBindings;
        [SerializeField] BagelController m_BagelController;
        [SerializeField] BagelTracker m_BagelTracker;
        [SerializeField] Transform m_StartingPoint;

        PlayManagerState m_PlayManagerState = new PlayManagerState();

        public PlayManagerState State => m_PlayManagerState;

        void Awake()
        {
            m_PlayerInputBindings.OnPauseAction += PlayerInputBindings_OnPauseAction;

            State.OnStateChange += State_OnStateChange;
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

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            switch (state)
            {
                case PlayManagerState.State.MainMenu:
                {
                    Clear();
                    break;
                }
                case PlayManagerState.State.BagelSelection:
                {
                    Clear();
                    break;
                }
                case PlayManagerState.State.Playing:
                {
                    Clear();
                    m_BagelController.gameObject.SetActive(true);
                    m_BagelTracker.gameObject.SetActive(true);
                    break;
                }
                case PlayManagerState.State.GameOver:
                {
                    Clear();
                    break;
                }
            }
        }

        void State_OnPauseStateChanged(object sender, bool paused)
        {
            if (paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
        }

        void Clear()
        {
            m_BagelController.transform.position = m_StartingPoint.position;
            m_BagelController.transform.rotation = m_StartingPoint.rotation;

            m_BagelTracker.gameObject.SetActive(false);
            m_BagelController.gameObject.SetActive(false);
        }
    }
}
