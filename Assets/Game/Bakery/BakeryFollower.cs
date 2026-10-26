using UnityEngine;

namespace Bagel
{
    // Something in the room that goes with its far end when the room's made longer or shorter for a level
    // (Bakery.Stretch): what hangs on the far wall, or looks at it, moves with the wall; what runs the room's length
    // (trims, the header beam, the reflections' box) stretches between the walls; what hangs from the ceiling at its
    // place (beams, lamps) is only there while the room reaches past it.
    public class BakeryFollower : MonoBehaviour
    {
        public enum Mode
        {
            FarWall,
            AlongTheRoom,
            UnderTheCeiling
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }

        [SerializeField] Mode m_Mode;
        [Tooltip("Far Wall: where it is from the far wall (z, into the room is negative). Under The Ceiling: how far from the far wall it has to be to be there.")]
        [SerializeField] float m_Offset;
        [Tooltip("Along The Room: where its ends are from the near wall and from the far wall (z).")]
        [SerializeField] float m_NearOffset;
        [SerializeField] float m_FarOffset;
        [Tooltip("Along The Room: which of its own axes lies along the room (its scale on it is its length).")]
        [SerializeField] Axis m_Axis = Axis.Z;

        Bakery m_Bakery;
        Renderer[] m_Renderers;
        Light[] m_Lights;
        bool? m_Shown;

        public Mode mode => m_Mode;
        public bool shown => m_Shown ?? true;

        // Set from where it is in the room as it is now.
        public void Fit(Mode mode, Bakery bakery, Axis axis = Axis.Z, float margin = 0f)
        {
            m_Mode = mode;
            m_Axis = axis;
            var z = transform.position.z;
            switch (mode)
            {
                case Mode.FarWall:
                    m_Offset = z - bakery.northSide;
                    break;
                case Mode.AlongTheRoom:
                    var half = Length() * 0.5f;
                    m_NearOffset = z - half - bakery.southSide;
                    m_FarOffset = z + half - bakery.northSide;
                    break;
                case Mode.UnderTheCeiling:
                    m_Offset = -margin;
                    break;
            }
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            m_Bakery = FindAnyObjectByType<Bakery>();
            if (m_Bakery == null)
                return;
            m_Bakery.resized += Follow;
            Follow();
        }

        void OnDisable()
        {
            if (m_Bakery != null)
                m_Bakery.resized -= Follow;
            m_Bakery = null;
        }

        void Follow()
        {
            var north = m_Bakery.northSide;
            var position = transform.position;
            switch (m_Mode)
            {
                case Mode.FarWall:
                    position.z = north + m_Offset;
                    transform.position = position;
                    break;
                case Mode.AlongTheRoom:
                    var near = m_Bakery.southSide + m_NearOffset;
                    var far = north + m_FarOffset;
                    position.z = (near + far) * 0.5f;
                    transform.position = position;
                    SetLength(far - near);
                    break;
                case Mode.UnderTheCeiling:
                    Show(position.z <= north + m_Offset);
                    break;
            }
        }

        // How long it is along the room (meters): its scale on its axis, or a reflection probe's box.
        float Length()
        {
            if (TryGetComponent<ReflectionProbe>(out var probe))
                return probe.size.z;
            return Mathf.Abs(transform.localScale[(int)m_Axis]);
        }

        void SetLength(float length)
        {
            if (TryGetComponent<ReflectionProbe>(out var probe))
            {
                var size = probe.size;
                size.z = length;
                probe.size = size;
                return;
            }
            var scale = transform.localScale;
            scale[(int)m_Axis] = Mathf.Sign(scale[(int)m_Axis]) * length;
            transform.localScale = scale;
        }

        void Show(bool show)
        {
            if (m_Shown == show)
                return;
            m_Shown = show;
            m_Renderers ??= GetComponentsInChildren<Renderer>(true);
            m_Lights ??= GetComponentsInChildren<Light>(true);
            foreach (var renderer in m_Renderers)
                renderer.enabled = show;
            foreach (var light in m_Lights)
                light.enabled = show;
        }
    }
}
