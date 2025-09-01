using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] PlayerInputBindings m_PlayerInputBindings;
        [SerializeField] CinemachineCamera m_CinemachineCamera;
        [SerializeField] BagelController m_BagelController;
        [SerializeField] BagelTracker m_BagelTracker;
        [SerializeField] Transform m_StartingPoint;

        // State Targets
        [SerializeField] Transform m_MainMenuTarget;
        [SerializeField] Transform m_BagelSelectionTarget;
        [SerializeField] Transform m_BagelTarget;
        [SerializeField] Transform m_GameOverTarget;

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
                    m_CinemachineCamera.Target.TrackingTarget = m_MainMenuTarget;
                    break;
                }
                case PlayManagerState.State.BagelSelection:
                {
                    Clear();
                    m_CinemachineCamera.Target.TrackingTarget = m_BagelSelectionTarget;
                    break;
                }
                case PlayManagerState.State.Playing:
                {
                    Clear();
                    m_CinemachineCamera.Target.TrackingTarget = m_BagelTarget;
                    m_BagelController.gameObject.SetActive(true);
                    m_BagelTracker.gameObject.SetActive(true);
                    break;
                }
                case PlayManagerState.State.GameOver:
                {
                    Clear();
                    m_CinemachineCamera.Target.TrackingTarget = m_GameOverTarget;
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
