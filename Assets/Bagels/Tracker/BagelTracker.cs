using System;
using UnityEngine;

namespace Bagel
{
    public class BagelTracker : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelController m_BagelController;

        BagelTrackerData m_BagelTrackerData;
        public BagelTrackerData BagelTrackerData
        {
            get
            {
                if (m_BagelTrackerData != null)
                    return m_BagelTrackerData;

                m_BagelTrackerData = ScriptableObject.CreateInstance<BagelTrackerData>();
                m_BagelTrackerData.hideFlags = HideFlags.HideAndDontSave;
                return m_BagelTrackerData;
            }
        }

        void Awake()
        {
            CopyConstantData();

            m_BagelController.OnToasterHit += BagelController_OnToasterHit;
            m_PlayManager.State.OnSetBagelType += State_OnSetBagelType;
        }

        void Update()
        {
#if UNITY_EDITOR
            CopyConstantData();
#endif

            HandleControllerPositionTracking();
            HandleControllerDataTracking();
        }

        void CopyConstantData()
        {
            BagelTrackerData.CopyFrom(m_BagelController.BagelControllerConstants);
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
            BagelTrackerData.speed = m_BagelController.CurrentSpeed;
            BagelTrackerData.force = m_BagelController.CurrentForce;
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
