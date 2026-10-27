using System;
using UnityEngine;

namespace Bagel
{
    // Something in the bakery that stays put on the room's walls, ceiling, counter or blackboard as the room's
    // measurements change (Bakery): in the Editor as they're edited, and in play as the room's made longer or shorter
    // for a level. Like a UI element's anchors, on each of the room's axes it keeps its distance from a side of one of
    // the room's parts (Bakery.Part): its start side (west, the floor, the near wall), its middle, its end side (east,
    // the ceiling, the far wall), or both, stretching between them (the trims, the beams, the blackboard's rails, the
    // reflections' box). Moved by hand, or anchored differently, it stays where it is, and keeps those distances from
    // then on. What hangs from the ceiling at its place (beams, lamps) can be there only while the room reaches past it.
    [ExecuteAlways]
    public class BakeryAnchor : MonoBehaviour
    {
        public enum Side
        {
            Start,
            Middle,
            End,
            Both
        }

        // What it keeps its distance from on one of the room's axes.
        [Serializable]
        public struct Axis : IEquatable<Axis>
        {
            public Bakery.Part part;
            public Side side;

            public Axis(Bakery.Part part, Side side)
            {
                this.part = part;
                this.side = side;
            }

            public bool Equals(Axis other) => part == other.part && side == other.side;
        }

        [Tooltip("Across the room (x): its start is the west wall, its end the east side.")]
        [SerializeField] Axis m_Across;
        [Tooltip("Up (y): its start is the floor, its end the ceiling.")]
        [SerializeField] Axis m_Up;
        [Tooltip("Along the room (z): its start is the near wall, its end the far wall.")]
        [SerializeField] Axis m_Along;
        [Tooltip("Only there while it's this far inside the far wall, at least: the far wall comes nearer for a short level, past the beams and the lamps on the ceiling.")]
        [SerializeField] bool m_OnlyInTheRoom;
        [SerializeField] float m_Margin;

        // Its distances from its sides (its start's, stretching, with its end's from the end sides), how it was anchored
        // when they were taken, and where it was put: to tell when it's been anchored differently, or moved by hand.
        [SerializeField, HideInInspector] Vector3 m_Offset;
        [SerializeField, HideInInspector] Vector3 m_EndOffset;
        [SerializeField, HideInInspector] bool m_Fitted;
        [SerializeField, HideInInspector] Axis m_FittedAcross;
        [SerializeField, HideInInspector] Axis m_FittedUp;
        [SerializeField, HideInInspector] Axis m_FittedAlong;
        [SerializeField, HideInInspector] Vector3 m_PlacedPosition;
        [SerializeField, HideInInspector] Vector3 m_PlacedScale;

        Bakery m_Bakery;
        Renderer[] m_Renderers;
        Light[] m_Lights;
        bool? m_Shown;

        public Axis across => m_Across;
        public Axis up => m_Up;
        public Axis along => m_Along;
        public bool shown => m_Shown ?? true;

        Axis this[int axis] => axis == 0 ? m_Across : axis == 1 ? m_Up : m_Along;

        // Anchored like this from now on, where it is.
        public void Anchor(Axis across, Axis up, Axis along, bool onlyInTheRoom = false, float margin = 0f)
        {
            m_Across = across;
            m_Up = up;
            m_Along = along;
            m_OnlyInTheRoom = onlyInTheRoom;
            m_Margin = margin;
            m_Fitted = false;
            Follow();
        }

        void OnEnable()
        {
            m_Bakery = GetComponentInParent<Bakery>();
            if (m_Bakery == null)
                m_Bakery = FindAnyObjectByType<Bakery>();
            if (m_Bakery == null)
                return;
            m_Bakery.changed += Follow;
            Follow();
        }

        void OnDisable()
        {
            if (m_Bakery != null)
                m_Bakery.changed -= Follow;
            m_Bakery = null;
        }

        void Follow()
        {
            if (m_Bakery == null)
                return;
            var anchoredDifferently = !m_Across.Equals(m_FittedAcross) || !m_Up.Equals(m_FittedUp) || !m_Along.Equals(m_FittedAlong);
            var moved = transform.localPosition != m_PlacedPosition || transform.localScale != m_PlacedScale;
            if (!m_Fitted || anchoredDifferently || moved)
                Fit();
            Place();
        }

