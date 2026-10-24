using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The gauges' two wings fold in when the bagel rolls close to a wall, like a car's mirrors, and open
    // again once it's clear. Each wing is a world-space panel turned around its inner edge (its
    // PanelRenderer's pivot): physics finds the walls (a sphere cast out to each side), and a spring
    // moves the wing, so it swings and settles instead of snapping.
    //
    // The wings' panes are VisualElementReferences, set in the Inspector by dragging an element from the
    // Hierarchy. They tell when the pane is there (its UI loaded) and when it's gone: once a wing is mostly
    // folded, its pane gets b-hud--folded and steps back.
    public class BagelHud : MonoBehaviour
    {
        public static readonly UniqueStyleString foldedUssClassName = new("b-hud--folded");

        [SerializeField] Transform m_LeftWing;
        [SerializeField] Transform m_RightWing;
        [SerializeField] VisualElementReference<VisualElement> m_LeftPane = new();
        [SerializeField] VisualElementReference<VisualElement> m_RightPane = new();

        [Space]
        [Tooltip("How far out to each side a wall starts to fold a wing, from the bagel's middle.")]
        [SerializeField] float m_Reach = 1.8f;
        [Tooltip("A wall this close folds a wing all the way.")]
        [SerializeField] float m_Closest = 0.7f;
        [SerializeField] float m_MaxFold = 70f;
        [SerializeField] float m_CastRadius = 0.3f;
        [SerializeField] LayerMask m_Walls = Physics.DefaultRaycastLayers;

        [Space]
        [SerializeField] float m_Stiffness = 140f;
        [SerializeField] float m_Damping = 16f;

        Wing m_Left;
        Wing m_Right;
        VisualElement m_LeftElement;
        VisualElement m_RightElement;

        // One wing: how far it's folded, how fast it's moving, and how it stands unfolded.
        struct Wing
        {
            public Transform transform;
            public Quaternion open;
            public float fold;
            public float speed;
            public bool folded;
        }

        public float leftFold => m_Left.fold;
        public float rightFold => m_Right.fold;

        void Awake()
        {
            m_Left = new Wing { transform = m_LeftWing, open = m_LeftWing != null ? m_LeftWing.localRotation : Quaternion.identity };
            m_Right = new Wing { transform = m_RightWing, open = m_RightWing != null ? m_RightWing.localRotation : Quaternion.identity };
        }

        void OnEnable()
        {
            m_LeftPane.RegisterReferenceResolvedCallback(OnLeftPaneResolved);
            m_LeftPane.RegisterReferenceUnloadedCallback(OnLeftPaneUnloaded);
            m_RightPane.RegisterReferenceResolvedCallback(OnRightPaneResolved);
            m_RightPane.RegisterReferenceUnloadedCallback(OnRightPaneUnloaded);
        }

        void OnDisable()
        {
            m_LeftPane.UnregisterReferenceResolvedCallback(OnLeftPaneResolved);
            m_LeftPane.UnregisterReferenceUnloadedCallback(OnLeftPaneUnloaded);
            m_RightPane.UnregisterReferenceResolvedCallback(OnRightPaneResolved);
            m_RightPane.UnregisterReferenceUnloadedCallback(OnRightPaneUnloaded);

            // Back open, for the next run.
            Open(ref m_Left, m_LeftElement);
            Open(ref m_Right, m_RightElement);
        }

        void OnLeftPaneResolved(VisualElement element) => m_LeftElement = element;
        void OnLeftPaneUnloaded(VisualElement element) => m_LeftElement = null;
        void OnRightPaneResolved(VisualElement element) => m_RightElement = element;
        void OnRightPaneUnloaded(VisualElement element) => m_RightElement = null;

        void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            // Out to each side, level, whichever way the bagel leans.
            var side = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            if (side == Vector3.zero)
                side = transform.right;

            // The left wing swings forward around its right edge, the right one around its left edge.
            Move(ref m_Left, Target(-side), 1f, deltaTime, m_LeftElement);
            Move(ref m_Right, Target(side), -1f, deltaTime, m_RightElement);
        }

        // How far a wing should fold: none with no wall within reach, all the way at the closest.
        float Target(Vector3 direction)
        {
            if (!Physics.SphereCast(transform.position, m_CastRadius, direction, out var hit, m_Reach, m_Walls, QueryTriggerInteraction.Ignore))
                return 0f;
            return m_MaxFold * Mathf.InverseLerp(m_Reach, m_Closest, hit.distance + m_CastRadius);
        }

        void Move(ref Wing wing, float target, float sign, float deltaTime, VisualElement pane)
        {
            if (wing.transform == null)
                return;

            // A damped spring towards the target: semi-implicit Euler, steady at a frame's time steps.
            wing.speed += (m_Stiffness * (target - wing.fold) - m_Damping * wing.speed) * deltaTime;
            wing.fold = Mathf.Clamp(wing.fold + wing.speed * deltaTime, 0f, m_MaxFold);
            wing.transform.localRotation = wing.open * Quaternion.Euler(0f, sign * wing.fold, 0f);

            var folded = wing.fold > m_MaxFold * 0.4f;
            if (folded == wing.folded)
                return;
            wing.folded = folded;
            pane?.EnableInClassList(foldedUssClassName, folded);
        }

        void Open(ref Wing wing, VisualElement pane)
        {
            wing.fold = 0f;
            wing.speed = 0f;
            wing.folded = false;
            if (wing.transform != null)
                wing.transform.localRotation = wing.open;
            pane?.RemoveFromClassList(foldedUssClassName);
        }
    }
}
