using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Bagel
{
    // Settings › Graphics, on the screen: the camera's post-processing and anti-aliasing, the game over shot's
    // depth of field, the sun's shadows and the frame rate. Nothing here changes an asset: the camera, the light
    // and the volume are the scene's, and the frame rate is the player's.
    public class GraphicsQuality : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] Camera m_Camera;
        [SerializeField] Light m_Sun;
        [Tooltip("The game over camera's depth of field, on or off.")]
        [SerializeField] CinemachineVolumeSettings m_GameOverFocus;

        PlayManagerState m_State;
        LightShadows m_SunShadows;

        void OnEnable()
        {
            m_State = m_PlayManager.state;
            m_SunShadows = m_Sun != null ? m_Sun.shadows : LightShadows.None;
            m_State.propertyChanged += State_OnPropertyChanged;
            Apply();
        }

        void OnDisable()
        {
            m_State.propertyChanged -= State_OnPropertyChanged;
            if (m_Sun != null)
                m_Sun.shadows = m_SunShadows;
            Application.targetFrameRate = -1;
        }

        void State_OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            var property = args.propertyName;
            if (property == SettingsData.postProcessingPath || property == SettingsData.antiAliasingPath ||
                property == SettingsData.depthOfFieldPath || property == SettingsData.shadowsPath ||
                property == SettingsData.frameRatePath)
                Apply();
        }

        void Apply()
        {
            var settings = m_State.settings;
            if (m_Camera != null && m_Camera.TryGetComponent<UniversalAdditionalCameraData>(out var camera))
            {
                camera.renderPostProcessing = settings.postProcessing;
                camera.antialiasing = (AntialiasingMode)settings.antiAliasing;
            }
            if (m_GameOverFocus != null)
                m_GameOverFocus.enabled = settings.depthOfField;
            if (m_Sun != null)
                m_Sun.shadows = settings.shadows ? m_SunShadows : LightShadows.None;
            Application.targetFrameRate = settings.frameRate > 0 ? settings.frameRate : -1;
        }
    }
}
