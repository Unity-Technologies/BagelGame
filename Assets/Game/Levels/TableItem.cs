using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Bagel
{
    // Something on the table that the bagel can knock about, like the sugar cubes and the jam, or roll into, like
    // the cookbooks, and that's shoved off for a new game (TableClearing). It remembers where it belongs, and comes
    // back up out of the table to it. Held still while the bagel rolls (a kinematic body), unless it starts loose.
    [RequireComponent(typeof(Rigidbody))]
    [NoAutoStaticsCleanup] // the items leave the list as they're destroyed
    public class TableItem : MonoBehaviour
    {
        static readonly List<TableItem> s_Items = new();

        Rigidbody m_Body;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        bool m_StartsKinematic;
        RigidbodyInterpolation m_Interpolation;
        Coroutine m_Rise;

        public static IReadOnlyList<TableItem> items => s_Items;

        public bool loose => !m_Body.isKinematic;
        public Vector3 homePosition => transform.parent != null ? transform.parent.TransformPoint(m_HomePosition) : m_HomePosition;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_StartsKinematic = m_Body.isKinematic;
            m_Interpolation = m_Body.interpolation;
            m_HomePosition = transform.localPosition;
            m_HomeRotation = transform.localRotation;
            s_Items.Add(this);
        }

        void OnDestroy() => s_Items.Remove(this);

        // Coming up out of the table, or going down into it.
        public bool moving => m_Rise != null;

        // Back where it belongs, coming up out of the table over duration seconds.
        public void Restock(float depth, float duration)
        {
            StopRise();
            gameObject.SetActive(true);
            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            m_Body.isKinematic = true;
            // The body goes too, not drawn on the way from where it was (no interpolation until it's moved).
            m_Body.interpolation = RigidbodyInterpolation.None;
            transform.SetLocalPositionAndRotation(m_HomePosition - Vector3.up * depth, m_HomeRotation);
            m_Body.position = transform.position;
            m_Body.rotation = transform.rotation;
            m_Rise = StartCoroutine(Rise(depth, duration));
        }

        IEnumerator Rise(float depth, float duration)
        {
            yield return new WaitForFixedUpdate();
            m_Body.interpolation = m_Interpolation;
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                // Slowly out, and slowly to a stop.
                var t = time / duration;
                var eased = t * t * (3f - 2f * t);
                m_Body.MovePosition(transform.parent.TransformPoint(m_HomePosition - Vector3.up * (depth * (1f - eased))));
                yield return null;
            }
            transform.SetLocalPositionAndRotation(m_HomePosition, m_HomeRotation);
            m_Body.isKinematic = m_StartsKinematic;
            m_Rise = null;
        }

        // Down into the table, for good: the level doesn't have it any more. It's no longer one of the table's things
        // straight away, so a new game doesn't set it again.
        public void Sink(float depth, float duration)
        {
            StopRise();
            s_Items.Remove(this);
            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            m_Body.isKinematic = true;
            if (isActiveAndEnabled)
                m_Rise = StartCoroutine(Lower(depth, duration));
        }

        IEnumerator Lower(float depth, float duration)
        {
            var from = transform.position;
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                // Slowly at first, then faster, as if pulled in.
                var t = time / duration;
                m_Body.MovePosition(from - Vector3.up * (depth * t * t));
                yield return null;
            }
            gameObject.SetActive(false);
            m_Rise = null;
        }

        void StopRise()
        {
            if (m_Rise == null)
                return;
            StopCoroutine(m_Rise);
            m_Rise = null;
            m_Body.interpolation = m_Interpolation;
        }
    }
}
