using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [Serializable]
    public class BagelTrackerData
    {
        static readonly BindingId k_ConstantsPath = Path(nameof(constants));
        public static readonly BindingId toppingsCountPath = Path(nameof(toppingsCount));
        static readonly BindingId k_ToppingsMaxCountPath = Path(nameof(toppingsMaxCount));
        static readonly BindingId k_InputPath = Path(nameof(input));
        static readonly BindingId k_SpeedPath = Path(nameof(speed));
        static readonly BindingId k_ForcePath = Path(nameof(force));
        static readonly BindingId k_SpinPath = Path(nameof(spin));
        static readonly BindingId k_ElapsedPath = Path(nameof(elapsed));

        static BindingId Path(string property) => new BindingId(nameof(PlayManagerState.tracker) + "." + property);

        [SerializeField] BagelTrackerConstants m_Constants;
        [SerializeField] int m_ToppingsCount;
        [SerializeField] int m_ToppingsMaxCount;
        [SerializeField] float m_Input;
        [SerializeField] float m_Speed;
        [SerializeField] float m_Force;
        [SerializeField] float m_Spin;
        [SerializeField] float m_Elapsed;

        [NonSerialized] PlayManagerState m_Owner;

        [CreateProperty]
        public BagelTrackerConstants constants
        {
            get => m_Constants;
            set { if (m_Constants == value) return; m_Constants = value; Notify(k_ConstantsPath); }
        }

        [CreateProperty]
        public int toppingsCount
        {
            get => m_ToppingsCount;
            set { if (m_ToppingsCount == value) return; m_ToppingsCount = value; Notify(toppingsCountPath); }
        }

        [CreateProperty]
        public int toppingsMaxCount
        {
            get => m_ToppingsMaxCount;
            set { if (m_ToppingsMaxCount == value) return; m_ToppingsMaxCount = value; Notify(k_ToppingsMaxCountPath); }
        }

        [CreateProperty]
        public float input
        {
            get => m_Input;
            set { if (m_Input == value) return; m_Input = value; Notify(k_InputPath); }
        }

        [CreateProperty]
        public float speed
        {
            get => m_Speed;
            set { if (m_Speed == value) return; m_Speed = value; Notify(k_SpeedPath); }
        }

        [CreateProperty]
        public float force
        {
            get => m_Force;
            set { if (m_Force == value) return; m_Force = value; Notify(k_ForcePath); }
        }

        [CreateProperty]
        public float spin
        {
            get => m_Spin;
            set { if (m_Spin == value) return; m_Spin = value; Notify(k_SpinPath); }
        }

        // Seconds since the run started (paused time doesn't count): what the run took, against the toppings it kept.
        [CreateProperty]
        public float elapsed
        {
            get => m_Elapsed;
            set { if (m_Elapsed == value) return; m_Elapsed = value; Notify(k_ElapsedPath); }
        }

        internal void Bind(PlayManagerState owner) => m_Owner = owner;

        public void Reset(int maxToppings)
        {
            toppingsMaxCount = maxToppings;
            toppingsCount = maxToppings;
            input = 0f;
            speed = 0f;
            force = 0f;
            spin = 0f;
            elapsed = 0f;
        }

        void Notify(in BindingId path) => m_Owner?.Notify(path);
    }
}
