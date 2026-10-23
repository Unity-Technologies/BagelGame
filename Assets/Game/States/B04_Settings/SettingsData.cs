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

        static BindingId Path(string property) => new BindingId(nameof(PlayManagerState.settings) + "." + property);

        [SerializeField] float m_FieldOfViewMin = 50.0f;
        [SerializeField] float m_FieldOfViewMax = 80.0f;
        [SerializeField] float m_FieldOfView = 60.0f;
        [SerializeField] int m_ThemeIndex;
        [SerializeField] bool m_ReduceMotion;
        [SerializeField] bool m_VisualEffects = true;

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

        internal void Bind(PlayManagerState owner) => m_Owner = owner;

        void Notify(in BindingId path) => m_Owner?.Notify(path);
    }
}
