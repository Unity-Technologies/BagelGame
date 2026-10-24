using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // The game over screen is a restaurant's menu that falls open onto the table beside the toaster: the run's
    // receipt on its left page, the leaderboard on its right (a world-space panel on its pages). It tumbles down
    // and lands flat, and whatever on the table it lands on, or next to, is knocked flying. It stays there,
    // until the next game sweeps it off with everything else (a TableItem that doesn't come back).
    [RequireComponent(typeof(TableItem), typeof(Rigidbody))]
    public class GameOverMenu : MonoBehaviour
    {
        [Tooltip("Where the pages are: the game over screen lies on them.")]
        [SerializeField] Transform m_Pages;
        [Tooltip("The open menu's size, flat (X across both pages, Z top to bottom).")]
        [SerializeField] Vector2 m_Size = new(8.5f, 5.9f);

        [Header("Falling")]
        [SerializeField] float m_DropHeight = 7f;
        [SerializeField] Vector3 m_DropFrom = new(0f, 0f, -2.5f);
        [SerializeField] Vector3 m_Tumble = new(-50f, 20f, 15f);
        [SerializeField] float m_FallTime = 0.9f;
        [Tooltip("How far through its fall (0 to 1) it knocks things out of its way.")]
        [SerializeField] float m_KnockAt = 0.55f;
        [SerializeField] float m_BounceHeight = 0.18f;
        [SerializeField] float m_BounceTime = 0.22f;

        [Header("Knocking")]
        [Tooltip("How far past its edges it knocks things over.")]
        [SerializeField] float m_Reach = 1.2f;
        [Tooltip("How fast what's under it shoots out past its nearest edge, at least (meters a second).")]
        [SerializeField] float m_KnockSpeed = 3f;
        [Tooltip("How fast what's close beside it is pushed away.")]
        [SerializeField] float m_NudgeSpeed = 1.6f;
        [SerializeField] float m_KnockLift = 1.2f;

        TableItem m_TableItem;
        Rigidbody m_Body;
        Coroutine m_Fall;

        public Transform pages => m_Pages;
        public bool falling => m_Fall != null;
        public Vector2 size => m_Size;

        void Awake()
        {
            m_TableItem = GetComponent<TableItem>();
            m_Body = GetComponent<Rigidbody>();
        }

        // Falls open from above onto restPosition, flat, its top towards restRotation's forward.
        public void Drop(Vector3 restPosition, Quaternion restRotation)
        {
            gameObject.SetActive(true);
            if (m_Fall != null)
                StopCoroutine(m_Fall);
            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            m_Body.isKinematic = true;
            m_Fall = StartCoroutine(Fall(restPosition, restRotation));
        }

        IEnumerator Fall(Vector3 rest, Quaternion restRotation)
        {
            var from = rest + restRotation * m_DropFrom + Vector3.up * m_DropHeight;
            var tumbled = restRotation * Quaternion.Euler(m_Tumble);
            transform.SetPositionAndRotation(from, tumbled);

            // Falling faster and faster, untwisting as it goes: flat when it lands. Just before it does, the
            // air it pushes and its edges send what's in the way flying, out from under it.
            var knocked = false;
            for (var time = 0f; time < m_FallTime; time += Time.deltaTime)
            {
                var t = time / m_FallTime;
                var fall = t * t;
                transform.SetPositionAndRotation(Vector3.LerpUnclamped(from, rest, fall), Quaternion.Slerp(tumbled, restRotation, Mathf.Sqrt(t)));
                if (!knocked && t > m_KnockAt)
                {
                    KnockAside(rest, restRotation, (1f - t) * m_FallTime);
                    knocked = true;
                }
                yield return null;
            }
            transform.SetPositionAndRotation(rest, restRotation);
            if (!knocked)
                KnockAside(rest, restRotation, 0f);

            // A little bounce.
            for (var time = 0f; time < m_BounceTime; time += Time.deltaTime)
            {
                var t = time / m_BounceTime;
                transform.position = rest + Vector3.up * (m_BounceHeight * 4f * t * (1f - t));
                yield return null;
            }
            transform.position = rest;
            m_Fall = null;
        }

        // Whatever is where it lands shoots out past its nearest edge before it gets there (in timeLeft seconds),
        // so nothing ends up on its pages; what's close beside it is pushed away.
        void KnockAside(Vector3 rest, Quaternion restRotation, float timeLeft)
        {
            var half = new Vector3(m_Size.x * 0.5f, 0f, m_Size.y * 0.5f);
            var hits = Physics.OverlapBox(rest + Vector3.up * 1.2f, new Vector3(half.x + m_Reach, 1.5f, half.z + m_Reach), restRotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            var knocked = new HashSet<TableItem>();
            foreach (var hit in hits)
            {
                var item = hit.GetComponentInParent<TableItem>();
                if (item == null || item == m_TableItem || !knocked.Add(item))
                    continue;

                var local = Quaternion.Inverse(restRotation) * (item.transform.position - rest);
                Vector3 velocity;
                var toEdgeX = half.x - Mathf.Abs(local.x);
                var toEdgeZ = half.z - Mathf.Abs(local.z);
                if (toEdgeX > 0f && toEdgeZ > 0f)
                {
                    // Under it: out to the side (across the table, clear of what's before and behind it), or out
                    // the top or bottom if that's much closer, fast enough to be clear when it lands.
                    var sideways = toEdgeX < toEdgeZ * 2f;
                    var outward = sideways ? new Vector3(Mathf.Sign(local.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(local.z));
                    var distance = (sideways ? toEdgeX : toEdgeZ) + 0.6f;
                    var speed = timeLeft > 0.05f ? Mathf.Max(m_KnockSpeed, distance / timeLeft * 1.3f) : m_KnockSpeed + 2f * distance;
                    velocity = restRotation * outward * speed;
                }
                else
                {
                    var away = Vector3.ProjectOnPlane(item.transform.position - rest, Vector3.up).normalized;
                    velocity = away * m_NudgeSpeed;
                }
                var spin = new Vector3(velocity.z, 0f, -velocity.x) * 2f;
                item.Knock(velocity + Vector3.up * m_KnockLift, spin);
            }
            // Knocked or not, the table isn't as it was: the next game clears it.
            m_TableItem.Loosen();
            m_Body.isKinematic = true;
        }
    }
}
