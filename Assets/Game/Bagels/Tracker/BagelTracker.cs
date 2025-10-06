using System;
using UnityEngine;

namespace Bagel
{
    public class BagelTracker : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelController m_BagelController;
        [SerializeField] BagelTrackerConstants m_BagelTrackerConstants;

        BagelTrackerData m_BagelTrackerData;
        public BagelTrackerData bagelTrackerData
        {
            get
            {
                if (m_BagelTrackerData != null)
                    return m_BagelTrackerData;

                m_BagelTrackerData = ScriptableObject.CreateInstance<BagelTrackerData>();
                m_BagelTrackerData.hideFlags = HideFlags.HideAndDontSave;
                m_BagelTrackerData.constants = m_BagelTrackerConstants;
                return m_BagelTrackerData;
            }
        }

        void Awake()
        {
            m_BagelController.onToasterHit += BagelController_OnToasterHit;
            m_PlayManager.state.onSetBagelType += State_OnSetBagelType;
        }

        void Update()
        {
            HandleControllerPositionTracking();
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
            bagelTrackerData.toppingsCount = m_BagelController.currentToppingCount;
            bagelTrackerData.input = m_BagelController.currentInput;
            bagelTrackerData.speed = m_BagelController.currentSpeed;
            bagelTrackerData.force = Mathf.Ceil(m_BagelController.currentForce);
            bagelTrackerData.spin = Mathf.Ceil(m_BagelController.currentSpin);
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            bagelTrackerData.toppingsMaxCount = bagelType.maxToppingCount;
        }

        void BagelController_OnToasterHit(object sender, EventArgs e)
        {
            OnToasterHit();
        }

        void OnToasterHit()
        {
            m_PlayManager.state.SetBagelTrackerData(m_BagelTrackerData);
            m_PlayManager.state.GoToGameOver();
        }
    }
}
