using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // Sets the table again for a new game: whatever the last run pushed out of place (TableItem), and whatever's
    // loose with it, is swept off towards the blackboard's wall, then comes back up out of the table where it
    // belongs, as good as new. What stayed put stays put.
    public class TableClearing : MonoBehaviour
    {
        [Tooltip("Which way things fly off: towards the wall with the blackboard.")]
        [SerializeField] Vector3 m_Away = new(-1f, 0f, 0f);
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

            // What's out of place goes, and so does whatever's loose: sweeping one sugar cube knocks the others.
            var anyOutOfPlace = false;
            foreach (var item in TableItem.items)
                anyOutOfPlace |= item.outOfPlace;
            if (!anyOutOfPlace)
                return;

            var items = new List<TableItem>();
            foreach (var item in TableItem.items)
            {
                if (item.outOfPlace || item.loose)
                    items.Add(item);
            }
            m_Clearing = StartCoroutine(Clear(items));
        }

        IEnumerator Clear(List<TableItem> items)
        {
            var away = m_Away.normalized;
            for (int i = 0; i < items.Count; i++)
            {
                // A little different for each, the same every time.
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
