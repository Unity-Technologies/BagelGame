using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    public class PlayCamera : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        [Header("Virtual Cameras")]
        [SerializeField] CinemachineCamera m_MainMenuCamera;
        [SerializeField] CinemachineCamera m_MainMenuSettingsCamera;
        [SerializeField] CinemachineCamera m_BagelSelectionCamera;
        [SerializeField] CinemachineCamera m_GameplayCamera;
        [SerializeField] CinemachineCamera m_GameOverCamera;

        PlayManagerState m_State;

        void Awake()
        {
            m_State = m_PlayManager.State;
            m_State.OnStateChange += (_, _) => UpdateCamera();
            m_State.OnMainMenuPaneModeChange += (_, _) => UpdateCamera();
        }

        void OnEnable()
        {
            UpdateCamera();
        }

        void UpdateCamera()
        {
            m_MainMenuCamera.enabled = m_State.CurrentState == PlayManagerState.State.MainMenu && m_State.CurrentMainMenuPaneMode == PlayManagerState.MainMenuPaneMode.Primary;
            m_MainMenuSettingsCamera.enabled = m_State.CurrentState == PlayManagerState.State.MainMenu && m_State.CurrentMainMenuPaneMode == PlayManagerState.MainMenuPaneMode.Secondary;
            m_BagelSelectionCamera.enabled = m_State.CurrentState == PlayManagerState.State.BagelSelection;
            m_GameplayCamera.enabled = m_State.CurrentState == PlayManagerState.State.Playing;
            m_GameOverCamera.enabled = m_State.CurrentState == PlayManagerState.State.GameOver;
        }
    }
}