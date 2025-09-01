using Unity.Cinemachine;
using UnityEngine;

namespace Bagel
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class PlayCinemaCamera : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] Transform m_MainMenuTarget;
        [SerializeField] Transform m_BagelSelectionTarget;
        [SerializeField] Transform m_BagelTarget;

        [Space]
        [SerializeField] float m_GameOverOrbitSpeed = 0.02f;
        [SerializeField] Transform m_GameOverRoot;
        [SerializeField] Transform m_GameOverTarget;

        CinemachineCamera m_CinemachineCamera;

        void Awake()
        {
            m_CinemachineCamera = GetComponent<CinemachineCamera>();
            m_PlayManager.State.OnStateChange += State_OnStateChange;
        }

        void Update()
        {
            if (!m_PlayManager.State.IsGameOver)
                return;

            m_GameOverRoot.Rotate(Vector3.up, m_GameOverOrbitSpeed);
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            switch (state)
            {
                case PlayManagerState.State.MainMenu:
                {
                    m_CinemachineCamera.Target.TrackingTarget = m_MainMenuTarget;
                    break;
                }
                case PlayManagerState.State.BagelSelection:
                {
                    m_CinemachineCamera.Target.TrackingTarget = m_BagelSelectionTarget;
                    break;
                }
                case PlayManagerState.State.Playing:
                {
                    m_CinemachineCamera.Target.TrackingTarget = m_BagelTarget;
                    break;
                }
                case PlayManagerState.State.GameOver:
                {
                    m_GameOverRoot.rotation = Quaternion.identity;

                    var gameOverPosition = m_GameOverRoot.position;
                    var bagelPosition = m_BagelTarget.position;
                    m_GameOverRoot.position = new Vector3(
                        bagelPosition.x,
                        gameOverPosition.y,
                        bagelPosition.z
                    );

                    m_CinemachineCamera.Target.TrackingTarget = m_GameOverTarget;
                    break;
                }
            }
        }
    }
}
