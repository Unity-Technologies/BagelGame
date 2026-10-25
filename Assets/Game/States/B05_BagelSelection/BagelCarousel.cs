using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // The bagels to choose from, on a lazy Susan at the end of the counter: for each kind, a plate with its bagel and,
    // behind it, its card with the stats (BagelCarouselStation). However many kinds there are, they go round it, and
    // the chosen one is at the front, facing the camera. Choosing another turns the lazy Susan to it, the short way
    // round: the cards sway and the bagels slide as it goes, and the chosen bagel hops.
    //
    // It's all there in the bakery while the game is played: roll back up the counter into it, and it spins and things
    // fly. The next time a bagel is chosen, it's set again.
    [ExecuteAlways]
    public class BagelCarousel : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelCarouselStation m_StationPrefab;
        [Tooltip("The lazy Susan's top (a kinematic body, turned from here): it turns, and the plates with it. Its origin is on its surface.")]
        [SerializeField] Rigidbody m_Turntable;
        [Tooltip("The top's board, 1 across: sized to fit the plates.")]
        [SerializeField] Transform m_Board;

        [Header("Layout")]
        [Tooltip("How far from the middle the cards stand, at the least: with more kinds, the cards are further out, so they don't touch.")]
        [SerializeField] float m_CardRadius = 1.5f;
        [Tooltip("How much room a card takes along the circle.")]
        [SerializeField] float m_CardRoom = 2.9f;
        [Tooltip("How far in front of its card a bagel's plate is, and how much room there is round a plate.")]
        [SerializeField] float m_PlateOffset = 1.2f;
        [SerializeField] float m_PlateRoom = 1f;

        [Header("Turning")]
        [Tooltip("How quickly it turns to a bagel (a spring: oscillations a second), and how much it overshoots (damping: 1 doesn't).")]
        [SerializeField] float m_TurnFrequency = 0.7f;
        [SerializeField] float m_TurnDamping = 0.6f;
        [Tooltip("How fast it turns at the most (degrees a second), and how quickly it gets going and stops (degrees a second, a second): a lazy Susan, not a centrifuge.")]
        [SerializeField] float m_MaxSpin = 160f;
        [SerializeField] float m_MaxSpinUp = 420f;
        [Tooltip("How hard it is to spin by knocking into it (kilogram square meters): a bagel rolling into it spins it.")]
        [SerializeField] float m_Inertia = 40f;
        [Tooltip("How fast the chosen bagel hops (meters a second), once it's at the front (within this many degrees).")]
        [SerializeField] float m_HopSpeed = 2.4f;
        [SerializeField] float m_HopWithin = 6f;

        readonly List<BagelCarouselStation> m_Stations = new();
        PlayManagerState m_State;
        float m_Angle;
        float m_TargetAngle;
        float m_Spin;
        int m_Index = -1;
        Coroutine m_Hop;

        public IReadOnlyList<BagelCarouselStation> stations => m_Stations;
        public Rigidbody turntable => m_Turntable;

        // Degrees between two kinds, round the lazy Susan.
        public float step => m_Stations.Count > 0 ? 360f / m_Stations.Count : 0f;

        // How far the lazy Susan has turned (degrees, counting whole turns), where it's turning to, and how fast (degrees
        // a second).
        public float angle => m_Angle;
        public float targetAngle => m_TargetAngle;
        public float spin => m_Spin;

        public BagelCarouselStation selectedStation => m_Index >= 0 && m_Index < m_Stations.Count ? m_Stations[m_Index] : null;

        void OnEnable()
        {
            m_State = m_PlayManager != null ? m_PlayManager.state : null;
            Build();
            if (Application.isPlaying && m_Turntable != null && m_Turntable.TryGetComponent<BagelCarouselKnocks>(out var knocks))
                knocks.onKnock += Knock;
            if (m_State == null)
                return;
            m_State.onSetBagelType += State_OnSetBagelType;
            m_State.onStateChange += State_OnStateChange;
        }

        void OnDisable()
        {
            if (m_Turntable != null && m_Turntable.TryGetComponent<BagelCarouselKnocks>(out var knocks))
                knocks.onKnock -= Knock;
            if (m_State != null)
            {
                m_State.onSetBagelType -= State_OnSetBagelType;
                m_State.onStateChange -= State_OnStateChange;
            }
            Clear();
        }

        void Clear()
        {
            foreach (var station in m_Stations)
            {
                if (station == null)
                    continue;
                if (station.plate != null)
                    DestroyImmediate(station.plate.gameObject);
                DestroyImmediate(station.gameObject);
            }
            m_Stations.Clear();
            m_Index = -1;
        }

        // A station for each kind, round the top, the chosen one at the front. In the Editor, out of play, they're only
        // shown, never saved with the scene, and the top isn't turned.
        void Build()
        {
            Clear();
            var types = m_State?.bagelTypes?.collection;
            if (types == null || types.Count == 0 || m_StationPrefab == null || m_Turntable == null)
                return;

            var count = types.Count;
            m_Index = Mathf.Clamp(m_State.selectedBagelIndex, 0, count - 1);
            m_Angle = m_TargetAngle = m_Index * 360f / count;
            var cardRadius = Mathf.Max(m_CardRadius, count * m_CardRoom / (2f * Mathf.PI));
            var flags = Application.isPlaying ? HideFlags.None : HideFlags.DontSave;
            var top = m_Turntable.transform;
            if (m_Board != null)
            {
                var size = cardRadius + m_PlateOffset + m_PlateRoom;
                var scale = new Vector3(size, 1f, size);
                if (m_Board.localScale != scale)
                    m_Board.localScale = scale;
            }
            var rest = top.parent != null ? top.parent.rotation : Quaternion.identity;
            for (int i = 0; i < count; i++)
            {
                // Kind i is i steps round, the other way the lazy Susan turns to bring it to the front.
                var rotation = rest * Quaternion.Euler(0f, m_Angle - i * 360f / count, 0f);
                var station = Instantiate(m_StationPrefab, transform);
                station.name = types[i] != null ? types[i].name : "Station";
                station.transform.SetPositionAndRotation(top.position + rotation * (Vector3.forward * cardRadius), rotation);
                station.plate.localPosition += Vector3.forward * m_PlateOffset;
                station.bagel.transform.localPosition += Vector3.forward * m_PlateOffset;
                SetHideFlags(station.gameObject, flags);
                station.SetBagelType(types[i]);
                station.SetSelected(i == m_Index);
                m_Stations.Add(station);
            }

            if (!Application.isPlaying)
                return;
            top.localRotation = Quaternion.Euler(0f, m_Angle, 0f);
            m_Spin = 0f;
            foreach (var station in m_Stations)
                station.Attach(this);
            StartCoroutine(ReleaseStations());
        }

        IEnumerator ReleaseStations()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            foreach (var station in m_Stations)
                station.Release();
        }

        static void SetHideFlags(GameObject gameObject, HideFlags flags)
        {
            gameObject.hideFlags = flags;
            foreach (Transform child in gameObject.transform)
                SetHideFlags(child.gameObject, flags);
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            var count = m_Stations.Count;
            if (count == 0 || m_State == null)
                return;
            var index = Mathf.Clamp(m_State.selectedBagelIndex, 0, count - 1);
            if (index == m_Index)
                return;

            // The short way round (forward, if it's half way).
            var steps = ((index - m_Index) % count + count) % count;
            if (steps > count / 2)
                steps -= count;
            m_TargetAngle += steps * step;
            m_Stations[m_Index].SetSelected(false);
            m_Index = index;
            m_Stations[m_Index].SetSelected(true);

            if (Application.isPlaying)
            {
                if (m_Hop != null)
                    StopCoroutine(m_Hop);
                m_Hop = StartCoroutine(HopLater(m_Stations[m_Index]));
            }
            else
            {
                Build();
            }
        }

        // Once it's come round to the front.
        IEnumerator HopLater(BagelCarouselStation station)
        {
            yield return new WaitForFixedUpdate();
            while (Mathf.Abs(m_TargetAngle - m_Angle) > m_HopWithin)
                yield return new WaitForFixedUpdate();
            station.Hop(m_HopSpeed);
            m_Hop = null;
        }

        // Choosing again: the table is set again, and the chosen bagel says hello.
        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state != PlayManagerState.State.BagelSelection || !Application.isPlaying)
                return;
            foreach (var station in m_Stations)
                station.Restock();
            if (selectedStation != null)
            {
                if (m_Hop != null)
                    StopCoroutine(m_Hop);
                m_Hop = StartCoroutine(HopLater(selectedStation));
            }
        }

        // The top is a spring to where it's turning: it turns there, a little past, and back. It's kinematic, turned
        // from here (what stands on it is tied to it, and a dynamic top on an axle would sag under them), so it's
        // spun by hand when something knocks into it (Knock).
        void FixedUpdate()
        {
            if (!Application.isPlaying || m_Turntable == null || m_Stations.Count == 0)
                return;

            var omega = 2f * Mathf.PI * m_TurnFrequency;
            var accel = (m_TargetAngle - m_Angle) * omega * omega - m_Spin * 2f * m_TurnDamping * omega;
            accel = Mathf.Clamp(accel, -m_MaxSpinUp, m_MaxSpinUp);
            m_Spin = Mathf.Clamp(m_Spin + accel * Time.fixedDeltaTime, -m_MaxSpin, m_MaxSpin);
            m_Angle += m_Spin * Time.fixedDeltaTime;
            var rest = m_Turntable.transform.parent != null ? m_Turntable.transform.parent.rotation : Quaternion.identity;
            m_Turntable.MoveRotation(rest * Quaternion.Euler(0f, m_Angle, 0f));
            var turn = m_Turntable.transform.up * (m_Spin * Mathf.Deg2Rad);
            foreach (var station in m_Stations)
                station.Turn(turn);
        }

        // Something hit the top, or something on it (BagelCarouselKnocks): it turns the way it was pushed, as if the
        // top were free to.
        public void Knock(Collision collision)
        {
            if (m_Turntable == null || collision.rigidbody == null || IsOnTop(collision.rigidbody))
                return;
            // The top gets the opposite of what it gave, at the contact, about its axle.
            var torque = 0f;
            var count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                var contact = collision.GetContact(i);
                var arm = contact.point - m_Turntable.position;
                torque += Vector3.Cross(arm, -contact.impulse).y;
            }
            m_Spin += torque / m_Inertia * Mathf.Rad2Deg;
        }

        public bool IsOnTop(Rigidbody body)
        {
            foreach (var station in m_Stations)
            {
                if (station.bagel == body || station.card == body)
                    return true;
            }
            return false;
        }
    }
}
