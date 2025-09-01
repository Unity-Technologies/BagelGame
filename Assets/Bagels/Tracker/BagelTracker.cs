using System;
using UnityEngine;

namespace Bagel
{
    public class BagelTracker : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelController m_BagelController;

        BagelTrackerData m_BagelTrackerData;
        public BagelTrackerData BagelTrackerData => m_BagelTrackerData;

        void Awake()
        {
            m_BagelTrackerData = ScriptableObject.CreateInstance<BagelTrackerData>();
            m_BagelTrackerData.hideFlags = HideFlags.HideAndDontSave;
            CopyConstantData();

            m_BagelController.OnToasterHit += BagelController_OnToasterHit;
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
            m_BagelTrackerData.toppingsMaxCount = m_BagelController.BagelType.maxToppingCount;
            m_BagelTrackerData.CopyFrom(m_BagelController.BagelControllerConstants);
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
            m_BagelTrackerData.toppingsCount = m_BagelController.CurrentToppingCount;
            m_BagelTrackerData.speed = m_BagelController.CurrentSpeed;
            m_BagelTrackerData.force = m_BagelController.CurrentForce;
        }

        void BagelController_OnToasterHit(object sender, EventArgs e)
        {
            OnToasterHit();
        }

        void OnToasterHit()
        {
            m_PlayManager.GoToGameOver(m_BagelTrackerData);
        }
    }
}
