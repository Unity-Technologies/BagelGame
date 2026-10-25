using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // Sets the table again for a new game: everything on it (TableItem), moved or not, is shoved off to either
    // side, away from the bagel's lane, then comes back up out of the table where it belongs, as good as new.
    public class TableClearing : MonoBehaviour
    {
        [SerializeField] float m_SweepSpeed = 14f;
        [SerializeField] float m_SweepLift = 4f;
        [Tooltip("How long things fly before the table is set again.")]
        [SerializeField] float m_FlyTime = 1.6f;
        [Tooltip("How far below the table things come up from, and how long they take.")]
        [SerializeField] float m_RiseDepth = 1.4f;
        [SerializeField] float m_RiseTime = 1f;

        PlayManager m_PlayManager;
        Coroutine m_Clearing;

        public bool clearing => m_Clearing != null;

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

            // Everything goes, every game, the first too: a table swept clean with both arms is funnier.
            m_Clearing = StartCoroutine(Clear(new List<TableItem>(TableItem.items)));
        }

        IEnumerator Clear(List<TableItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                // Off the nearer side, so nothing flies across the bagel's lane. A little different for each, the
                // same every time.
                var side = transform.InverseTransformPoint(items[i].transform.position).x < 0f ? -1f : 1f;
                var away = transform.TransformDirection(new Vector3(side, 0f, 0f));
                var wobble = Mathf.Sin(i * 12.9898f) * 0.5f + 0.5f;
                var push = away * m_SweepSpeed * (0.85f + 0.3f * wobble) + Vector3.up * m_SweepLift * (0.7f + 0.6f * wobble);
                var spin = new Vector3(wobble - 0.5f, 1f - wobble, 0.5f) * 8f;
                if (items[i].isActiveAndEnabled)
                    items[i].Sweep(push, spin);
            }

            yield return new WaitForSeconds(m_FlyTime);

            foreach (var item in items)
            {
                if (item != null)
                    item.Restock(m_RiseDepth, m_RiseTime);
            }
            TableItem.MarkSet();
            yield return new WaitForSeconds(m_RiseTime);
            m_Clearing = null;
        }
    }
}
