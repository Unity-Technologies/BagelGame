using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // Sets the table again for a new game once it's been played on: everything on it (TableItem), moved or not, is
    // shoved off to the right (as the bagel sees it from the start), hard, out toward the alley, where nothing bounces
    // back onto the table. What flies is a copy of each thing as it was (TableLeftover), gone once it gets down to the
    // floor; the things themselves come back up out of the table at the same time, where they belong, as good as new.
    // The copies go through whatever else stands on the table (the course's ramps and platforms, the toaster, the bagel
    // waiting to roll): nothing holds them back, and they don't knock the bagel. Another level clears it the same way,
    // the course too, and nothing comes back: the level's table (BagelLevelTable) sets the new level.
    public class TableClearing : MonoBehaviour
    {
        [SerializeField] float m_SweepSpeed = 16f;
        [SerializeField] float m_SweepLift = 5f;
        [Tooltip("How far below the table things come up from, and how long they take.")]
        [SerializeField] float m_RiseDepth = 1.4f;
        [SerializeField] float m_RiseTime = 1f;
        [Tooltip("The floor's height (world space): what's shoved off goes once it gets down there.")]
        [SerializeField] float m_FloorHeight;

        Coroutine m_Clearing;
        readonly List<TableLeftover> m_Leftovers = new();

        public bool clearing => m_Clearing != null;

        // What the last clearing shoved off, still flying until it's gone (destroyed).
        public IReadOnlyList<TableLeftover> leftovers => m_Leftovers;

        void OnDisable() => m_Clearing = null;

        // A new game on a table that's been played on: everything goes, moved or not. The level's table
        // (BagelLevelTable) calls it, once all the level's things are there.
        public void ClearForANewGame()
        {
            if (m_Clearing != null || !isActiveAndEnabled)
                return;
            m_Clearing = StartCoroutine(Clear(new List<TableItem>(TableItem.items)));
        }

        IEnumerator Clear(List<TableItem> items)
        {
            var things = new List<GameObject>();
            var homes = new List<Vector3>();
            foreach (var item in items)
            {
                if (item != null && item.isActiveAndEnabled)
                {
                    things.Add(item.gameObject);
                    homes.Add(item.homePosition);
                }
            }
            ShoveOff(things, null, homes);
            foreach (var leftover in m_Leftovers)
            {
                // What flies off doesn't knock what's coming back.
                foreach (var item in items)
                {
                    if (item != null)
                        leftover.IgnoreCollisions(item.gameObject);
                }
            }

            foreach (var item in items)
            {
                if (item != null)
                    item.Restock(m_RiseDepth, m_RiseTime);
            }
            yield return new WaitForSeconds(m_RiseTime);
            m_Clearing = null;
        }

        // Another level: the old level's pieces go at once, and what was on them (or the piece itself, a piece of the
        // course) is shoved off as for a new game, the copies flying in the table's parent.
        public void ClearForAnotherLevel(IReadOnlyList<GameObject> pieces)
        {
            if (!isActiveAndEnabled)
                return;
            var things = new List<GameObject>();
            foreach (var piece in pieces)
            {
                if (piece == null)
                    continue;
                var items = piece.GetComponentsInChildren<TableItem>();
                if (items.Length == 0)
                    things.Add(piece);
                foreach (var item in items)
                {
                    if (item.isActiveAndEnabled)
                        things.Add(item.gameObject);
                }
            }
            var where = new List<Vector3>();
            foreach (var thing in things)
                where.Add(thing.transform.position);
            ShoveOff(things, transform.parent, where);
            foreach (var leftover in m_Leftovers)
            {
                foreach (var thing in things)
                    leftover.IgnoreCollisions(thing);
            }
        }

        // What's on the table now, then, doesn't knock what's still flying off it.
        public void IgnoreLeftovers(GameObject piece)
        {
            foreach (var leftover in m_Leftovers)
            {
                if (leftover != null)
                    leftover.IgnoreCollisions(piece);
            }
        }

        // A copy of each thing where it is now, thrown to the right, in parent (or with the thing, without one). A little
        // different for each, the same every time.
        void ShoveOff(List<GameObject> things, Transform parent, List<Vector3> homes)
        {
            m_Leftovers.Clear();
            var right = transform.right;
            for (int i = 0; i < things.Count; i++)
            {
                var wobble = Mathf.Sin(i * 12.9898f) * 0.5f + 0.5f;
                var push = right * m_SweepSpeed * (0.85f + 0.3f * wobble) + Vector3.up * m_SweepLift * (0.7f + 0.6f * wobble);
                var spin = new Vector3(wobble - 0.5f, 1f - wobble, -0.5f) * 8f;
                var into = parent != null ? parent : things[i].transform.parent;
                m_Leftovers.Add(TableLeftover.Make(things[i], into, m_FloorHeight, push, spin));
            }

            // What flies off doesn't knock anything else standing on the table.
            var standing = StandingOnTheTable(homes);
            foreach (var leftover in m_Leftovers)
                leftover.IgnoreCollisions(standing);
        }

        // What stands on the table round its things (where they belong), that isn't one of them: what rises above it
        // further than a tablecloth does (the course's ramps and platforms are sunk into it).
        List<Collider> StandingOnTheTable(List<Vector3> homes)
        {
            const float margin = 0.3f;
            var top = transform.position.y;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var home in homes)
                bounds.Encapsulate(home);
            bounds.Expand(new Vector3(16f, 0f, 8f));
            var center = new Vector3(bounds.center.x, top + 4f, bounds.center.z);
            var extents = new Vector3(bounds.extents.x, 4f, bounds.extents.z);

            var standing = new List<Collider>();
            foreach (var collider in Physics.OverlapBox(center, extents, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var body = collider.attachedRigidbody;
                if (body != null && (body.GetComponent<TableItem>() != null || body.GetComponent<TableLeftover>() != null))
                    continue;
                if (collider.bounds.max.y > top + margin)
                    standing.Add(collider);
            }
            return standing;
        }
    }
}
