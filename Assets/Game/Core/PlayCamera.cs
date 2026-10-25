using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class PlayCamera : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        [Header("Tracking Elements")]
        [SerializeField] VisualElementReference m_MainMenuSecondaryPane;

        [Header("Virtual Cameras")]
        [SerializeField] CinemachineCamera m_MainMenuCamera;
        [SerializeField] CinemachineCamera m_MainMenuSecondaryCamera;
        [SerializeField] CinemachineCamera m_BagelSelectionCamera;
        [SerializeField] CinemachineCamera m_PlayCamera;
        [Tooltip("Looks past the toaster at the television: at game over, and for the main menu's leaderboard.")]
        [SerializeField] CinemachineCamera m_GameOverCamera;

        [Header("Framing")]
        [Tooltip("What the Settings camera keeps in view, whatever the screen's shape and the field of view: the whiteboard and a margin around it (meters, across and up). It orbits its target on the board at that distance.")]
        [SerializeField] Vector2 m_SettingsFrame = new(36f, 22f);

        PlayManagerState m_State;
        CinemachineOrbitalFollow m_SettingsOrbit;
        Camera m_OutputCamera;

        public float fieldOfView
        {
            get => m_MainMenuCamera.Lens.FieldOfView;
            set => UpdateFieldOfView(value);
        }

        void OnEnable()
        {
            m_SettingsOrbit = m_MainMenuSecondaryCamera.GetComponent<CinemachineOrbitalFollow>();
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;
            m_State.onMainMenuPaneModeChange += State_OnMainMenuPaneModeChange;
            m_State.propertyChanged += State_OnPropertyChanged;

            UpdateCamera();
            UpdateFieldOfView(m_State.settings.fieldOfView);
        }

        void OnDisable()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.onMainMenuPaneModeChange -= State_OnMainMenuPaneModeChange;
            m_State.propertyChanged -= State_OnPropertyChanged;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state) => UpdateCamera();

        void State_OnMainMenuPaneModeChange(object sender, PlayManagerState.MainMenuPaneMode mode) => UpdateCamera();

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName.Equals(SettingsData.fieldOfViewPath))
                UpdateFieldOfView(m_State.settings.fieldOfView);
            else if (args.propertyName.Equals(PlayManagerState.mainMenuPanePath))
                UpdateCamera();
        }

        // The whiteboard fits on screen with its margin: as far back as the narrower of its width and height needs.
        void LateUpdate()
        {
            if (m_SettingsOrbit == null)
                return;
            if (m_OutputCamera == null)
                m_OutputCamera = Camera.main;
            var aspect = m_OutputCamera != null ? m_OutputCamera.aspect : 16f / 9f;
            var tan = Mathf.Tan(m_MainMenuSecondaryCamera.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            m_SettingsOrbit.Radius = Mathf.Max(m_SettingsFrame.y * 0.5f / tan, m_SettingsFrame.x * 0.5f / (tan * aspect));
        }

        void UpdateFieldOfView(float newFOV)
        {
            m_MainMenuCamera.Lens.FieldOfView = newFOV;
            m_MainMenuSecondaryCamera.Lens.FieldOfView = newFOV;
            m_PlayCamera.Lens.FieldOfView = newFOV;
        }

        void UpdateCamera()
        {
            var mainMenu = m_State.currentState == PlayManagerState.State.MainMenu;
            m_MainMenuCamera.enabled = mainMenu && m_State.mainMenuPane == PlayManagerState.MainMenuPane.Main;
            m_MainMenuSecondaryCamera.enabled = mainMenu && m_State.mainMenuPane == PlayManagerState.MainMenuPane.Settings;
            m_BagelSelectionCamera.enabled = m_State.currentState == PlayManagerState.State.BagelSelection;
            m_PlayCamera.enabled = m_State.currentState == PlayManagerState.State.Playing;
            m_GameOverCamera.enabled = m_State.currentState == PlayManagerState.State.GameOver ||
                (mainMenu && m_State.mainMenuPane == PlayManagerState.MainMenuPane.Leaderboard);
        }
    }
}
