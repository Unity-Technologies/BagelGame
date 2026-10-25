using System.Collections;
using UnityEngine;

namespace Bagel
{
    // Clears the table for a new game after a game over: everything on it (TableItem) is swept off towards
    // the blackboard's wall, then what belongs on it comes back up out of the table, as good as new. The menu
    // that fell on it at game over goes with the rest, and stays gone.
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
            if (state == PlayManagerState.State.Playing && TableItem.disturbed && m_Clearing == null)
                m_Clearing = StartCoroutine(Clear());
        }

        IEnumerator Clear()
        {
            var away = m_Away.normalized;
            var items = new TableItem[TableItem.items.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = TableItem.items[i];

            for (int i = 0; i < items.Length; i++)
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
                if (item == null)
                    continue;
                if (item.comesBack)
                    item.Restock(m_RiseDepth, m_RiseTime);
                else
                    item.gameObject.SetActive(false);
            }
            TableItem.MarkSet();
            yield return new WaitForSeconds(m_RiseTime);
            m_Clearing = null;
        }
    }
}
