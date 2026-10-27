using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // A copy of something on the table, shoved off (TableClearing): for a new game, while the thing itself comes back up
    // where it belongs, or for another level, while the thing's gone. It flies, with a light body of its own (all the
    // copies weigh the same, the big ones as little as the small: nothing holds the others back), until it gets down to
    // the floor, then shrinks away. Caught on something on the way, it's shoved again, until it's off.
    public class TableLeftover : MonoBehaviour
    {
        const float k_MinAge = 0.2f;
        const float k_MaxAge = 8f;
        const float k_ShrinkTime = 0.25f;
        const float k_OnTheFloor = 0.25f;
        const float k_StuckSpeed = 0.5f;
        const float k_StuckTime = 0.3f;
        const float k_Mass = 0.2f;

        float m_FloorHeight;
        Vector3 m_Push;
        float m_StuckFor;
        float m_Age;
        bool m_Going;
        Collider[] m_Colliders;

        public Rigidbody body { get; private set; }

        // Off it goes, at push (meters a second, whatever it weighs), tumbling: a table item, or a piece of the course,
        // which gets a body of its own and goes through what it's sunk into (the cloth, the counter).
        public static TableLeftover Make(GameObject thing, Transform parent, float floorHeight, Vector3 push, Vector3 spin)
        {
            var sunkInto = new List<Collider>();
            if (!thing.TryGetComponent<Rigidbody>(out _))
            {
                foreach (var collider in thing.GetComponentsInChildren<Collider>())
                {
                    var bounds = collider.bounds;
                    sunkInto.AddRange(Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
                }
            }

            // Copied under a hidden parent, so nothing on it wakes up before it's no longer a table item: the table
            // doesn't count it.
            var holder = new GameObject(nameof(TableLeftover));
            holder.SetActive(false);
            var copy = Instantiate(thing, holder.transform, true);
            if (copy.TryGetComponent<TableItem>(out var item))
                DestroyImmediate(item);
            copy.name = thing.name;
            copy.transform.SetParent(parent, true);
            Destroy(holder);

            var leftover = copy.AddComponent<TableLeftover>();
            leftover.m_FloorHeight = floorHeight;
            leftover.body = copy.TryGetComponent<Rigidbody>(out var body) ? body : copy.AddComponent<Rigidbody>();
            leftover.body.isKinematic = false;
            leftover.body.mass = k_Mass;
            leftover.body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            leftover.m_Colliders = copy.GetComponentsInChildren<Collider>();
            leftover.IgnoreCollisions(sunkInto);
            leftover.m_Push = push;
            leftover.body.AddForce(push, ForceMode.VelocityChange);
            leftover.body.AddTorque(spin, ForceMode.VelocityChange);
            return leftover;
        }

        public void IgnoreCollisions(IReadOnlyList<Collider> others)
        {
            foreach (var other in others)
            {
                foreach (var collider in m_Colliders)
                    Physics.IgnoreCollision(collider, other);
            }
        }

        public void IgnoreCollisions(GameObject other)
        {
            foreach (var otherCollider in other.GetComponentsInChildren<Collider>())
            {
                foreach (var collider in m_Colliders)
                    Physics.IgnoreCollision(collider, otherCollider);
            }
        }

        void FixedUpdate()
        {
            m_Age += Time.fixedDeltaTime;
            if (m_Going)
                return;
            if (m_Age > k_MaxAge || m_Age > k_MinAge && Lowest() <= m_FloorHeight + k_OnTheFloor)
            {
                StartCoroutine(Shrink());
                return;
            }

            m_StuckFor = body.linearVelocity.magnitude < k_StuckSpeed ? m_StuckFor + Time.fixedDeltaTime : 0f;
            if (m_StuckFor > k_StuckTime)
            {
                body.AddForce(m_Push, ForceMode.VelocityChange);
                m_StuckFor = 0f;
            }
        }

        float Lowest()
        {
            var lowest = float.MaxValue;
            foreach (var collider in m_Colliders)
                lowest = Mathf.Min(lowest, collider.bounds.min.y);
            return lowest;
        }

        IEnumerator Shrink()
        {
            m_Going = true;
            var scale = transform.localScale;
            for (var time = 0f; time < k_ShrinkTime; time += Time.deltaTime)
            {
                var t = time / k_ShrinkTime;
                transform.localScale = scale * (1f - t * t);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