        // Its distances from its sides, as it is now.
        void Fit()
        {
            var position = m_Bakery.transform.InverseTransformPoint(transform.position);
            for (int axis = 0; axis < 3; axis++)
            {
                var bounds = m_Bakery.GetBounds(this[axis].part);
                switch (this[axis].side)
                {
                    case Side.Start:
                        m_Offset[axis] = position[axis] - bounds.min[axis];
                        break;
                    case Side.Middle:
                        m_Offset[axis] = position[axis] - bounds.center[axis];
                        break;
                    case Side.End:
                        m_Offset[axis] = position[axis] - bounds.max[axis];
                        break;
                    case Side.Both:
                        var half = GetLength(axis) * 0.5f;
                        m_Offset[axis] = position[axis] - half - bounds.min[axis];
                        m_EndOffset[axis] = position[axis] + half - bounds.max[axis];
                        break;
                }
            }
            m_Fitted = true;
            m_FittedAcross = m_Across;
            m_FittedUp = m_Up;
            m_FittedAlong = m_Along;
        }

        // Where its distances from its sides put it, in the room as it is now.
        void Place()
        {
            var position = m_Bakery.transform.InverseTransformPoint(transform.position);
            for (int axis = 0; axis < 3; axis++)
            {
                var bounds = m_Bakery.GetBounds(this[axis].part);
                switch (this[axis].side)
                {
                    case Side.Start:
                        position[axis] = bounds.min[axis] + m_Offset[axis];
                        break;
                    case Side.Middle:
                        position[axis] = bounds.center[axis] + m_Offset[axis];
                        break;
                    case Side.End:
                        position[axis] = bounds.max[axis] + m_Offset[axis];
                        break;
                    case Side.Both:
                        var start = bounds.min[axis] + m_Offset[axis];
                        var end = bounds.max[axis] + m_EndOffset[axis];
                        position[axis] = (start + end) * 0.5f;
                        SetLength(axis, end - start);
                        break;
                }
            }
            var world = m_Bakery.transform.TransformPoint(position);
            if (transform.position != world)
                transform.position = world;
            m_PlacedPosition = transform.localPosition;
            m_PlacedScale = transform.localScale;

            if (m_OnlyInTheRoom)
                Show(position.z <= m_Bakery.GetBounds(Bakery.Part.Room).max.z - m_Margin);
        }

        // How long it is along one of the room's axes: a reflection probe's box, or its mesh as it's scaled on its own
        // axis that lies along it.
        float GetLength(int axis)
        {
            if (TryGetComponent<ReflectionProbe>(out var probe))
                return probe.size[axis];
            var own = OwnAxis(axis);
            return Mathf.Abs(transform.localScale[own]) * MeshSize(own);
        }

        void SetLength(int axis, float length)
        {
            if (TryGetComponent<ReflectionProbe>(out var probe))
            {
                var size = probe.size;
                if (size[axis] == length)
                    return;
                size[axis] = length;
                probe.size = size;
                return;
            }
            var own = OwnAxis(axis);
            var scale = transform.localScale;
            var wanted = Mathf.Sign(scale[own]) * length / MeshSize(own);
            if (scale[own] == wanted)
                return;
            scale[own] = wanted;
            transform.localScale = scale;
        }

        // Which of its own axes lies along one of the room's.
        int OwnAxis(int axis)
        {
            var rotation = Quaternion.Inverse(m_Bakery.transform.rotation) * transform.rotation;
            var best = 0;
            var most = -1f;
            for (int own = 0; own < 3; own++)
            {
                var direction = Vector3.zero;
                direction[own] = 1f;
                var along = Mathf.Abs((rotation * direction)[axis]);
                if (along > most)
                {
                    most = along;
                    best = own;
                }
            }
            return best;
        }

        float MeshSize(int own)
        {
            if (TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null && filter.sharedMesh.bounds.size[own] > 0f)
                return filter.sharedMesh.bounds.size[own];
            return 1f;
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
