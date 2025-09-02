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
        public BagelTrackerData BagelTrackerData
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
            m_BagelController.OnToasterHit += BagelController_OnToasterHit;
            m_PlayManager.State.OnSetBagelType += State_OnSetBagelType;
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
            BagelTrackerData.toppingsCount = m_BagelController.CurrentToppingCount;
            BagelTrackerData.input = m_BagelController.CurrentInput;
            BagelTrackerData.speed = m_BagelController.CurrentSpeed;
            BagelTrackerData.force = Mathf.Ceil(m_BagelController.CurrentForce);
            BagelTrackerData.spin = Mathf.Ceil(m_BagelController.CurrentSpin);
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            BagelTrackerData.toppingsMaxCount = bagelType.maxToppingCount;
        }

        void BagelController_OnToasterHit(object sender, EventArgs e)
        {
            OnToasterHit();
        }

        void OnToasterHit()
        {
            m_PlayManager.State.SetBagelTrackerData(m_BagelTrackerData);
            m_PlayManager.State.GoToGameOver();
        }
    }
}
