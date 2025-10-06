using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayCamera : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        [Header("Virtual Cameras")]
        [SerializeField] CinemachineCamera m_MainMenuCamera;
        [SerializeField] CinemachineCamera m_MainMenuSecondaryCamera;
        [SerializeField] CinemachineCamera m_BagelSelectionCamera;
        [SerializeField] CinemachineCamera m_PlayCamera;
        [SerializeField] CinemachineCamera m_GameOverCamera;

        PlayManagerState m_State;

        public float fieldOfView
        {
            get => m_MainMenuCamera.Lens.FieldOfView;
            set => UpdateFieldOfView(value);
        }

        void Awake()
        {
            m_State = m_PlayManager.state;
            m_State.onStateChange += (_, _) => UpdateCamera();
            m_State.onMainMenuPaneModeChange += (_, _) => UpdateCamera();
        }

        void OnEnable()
        {
            UpdateCamera();
        }

        void UpdateFieldOfView(float newFOV)
        {
            m_MainMenuCamera.Lens.FieldOfView = newFOV;
            m_MainMenuSecondaryCamera.Lens.FieldOfView = newFOV;
            m_PlayCamera.Lens.FieldOfView = newFOV;
        }

        void UpdateCamera()
        {
            m_MainMenuCamera.enabled = m_State.currentState == PlayManagerState.State.MainMenu && m_State.currentMainMenuPaneMode == PlayManagerState.MainMenuPaneMode.Primary;
            m_MainMenuSecondaryCamera.enabled = m_State.currentState == PlayManagerState.State.MainMenu && m_State.currentMainMenuPaneMode == PlayManagerState.MainMenuPaneMode.Secondary;
            m_BagelSelectionCamera.enabled = m_State.currentState == PlayManagerState.State.BagelSelection;
            m_PlayCamera.enabled = m_State.currentState == PlayManagerState.State.Playing;
            m_GameOverCamera.enabled = m_State.currentState == PlayManagerState.State.GameOver;
        }
    }
}