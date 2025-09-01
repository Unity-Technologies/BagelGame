using System;
using UnityEngine;

namespace Bagel
{
    public class BagelController : MonoBehaviour
    {
        public BagelType BagelType;

        [HideInInspector]
        public float tiltRecoverySpeed = 10.0f; // Speed at which it recovers its upright tilt

        [SerializeField] PlayerInputBindings m_PlayerInputBindings;
        [SerializeField] LayerMask m_ToastersLayerMask;
        [SerializeField] BagelControllerConstants m_BagelControllerConstants;

        Rigidbody m_RigidBody;
        Collider m_Collider;
        PhysicsMaterial m_PhysicsMaterial;
        Transform m_BagelSlot;

        int m_CurrentToppingCount;
        float m_CurrentSpeed;
        float m_CurrentForce;

        public int CurrentToppingCount => m_CurrentToppingCount;
        public float CurrentSpeed => m_CurrentSpeed;
        public float CurrentForce => m_CurrentForce;

        public BagelControllerConstants BagelControllerConstants => m_BagelControllerConstants;

        public event EventHandler OnToasterHit;

        public Vector3 GetAbsoluteRight()
        {
            var right = transform.right;
            right.y = 0f;
            right.Normalize();
            return right;
        }

        public Vector3 GetAbsoluteForward()
        {
            var right = GetAbsoluteRight();
            var forward = Quaternion.AngleAxis(-90f, Vector3.up) * right;
            return forward;
        }

        public Vector3 GetNonRotatedRelativeUp()
        {
            var right = transform.right;
            var absoluteForward = GetAbsoluteForward();
            var up = Vector3.Cross(absoluteForward, right);
            return up;
        }

        public void Init()
        {
            if (BagelType == null || m_RigidBody == null)
                return;

            CopyInitialData();
            CopyConstantData();

            foreach (Transform child in m_BagelSlot)
                Destroy(child.gameObject);

            Instantiate(BagelType.modelPrefab, m_BagelSlot);
        }

        void CopyInitialData()
        {
            m_CurrentToppingCount = BagelType.maxToppingCount;
        }

        void CopyConstantData()
        {
            m_RigidBody.mass = BagelType.mass;
            m_PhysicsMaterial.dynamicFriction = BagelType.dynamicFriction;
            m_PhysicsMaterial.staticFriction = BagelType.staticFriction;
            m_PhysicsMaterial.bounciness = BagelType.bounciness;
        }

        void OnEnable()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Collider = GetComponent<Collider>();
            m_PhysicsMaterial = m_Collider.material;
            m_BagelSlot = transform.GetChild(0);
            Init();
        }

        void FixedUpdate()
        {
#if UNITY_EDITOR
            CopyConstantData();
#endif

            HandleMovement();
            HandleToppingLoss();
            TiltUpright();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, GetAbsoluteForward() * 5f);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetAbsoluteRight() * 5f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, GetNonRotatedRelativeUp() * 5f);
        }

        void HandleMovement()
        {
            var inputVector = m_PlayerInputBindings.GetMovementVectorNormalized();

            m_RigidBody.AddTorque(transform.right * BagelType.rollTorque * inputVector.y, ForceMode.Force);
            m_RigidBody.AddTorque(GetNonRotatedRelativeUp() * BagelType.turnTorque * inputVector.x, ForceMode.Force);

            m_CurrentSpeed = Vector3.Dot(m_RigidBody.linearVelocity, GetAbsoluteForward());
            m_CurrentForce = inputVector.y;
        }

        void HandleToppingLoss()
        {
            m_CurrentToppingCount = Mathf.Max(0, m_CurrentToppingCount - 2);
        }

        void TiltUpright()
        {
            var currentRight = transform.right;
            var globalUp = Vector3.up;

            float tiltAngle = Vector3.Angle(currentRight, globalUp) - 90f;

            var tiltAxis = GetAbsoluteForward();
            var correctiveTorque = tiltAxis * (tiltAngle * tiltRecoverySpeed);

            m_RigidBody.AddTorque(correctiveTorque, ForceMode.Force);
        }

        void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & m_ToastersLayerMask) != 0)
            {
                OnToasterHit?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
