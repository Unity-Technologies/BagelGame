using UnityEngine;

namespace Bagel
{
    public class BagelController : MonoBehaviour
    {
        public float rollTorque = 10.0f; // Torque applied for rolling
        public float turnSpeed = 100.0f;
        public float turnTorque = 5.0f;

        public float tiltRecoverySpeed = 10.0f; // Speed at which it recovers its upright tilt

        [SerializeField] PlayerInputBindings m_PlayerInputBindings;
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] LayerMask m_ToastersLayerMask;

        Rigidbody m_RigidBody;

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

        void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            HandleMovement();
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
            m_RigidBody.AddTorque(transform.right * rollTorque * inputVector.y, ForceMode.Force);
            m_RigidBody.AddTorque(GetNonRotatedRelativeUp() * turnTorque * inputVector.x, ForceMode.Force);
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
                m_PlayManager.GoToGameOver();
                return;
            }
        }
    }
}
