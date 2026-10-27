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
        [SerializeField] float m_FallLimit = 10.0f;
        [Tooltip("How long it takes to reach its turn speed, or to stop turning, in seconds.")]
        [SerializeField] float m_TurnResponse = 0.1f;

        PlayManagerState m_State;
        Rigidbody m_RigidBody;
        Collider m_Collider;
        PhysicsMaterial m_PhysicsMaterial;
        Transform m_BagelSlot;

        float m_CurrentToppingCount;
        float m_CurrentInput;
        float m_CurrentSpeed;
        float m_CurrentForce;
        float m_CurrentSpin;
        float m_LastTouchTime = float.NegativeInfinity;

        // A rolling bagel hops off the ground for a moment now and then: it's still rolling.
        const float k_GroundedGrace = 0.1f;

        // It's backing up with the stick this far back (so a stick pushed only a little low of the side still turns
        // it that way), or with it to the side while it rolls back this fast (meters a second).
        const float k_BackingInput = 0.25f;
        const float k_BackingSpeed = 0.5f;

        public BagelType bagelType => m_BagelType;
        public int currentToppingCount => Mathf.CeilToInt(m_CurrentToppingCount);
        public float currentInput => m_CurrentInput;
        public float currentSpeed => m_CurrentSpeed;
        public float currentForce => m_CurrentForce;
        public float currentSpin => m_CurrentSpin;

        // Whether it's touching something, and how rough that is: 0 for ice, 1 for ground with the bagel's
        // own grip.
        public bool isGrounded => Time.fixedTime - m_LastTouchTime <= k_GroundedGrace;
        public float groundRoughness { get; private set; }

        public event EventHandler onToasterHit;
        public event EventHandler onFellOff;
        public event EventHandler<float> onImpact;

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
            m_State = m_PlayManager.state;
            m_State.onStateChange += State_OnStateChange;
            m_State.onSetBagelType += State_OnSetBagelType;
            if (m_State.selectedBagelType != null)
                m_BagelType = m_State.selectedBagelType;
            enabled = false;
            Hold();
        }

        void OnDestroy()
        {
            m_State.onStateChange -= State_OnStateChange;
            m_State.onSetBagelType -= State_OnSetBagelType;
        }

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state == PlayManagerState.State.Playing)
            {
                m_RigidBody.isKinematic = false;
                transform.position = m_StartingLocation.position;
                transform.rotation = m_StartingLocation.rotation;
                m_RigidBody.position = transform.position;
                m_RigidBody.rotation = transform.rotation;
                m_RigidBody.linearVelocity = Vector3.zero;
                m_RigidBody.angularVelocity = Vector3.zero;
                m_RigidBody.Sleep();
                // A new bagel: no knock from before (the last one's crash, things set round it while it waited).
                m_CurrentForce = 0f;
                m_CurrentSpin = 0f;
                enabled = true;
            }
            else
            {
                enabled = false;
                // Waiting at the start for the next game while its level's chosen, as things come and go round it.
                if (state == PlayManagerState.State.LevelSelection)
                    Hold();
            }
        }

        // At the start, standing on its edge, held still until it rolls.
        void Hold()
        {
            if (!m_RigidBody.isKinematic)
            {
                m_RigidBody.linearVelocity = Vector3.zero;
                m_RigidBody.angularVelocity = Vector3.zero;
            }
            m_RigidBody.isKinematic = true;
            transform.SetPositionAndRotation(m_StartingLocation.position, m_StartingLocation.rotation);
            m_RigidBody.position = transform.position;
            m_RigidBody.rotation = transform.rotation;
        }

        void State_OnSetBagelType(object sender, BagelType bagelType)
        {
            if (bagelType == null)
                return;

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
            HandleFalling();
        }

        // Off the edge of the level: the run is over, toppings and all.
        void HandleFalling()
        {
            if (transform.position.y > m_StartingLocation.position.y - m_FallLimit)
                return;

            m_CurrentToppingCount = 0;
            onFellOff?.Invoke(this, EventArgs.Empty);
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
            var inputVector = m_PlayManager.playInputBindings.GetMovementVectorNormalized();
            var rollTorque = transform.right * m_BagelType.rollTorque * inputVector.y;

            m_RigidBody.AddTorque(rollTorque, ForceMode.Force);
            Steer(inputVector);

            m_CurrentInput = inputVector.y;
            m_CurrentSpeed = Vector3.Dot(m_RigidBody.linearVelocity, GetAbsoluteForward());
            m_CurrentForce = Mathf.Max(0, m_CurrentForce - m_BagelType.impactAmortizationRate);
            m_CurrentSpin = m_RigidBody.angularVelocity.magnitude;
        }

        // With the stick over to a side it turns at its bagel's turn speed, rolling or standing still, and it stops
        // turning once the stick's let go: its turn (about its up, however it leans) is brought to that within the
        // turn response, whatever its grip on the ground. The stick's as far over as it goes that way (the keys'
        // diagonals too, which the input normalizes), so forward and right turns as fast as right alone. Backing up,
        // it steers like a car in reverse: back and right goes back and to the right, so it turns the other way.
        void Steer(Vector2 input)
        {
            var most = Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.y));
            var steer = most > 0f ? input.x / most : 0f;
            var backing = input.y < -k_BackingInput || (input.y <= 0f && m_CurrentSpeed < -k_BackingSpeed);
            if (backing)
                steer = -steer;

            var up = GetNonRotatedRelativeUp();
            var turnSpeed = m_BagelType.turnSpeed * Mathf.Deg2Rad;
            var turn = Vector3.Dot(m_RigidBody.angularVelocity, up);
            var turned = Mathf.MoveTowards(turn, steer * turnSpeed, turnSpeed / m_TurnResponse * Time.fixedDeltaTime);

            // A little more, for what the body's damping takes off it this step.
            var kept = Mathf.Max(0.1f, 1f - m_RigidBody.angularDamping * Time.fixedDeltaTime);
            m_RigidBody.AddTorque(up * (turned / kept - turn), ForceMode.VelocityChange);
        }

        void HandleToppingLoss()
        {
            m_CurrentToppingCount = Mathf.Max(0, m_CurrentToppingCount - ToppingLoss(m_BagelType, m_CurrentForce, m_CurrentSpin));
        }

        // Toppings lost in one physics step, from the last impact (in g) and the current spin.
        public static float ToppingLoss(BagelType bagelType, float force, float spin)
        {
            var impactLoss = force * bagelType.impactToppingLossFactor;
            var spinLoss = spin * bagelType.spinToppingLossFactor;
            return impactLoss + spinLoss;
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
                onToasterHit?.Invoke(this, EventArgs.Empty);
            }
        }

        void OnCollisionEnter(Collision collision) => EvaluateCollision(collision);

        void OnCollisionStay(Collision collision) => EvaluateCollision(collision);

        void EvaluateCollision(Collision collision)
        {
            var contactNormal = collision.GetContact(0).normal;
            var shockGs = ShockGs(collision.impulse, contactNormal, Time.fixedDeltaTime, m_RigidBody.mass, Physics.gravity.magnitude);

            m_CurrentForce = Mathf.Max(m_CurrentForce, shockGs);
            onImpact?.Invoke(this, shockGs);

            var ground = collision.collider.sharedMaterial;
            m_LastTouchTime = Time.fixedTime;
            groundRoughness = ground != null ? Mathf.InverseLerp(0.05f, m_PhysicsMaterial.dynamicFriction, ground.dynamicFriction) : 1f;
        }

        // How hard a contact hit, in g, along its normal and without the 1 g of resting on the ground.
        public static float ShockGs(Vector3 impulse, Vector3 contactNormal, float dt, float mass, float gravity)
        {
            var averageForce = impulse / dt;
            var shockForce = Mathf.Abs(Vector3.Dot(averageForce, contactNormal.normalized));
            var shockGs = shockForce / (mass * gravity);

            return Mathf.Max(shockGs, 1.0f) - 1.0f;
        }
    }
}
