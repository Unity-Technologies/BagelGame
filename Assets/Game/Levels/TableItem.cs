using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Bagel
{
    // Something on the table that can be pushed out of place, like the sugar cubes the bagel rolls into, and
    // swept off for a new game (TableClearing). It remembers where it belongs, and comes back up out of the
    // table to it. Held still while the bagel rolls (a kinematic body), unless it starts loose.
    [RequireComponent(typeof(Rigidbody))]
    [NoAutoStaticsCleanup] // the items leave the list as they're destroyed
    public class TableItem : MonoBehaviour
    {
        static readonly List<TableItem> s_Items = new();

        Rigidbody m_Body;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        bool m_StartsKinematic;
        bool m_Disturbed;
        Coroutine m_Rise;

        public static IReadOnlyList<TableItem> items => s_Items;

        // Whether anything has been knocked or swept since the table was last set. Each thing remembers its
        // own, so a table loaded again starts set.
        public static bool disturbed
        {
            get
            {
                foreach (var item in s_Items)
                {
                    if (item.m_Disturbed)
                        return true;
                }
                return false;
            }
        }

        public bool loose => !m_Body.isKinematic;
        public Vector3 homePosition => transform.parent != null ? transform.parent.TransformPoint(m_HomePosition) : m_HomePosition;

        // Swept off, or pushed or turned away from where it belongs.
        public bool outOfPlace => m_Disturbed || !gameObject.activeSelf ||
            Vector3.Distance(transform.localPosition, m_HomePosition) > 0.05f || Quaternion.Angle(transform.localRotation, m_HomeRotation) > 2f;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_StartsKinematic = m_Body.isKinematic;
            m_HomePosition = transform.localPosition;
            m_HomeRotation = transform.localRotation;
            s_Items.Add(this);
        }

        void OnDestroy() => s_Items.Remove(this);

        // Free to fly: the next push moves it. Small things fly fast, so it checks its path for what it'd hit.
        public void Loosen()
        {
            StopRise();
            m_Body.isKinematic = false;
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            m_Body.WakeUp();
            m_Disturbed = true;
        }

        // Sent flying at push (meters a second, whatever it weighs), tumbling.
        public void Sweep(Vector3 push, Vector3 spin)
        {
            Loosen();
            m_Body.AddForce(push, ForceMode.VelocityChange);
            m_Body.AddTorque(spin, ForceMode.VelocityChange);
        }

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
            transform.SetLocalPositionAndRotation(m_HomePosition - Vector3.up * depth, m_HomeRotation);
            m_Rise = StartCoroutine(Rise(depth, duration));
        }

        IEnumerator Rise(float depth, float duration)
        {
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

        void StopRise()
        {
            if (m_Rise == null)
                return;
            StopCoroutine(m_Rise);
            m_Rise = null;
        }

        // Everything is back: the table is set.
        public static void MarkSet()
        {
            foreach (var item in s_Items)
                item.m_Disturbed = false;
        }
    }
}
