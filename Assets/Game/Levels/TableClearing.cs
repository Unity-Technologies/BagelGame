using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // Sets the table again for a new game: everything on it (TableItem), moved or not, is shoved off to the right (as
    // the bagel sees it from the start), out toward the alley, where nothing bounces back onto the table. What flies
    // is a copy of each thing as it was (TableLeftover), gone once it gets down to the floor; the things themselves
    // come back up out of the table at the same time, where they belong, as good as new.
    public class TableClearing : MonoBehaviour
    {
        [SerializeField] float m_SweepSpeed = 10f;
        [SerializeField] float m_SweepLift = 5f;
        [Tooltip("How far below the table things come up from, and how long they take.")]
        [SerializeField] float m_RiseDepth = 1.4f;
        [SerializeField] float m_RiseTime = 1f;
        [Tooltip("The floor's height (world space): what's shoved off goes once it gets down there.")]
        [SerializeField] float m_FloorHeight;

        PlayManager m_PlayManager;
        Coroutine m_Clearing;
        readonly List<TableLeftover> m_Leftovers = new();

        public bool clearing => m_Clearing != null;

        // What the last clearing shoved off, still flying until it's gone (destroyed).
        public IReadOnlyList<TableLeftover> leftovers => m_Leftovers;

        void OnEnable()
        {
            m_PlayManager = FindAnyObjectByType<PlayManager>();
            if (m_PlayManager != null)
                m_PlayManager.state.onStateChange += State_OnStateChange;
        }

        void OnDisable()
        {
            if (m_PlayManager != null)
                m_PlayManager.state.onStateChange -= State_OnStateChange;
            m_Clearing = null;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state != PlayManagerState.State.Playing || m_Clearing != null)
                return;

            // Everything goes, every game, the first too: a table swept clean with one arm is funnier.
            m_Clearing = StartCoroutine(Clear(new List<TableItem>(TableItem.items)));
        }

        IEnumerator Clear(List<TableItem> items)
        {
            ShoveOff(items);

            foreach (var item in items)
            {
                if (item != null)
                    item.Restock(m_RiseDepth, m_RiseTime);
            }
            yield return new WaitForSeconds(m_RiseTime);
            m_Clearing = null;
        }

        // A copy of each thing where it is now, thrown to the right. A little different for each, the same every time.
        void ShoveOff(List<TableItem> items)
        {
            m_Leftovers.Clear();
            var right = transform.right;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || !items[i].isActiveAndEnabled)
                    continue;

                var wobble = Mathf.Sin(i * 12.9898f) * 0.5f + 0.5f;
                var push = right * m_SweepSpeed * (0.85f + 0.3f * wobble) + Vector3.up * m_SweepLift * (0.7f + 0.6f * wobble);
                var spin = new Vector3(wobble - 0.5f, 1f - wobble, -0.5f) * 8f;
                m_Leftovers.Add(TableLeftover.Make(items[i], m_FloorHeight, push, spin));
            }

            // What flies off doesn't knock what's coming back.
            foreach (var leftover in m_Leftovers)
            {
                foreach (var item in items)
                {
                    if (item != null)
                        leftover.IgnoreCollisions(item.gameObject);
                }
            }
        }
    }
}
