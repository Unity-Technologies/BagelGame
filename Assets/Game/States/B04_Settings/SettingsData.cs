using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [Serializable]
    public class SettingsData
    {
        public static readonly BindingId fieldOfViewPath = Path(nameof(fieldOfView));
        public static readonly BindingId themeIndexPath = Path(nameof(themeIndex));
        public static readonly BindingId reduceMotionPath = Path(nameof(reduceMotion));
        public static readonly BindingId visualEffectsPath = Path(nameof(visualEffects));
        public static readonly BindingId soundPath = Path(nameof(sound));
        public static readonly BindingId soundVolumePath = Path(nameof(soundVolume));
        public static readonly BindingId postProcessingPath = Path(nameof(postProcessing));
        public static readonly BindingId antiAliasingPath = Path(nameof(antiAliasing));
        public static readonly BindingId depthOfFieldPath = Path(nameof(depthOfField));
        public static readonly BindingId shadowsPath = Path(nameof(shadows));
        public static readonly BindingId frameRatePath = Path(nameof(frameRate));

        // The frame rates Settings › Graphics offers; 0 is as fast as it goes.
        public static readonly int[] frameRates = { 30, 60, 120, 0 };

        static BindingId Path(string property) => new BindingId(nameof(PlayManagerState.settings) + "." + property);

        [SerializeField] float m_FieldOfViewMin = 50.0f;
        [SerializeField] float m_FieldOfViewMax = 80.0f;
        [SerializeField] float m_FieldOfView = 60.0f;
        [SerializeField] int m_ThemeIndex;
        [SerializeField] bool m_ReduceMotion;
        [SerializeField] bool m_VisualEffects = true;
        [SerializeField] bool m_Sound = true;
        [SerializeField, Range(0f, 1f)] float m_SoundVolume = 0.8f;
        [SerializeField] bool m_PostProcessing = true;
        [SerializeField, Range(0, 2)] int m_AntiAliasing;
        [SerializeField] bool m_DepthOfField = true;
        [SerializeField] bool m_Shadows = true;
        [SerializeField] int m_FrameRate;

        [NonSerialized] PlayManagerState m_Owner;

        [CreateProperty] public float fieldOfViewMin => m_FieldOfViewMin;
        [CreateProperty] public float fieldOfViewMax => m_FieldOfViewMax;

        [CreateProperty]
        public float fieldOfView
        {
            get => m_FieldOfView;
            set
            {
                value = Mathf.Clamp(value, m_FieldOfViewMin, m_FieldOfViewMax);
                if (m_FieldOfView == value)
                    return;
                m_FieldOfView = value;
                Notify(fieldOfViewPath);
            }
        }

        [CreateProperty]
        public int themeIndex
        {
            get => m_ThemeIndex;
            set
            {
                value = Mathf.Max(0, value);
                if (m_ThemeIndex == value)
                    return;
                m_ThemeIndex = value;
                Notify(themeIndexPath);
            }
        }

        // Turns off transitions and UI animations.
        [CreateProperty]
        public bool reduceMotion
        {
            get => m_ReduceMotion;
            set
            {
                if (m_ReduceMotion == value)
                    return;
                m_ReduceMotion = value;
                Notify(reduceMotionPath);
            }
        }

        // Blurs and drop shadows: the most expensive things the UI draws.
        [CreateProperty]
        public bool visualEffects
        {
            get => m_VisualEffects;
            set
            {
                if (m_VisualEffects == value)
                    return;
                m_VisualEffects = value;
                Notify(visualEffectsPath);
            }
        }

        [CreateProperty]
        public bool sound
        {
            get => m_Sound;
            set
            {
                if (m_Sound == value)
                    return;
                m_Sound = value;
                Notify(soundPath);
            }
        }

        [CreateProperty]
        public float soundVolume
        {
            get => m_SoundVolume;
            set
            {
                value = Mathf.Clamp01(value);
                if (m_SoundVolume == value)
                    return;
                m_SoundVolume = value;
                Notify(soundVolumePath);
            }
        }

        // Bloom, color grading and the vignette: the camera's post-processing.
        [CreateProperty]
        public bool postProcessing
        {
            get => m_PostProcessing;
            set
            {
                if (m_PostProcessing == value)
                    return;
                m_PostProcessing = value;
                Notify(postProcessingPath);
            }
        }

        // The camera's anti-aliasing on top of the pipeline's: 0 off, 1 FXAA, 2 SMAA.
        [CreateProperty]
        public int antiAliasing
        {
            get => m_AntiAliasing;
            set
            {
                value = Mathf.Clamp(value, 0, 2);
                if (m_AntiAliasing == value)
                    return;
                m_AntiAliasing = value;
                Notify(antiAliasingPath);
            }
        }

        // The game over shot's depth of field: the toaster soft in front of the television.
        [CreateProperty]
        public bool depthOfField
        {
            get => m_DepthOfField;
            set
            {
                if (m_DepthOfField == value)
                    return;
                m_DepthOfField = value;
                Notify(depthOfFieldPath);
            }
        }

        // The sun's shadows.
        [CreateProperty]
        public bool shadows
        {
            get => m_Shadows;
            set
            {
                if (m_Shadows == value)
                    return;
                m_Shadows = value;
                Notify(shadowsPath);
            }
        }

        // Frames a second at most, one of frameRates; 0 is as fast as it goes.
        [CreateProperty]
        public int frameRate
        {
            get => m_FrameRate;
            set
            {
                value = System.Array.IndexOf(frameRates, value) >= 0 ? value : 0;
                if (m_FrameRate == value)
                    return;
                m_FrameRate = value;
                Notify(frameRatePath);
            }
        }

        // Everything back as a new game has it (what the leaderboard keeps isn't a setting).
        public void Reset()
        {
            var defaults = new SettingsData();
            fieldOfView = defaults.m_FieldOfView;
            themeIndex = defaults.m_ThemeIndex;
            reduceMotion = defaults.m_ReduceMotion;
            visualEffects = defaults.m_VisualEffects;
            sound = defaults.m_Sound;
            soundVolume = defaults.m_SoundVolume;
            postProcessing = defaults.m_PostProcessing;
            antiAliasing = defaults.m_AntiAliasing;
            depthOfField = defaults.m_DepthOfField;
            shadows = defaults.m_Shadows;
            frameRate = defaults.m_FrameRate;
        }

        internal void Bind(PlayManagerState owner) => m_Owner = owner;

        void Notify(in BindingId path) => m_Owner?.Notify(path);
    }
}
