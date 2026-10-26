using System.Collections;
using UnityEngine;

namespace Bagel
{
    // The cash register by the lazy Susan, with the keys to choose a bagel on its top (BagelSelectionControlsPane). An
    // order rings it up, on the way to the payment tablet: the drawer shoots out, and slides back in.
    public class BagelRegister : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] Transform m_Drawer;
        [Tooltip("How far the drawer comes out, and how long it stays out (seconds).")]
        [SerializeField] float m_DrawerTravel = 0.9f;
        [SerializeField] float m_DrawerOpenTime = 0.8f;

        PlayManagerState m_State;
        PlayManagerState.State m_LastState;
        Vector3 m_DrawerHome;
        Coroutine m_Ring;

        public bool ringing => m_Ring != null;

        void OnEnable()
        {
            if (m_PlayManager == null || m_Drawer == null)
                return;
            m_State = m_PlayManager.state;
            m_LastState = m_State.currentState;
            m_DrawerHome = m_Drawer.localPosition;
            m_State.onStateChange += State_OnStateChange;
        }

        void OnDisable()
        {
            if (m_State != null)
                m_State.onStateChange -= State_OnStateChange;
            if (m_Drawer != null)
                m_Drawer.localPosition = m_DrawerHome;
            m_Ring = null;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            var ordered = m_LastState == PlayManagerState.State.BagelSelection &&
                (state == PlayManagerState.State.LevelSelection || state == PlayManagerState.State.Playing);
            m_LastState = state;
            if (!ordered)
                return;
            if (m_Ring != null)
                StopCoroutine(m_Ring);
            m_Ring = StartCoroutine(Ring());
        }

        IEnumerator Ring()
        {
            // Out with a bang, a little past and back, then a while later slowly in.
            var open = m_DrawerHome + Vector3.forward * m_DrawerTravel;
            yield return Slide(m_DrawerHome, open, 0.12f, true);
            yield return new WaitForSeconds(m_DrawerOpenTime);
            yield return Slide(open, m_DrawerHome, 0.45f, false);
            m_Ring = null;
        }

        IEnumerator Slide(Vector3 from, Vector3 to, float duration, bool overshoot)
        {
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                var t = time / duration;
                var eased = overshoot ? 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f) : t * t * (3f - 2f * t);
                m_Drawer.localPosition = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }
            m_Drawer.localPosition = to;
        }
    }
}
