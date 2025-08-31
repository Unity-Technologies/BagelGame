using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class PlayManager : MonoBehaviour, IPlayManager
    {
        [SerializeField] PlayerInputBindings m_PlayerInputBindings;
        [SerializeField] CinemachineCamera m_CinemachineCamera;
        [SerializeField] BagelController m_BagelController;
        [SerializeField] Transform m_StartingPoint;

        // State Targets
        [SerializeField] Transform m_MainMenuTarget;
        [SerializeField] Transform m_BagelTarget;
        [SerializeField] Transform m_GameOverTarget;

        PlayManagerState m_PlayManagerState = new PlayManagerState();

        void OnEnable()
        {
            GoToMainMenu();
        }

        void Start()
        {
            m_PlayerInputBindings.OnPauseAction += PlayerInputBindings_OnPauseAction;
        }

        void PlayerInputBindings_OnPauseAction(object sender, EventArgs e)
        {
            if (!m_PlayManagerState.IsPlaying())
                return;

            if (m_PlayManagerState.IsPaused())
                Resume();
            else
                Pause();
        }

        public event EventHandler<bool> OnPauseStateChanged {
            add { m_PlayManagerState.OnPauseStateChanged += value; }
            remove { m_PlayManagerState.OnPauseStateChanged -= value; }
        }

        void Clear()
        {
            m_BagelController.transform.position = m_StartingPoint.position;
            m_BagelController.enabled = false;
        }

        public bool IsPlaying()
        {
            return m_PlayManagerState.IsPlaying();
        }

        public bool IsPaused()
        {
            return m_PlayManagerState.IsPaused();
        }

        public void GoToMainMenu()
        {
            Clear();
            m_PlayManagerState.GoToMainMenu();
            m_CinemachineCamera.Target.TrackingTarget = m_MainMenuTarget;
        }

        public void GoToPlay()
        {
            Clear();
            m_PlayManagerState.GoToPlay();
            m_CinemachineCamera.Target.TrackingTarget = m_BagelTarget;
            m_BagelController.enabled = true;
        }

        public void Pause()
        {
            m_PlayManagerState.Pause();
            m_BagelController.enabled = false;
        }

        public void Resume()
        {
            m_PlayManagerState.Resume();
            m_BagelController.enabled = true;
        }

        public void GoToGameOver()
        {
            Clear();
            m_CinemachineCamera.Target.TrackingTarget = m_GameOverTarget;
        }
    }
}
