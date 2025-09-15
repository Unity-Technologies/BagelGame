using System;
using UnityEngine;

namespace Bagel
{
    public class BagelController : MonoBehaviour
    {
        [SerializeField] BagelType m_BagelType;
        [SerializeField] PlayManager m_PlayManager;

        [SerializeField] LayerMask m_ToastersLayerMask;
        [SerializeField] Transform m_StartingLocation;

        Rigidbody m_RigidBody;
        Collider m_Collider;
        PhysicsMaterial m_PhysicsMaterial;
        Transform m_BagelSlot;

        float m_CurrentToppingCount;
        float m_CurrentInput;
        float m_CurrentSpeed;
        float m_CurrentForce;
        float m_CurrentSpin;

        public BagelType BagelType => m_BagelType;
        public int CurrentToppingCount => Mathf.CeilToInt(m_CurrentToppingCount);
        public float CurrentInput => m_CurrentInput;
        public float CurrentSpeed => m_CurrentSpeed;
        public float CurrentForce => m_CurrentForce;
        public float CurrentSpin => m_CurrentSpin;

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
            CopyInitialData();
            CopyConstantData();

            foreach (Transform child in m_BagelSlot)
                Destroy(child.gameObject);

            Instantiate(m_BagelType.modelPrefab, m_BagelSlot);
        }

        void CopyInitialData()
        {
            m_CurrentToppingCount = m_BagelType.maxToppingCount;
        }

        void CopyConstantData()
        {
            m_RigidBody.mass = m_BagelType.mass;
            m_PhysicsMaterial.dynamicFriction = m_BagelType.dynamicFriction;
            m_PhysicsMaterial.staticFriction = m_BagelType.staticFriction;
            m_PhysicsMaterial.bounciness = m_BagelType.bounciness;
        }

        void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Collider = GetComponent<Collider>();
            m_PhysicsMaterial = m_Collider.material;
            m_BagelSlot = transform.GetChild(0);
            m_PlayManager.State.OnStateChange += State_OnStateChange;
            m_PlayManager.State.OnSetBagelType += State_OnSetBagelType;
            enabled = false;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.Playing)
            {
                transform.position = m_StartingLocation.position;
                transform.rotation = m_StartingLocation.rotation;
                m_RigidBody.position = transform.position;
                m_RigidBody.rotation = transform.rotation;
                enabled = true;
            }
            else
            {
                enabled = false;
            }
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            m_BagelType = bagelType;
            Init();
        }

        void OnEnable()
        {
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
            var inputVector = m_PlayManager.PlayInputBindings.GetMovementVectorNormalized();
            var rollTorque = transform.right * m_BagelType.rollTorque * inputVector.y;
            var turnTorque = GetNonRotatedRelativeUp() * m_BagelType.turnTorque * inputVector.x;

            m_RigidBody.AddTorque(rollTorque, ForceMode.Force);
            m_RigidBody.AddTorque(turnTorque, ForceMode.Force);

            m_CurrentInput = inputVector.y;
            m_CurrentSpeed = Vector3.Dot(m_RigidBody.linearVelocity, GetAbsoluteForward());
            m_CurrentForce = Mathf.Max(0, m_CurrentForce - m_BagelType.impactAmortizationRate);
            m_CurrentSpin = m_RigidBody.angularVelocity.magnitude;
        }

        void HandleToppingLoss()
        {
            var impactLoss = m_CurrentForce * m_BagelType.impactToppingLossFactor;
            var spinLoss = m_CurrentSpin * m_BagelType.spinToppingLossFactor;
            var loss = impactLoss + spinLoss;

            m_CurrentToppingCount = Mathf.Max(0, m_CurrentToppingCount - loss);
        }

        void TiltUpright()
        {
            var currentRight = transform.right;
            var globalUp = Vector3.up;

            float tiltAngle = Vector3.Angle(currentRight, globalUp) - 90f;

            var tiltAxis = GetAbsoluteForward();
            var correctiveTorque = tiltAxis * (tiltAngle * m_BagelType.tiltRecoverySpeed);

            m_RigidBody.AddTorque(correctiveTorque, ForceMode.Force);
        }

        void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & m_ToastersLayerMask) != 0)
            {
                OnToasterHit?.Invoke(this, EventArgs.Empty);
            }
        }

        void OnCollisionEnter(Collision collision) => EvaluateCollision(collision);

        void OnCollisionStay(Collision collision) => EvaluateCollision(collision);

        void EvaluateCollision(Collision collision)
        {
            var impulse = collision.impulse;
            var dt = Time.fixedDeltaTime;
            var averageImpulse = impulse / dt;

            var contactNormal = collision.GetContact(0).normal.normalized;
            var shockForce = Mathf.Abs(Vector3.Dot(averageImpulse, contactNormal));
            var scrapeForce = (averageImpulse - Vector3.Project(averageImpulse, contactNormal)).magnitude;

            var g = Physics.gravity.magnitude;
            var shockGs = shockForce / (m_RigidBody.mass * g);
            var scrapeGs = scrapeForce / (m_RigidBody.mass * g);

            // Remove normal gravity.
            shockGs = Mathf.Max(shockGs, 1.0f) - 1.0f;

            m_CurrentForce = Mathf.Max(m_CurrentForce, shockGs);
        }
    }
}
