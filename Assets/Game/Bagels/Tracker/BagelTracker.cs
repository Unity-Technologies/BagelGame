using System;
using UnityEngine;

namespace Bagel
{
    public class BagelTracker : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelController m_BagelController;
        [SerializeField] BagelTrackerConstants m_BagelTrackerConstants;

        PlayManagerState m_State;

        public PlayManagerState state => m_PlayManager.state;

        void Awake()
        {
            m_State = m_PlayManager.state;
            m_State.tracker.constants = m_BagelTrackerConstants;
            m_BagelController.onToasterHit += BagelController_OnToasterHit;
        }

        void OnDestroy()
        {
            m_BagelController.onToasterHit -= BagelController_OnToasterHit;
        }

        void Update()
        {
            HandleControllerPositionTracking();

            if (m_State.isPlaying)
                HandleControllerDataTracking();
        }

        void HandleControllerPositionTracking()
        {
            transform.position = m_BagelController.transform.position;

            var forward = m_BagelController.GetAbsoluteForward();
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            var tiltedUp = m_BagelController.GetNonRotatedRelativeUp();
            transform.rotation = Quaternion.FromToRotation(transform.up, tiltedUp) * transform.rotation;
        }

        void HandleControllerDataTracking()
        {
            var tracker = m_State.tracker;
            tracker.toppingsCount = m_BagelController.currentToppingCount;
            tracker.input = m_BagelController.currentInput;
            tracker.speed = m_BagelController.currentSpeed;
            tracker.force = Mathf.Ceil(m_BagelController.currentForce);
            tracker.spin = Mathf.Ceil(m_BagelController.currentSpin);
        }

        void BagelController_OnToasterHit(object sender, EventArgs e)
        {
            if (!m_State.isPlaying)
                return;

            HandleControllerDataTracking();
            m_State.FinishRun();
        }
    }
}
