using System;
using UnityEngine;

namespace Bagel
{
    // One kind of bagel on the lazy Susan (BagelCarousel): its plate, the bagel on it, and its card behind it, with the
    // stats. The plate turns with the lazy Susan. The bagel is held to its plate by a spring, sideways only: it slides
    // about as the lazy Susan turns, and can hop. The card's foot is pinned to the top (a joint), and a spring keeps it
    // standing: it sways. Knocked hard enough (by a bagel rolling into it), either one comes loose and flies, until the
    // table is set again (Restock).
    public class BagelCarouselStation : MonoBehaviour
    {
        [SerializeField] Transform m_Plate;
        [SerializeField] Rigidbody m_Bagel;
        [Tooltip("Where the bagel's model goes, in the bagel's body.")]
        [SerializeField] Transform m_BagelSlot;
        [SerializeField] Rigidbody m_Card;

        [Header("Bagel")]
        [Tooltip("How strongly the bagel is held to its spot on the plate (a spring, oscillations a second), and how much it's damped (1 doesn't bounce).")]
        [SerializeField] float m_BagelHoldFrequency = 2.5f;
        [SerializeField] float m_BagelHoldDamping = 0.45f;
        [Tooltip("Pushed further than this from its spot, the bagel comes loose.")]
        [SerializeField] float m_BagelLooseDistance = 1.2f;
        [Tooltip("What it takes to knock the bagel or the card loose (newton seconds, from something that isn't on the lazy Susan).")]
        [SerializeField] float m_KnockImpulse = 3f;

        [Header("Card")]
        [Tooltip("How strongly the card stands up (a spring, oscillations a second), how much it's damped (1 doesn't sway back), and how far it can lean (degrees).")]
        [SerializeField] float m_CardStandFrequency = 2.6f;
        [SerializeField] float m_CardStandDamping = 0.6f;
        [SerializeField] float m_CardSwayLimit = 20f;
        [Tooltip("What it takes to knock the card off its foot.")]
        [SerializeField] float m_CardBreakForce = 250f;

        BagelType m_BagelType;
        BagelCarousel m_Carousel;
        Rigidbody m_Turntable;
        Pose m_BagelHome;
        Pose m_CardHome;
        Vector3 m_BagelScale;
        Vector3 m_CardScale;
        bool m_BagelLoose;
        bool m_Selected;
        ConfigurableJoint m_CardJoint;
        Vector3 m_Spin;

        public BagelType bagelType => m_BagelType;
        public Transform plate => m_Plate;
        public Rigidbody bagel => m_Bagel;
        public Rigidbody card => m_Card;
        public bool selected => m_Selected;

        // Whether the bagel or the card has been knocked loose.
        public bool knocked => m_BagelLoose || m_Card != null && m_CardJoint == null && m_Turntable != null;

        public event EventHandler<BagelType> onBagelTypeChange;
        public event Action<bool> onSelectedChange;

        public void SetBagelType(BagelType bagelType)
        {
            foreach (Transform child in m_BagelSlot)
                DestroyImmediate(child.gameObject);

            m_BagelType = bagelType;
            if (bagelType != null && bagelType.modelPrefab != null)
            {
                var model = Instantiate(bagelType.modelPrefab, m_BagelSlot);
                model.gameObject.hideFlags = gameObject.hideFlags;
            }
            onBagelTypeChange?.Invoke(this, bagelType);
        }

        public void SetSelected(bool selected)
        {
            if (m_Selected == selected)
                return;
            m_Selected = selected;
            onSelectedChange?.Invoke(selected);
        }

        // In play: the plate goes onto the lazy Susan's top, and the bagel and the card, already where they belong on
        // it, will be tied to it (Release). Where they are now is home.
        public void Attach(BagelCarousel carousel)
        {
            m_Carousel = carousel;
            m_Turntable = carousel.turntable;
            var turntable = m_Turntable;
            m_Plate.SetParent(turntable.transform, true);
            Listen(m_Bagel, OnBagelKnocked);
            Listen(m_Card, OnCardKnocked);
            m_BagelHome = Local(m_Bagel.transform);
            m_CardHome = Local(m_Card.transform);
            m_BagelScale = m_Bagel.transform.localScale;
            m_CardScale = m_Card.transform.localScale;
        }

        // Free to move, tied on: once the physics has the lazy Susan where it's been put (the first step after a scene
        // loads would throw everything off it).
        public void Release()
        {
            if (m_Turntable == null || !m_Card.isKinematic)
                return;
            m_Bagel.isKinematic = false;
            m_Card.isKinematic = false;
            PutBack(m_Bagel, m_BagelHome, m_BagelScale, false);
            PutBack(m_Card, m_CardHome, m_CardScale, false);
            TieTheCard();
        }

        Pose Local(Transform t)
        {
            var turntable = m_Turntable.transform;
            return new Pose(turntable.InverseTransformPoint(t.position), Quaternion.Inverse(turntable.rotation) * t.rotation);
        }

        Pose World(Pose local)
        {
            var turntable = m_Turntable.transform;
            return new Pose(turntable.TransformPoint(local.position), turntable.rotation * local.rotation);
        }

        void TieTheCard()
        {
            m_CardJoint = m_Card.gameObject.AddComponent<ConfigurableJoint>();
            m_CardJoint.connectedBody = m_Turntable;
            m_CardJoint.anchor = Vector3.zero; // the card's foot
            // Where its foot is on the top, from where it's put (the physics may not have caught up yet).
            m_CardJoint.autoConfigureConnectedAnchor = false;
            m_CardJoint.connectedAnchor = m_Turntable.transform.InverseTransformPoint(m_Card.transform.position);
            m_CardJoint.xMotion = m_CardJoint.yMotion = m_CardJoint.zMotion = ConfigurableJointMotion.Locked;
            m_CardJoint.angularXMotion = ConfigurableJointMotion.Limited;
            m_CardJoint.angularYMotion = ConfigurableJointMotion.Locked;
            m_CardJoint.angularZMotion = ConfigurableJointMotion.Limited;
            m_CardJoint.lowAngularXLimit = new SoftJointLimit { limit = -m_CardSwayLimit };
            m_CardJoint.highAngularXLimit = new SoftJointLimit { limit = m_CardSwayLimit };
            m_CardJoint.angularZLimit = new SoftJointLimit { limit = m_CardSwayLimit };
            m_CardJoint.breakForce = m_CardBreakForce;
            m_CardJoint.breakTorque = m_CardBreakForce;
        }

        static void Listen(Rigidbody body, System.Action<Collision> onKnock)
        {
            if (!body.TryGetComponent<BagelCarouselKnocks>(out var knocks))
                knocks = body.gameObject.AddComponent<BagelCarouselKnocks>();
            knocks.onKnock += onKnock;
        }

        // Knocked by something from outside (a bagel rolling into it): the lazy Susan feels it, and hard enough, it
        // comes loose.
        bool Knocked(Collision collision)
        {
            if (collision.rigidbody == null || m_Carousel.IsOnTop(collision.rigidbody))
                return false;
            m_Carousel.Knock(collision);
            return collision.impulse.magnitude >= m_KnockImpulse;
        }

        void OnBagelKnocked(Collision collision)
        {
            if (Knocked(collision))
                m_BagelLoose = true;
        }

        void OnCardKnocked(Collision collision)
        {
            if (Knocked(collision) && m_CardJoint != null)
                Destroy(m_CardJoint);
        }

        // Up, a little, with a spin: this one!
        public void Hop(float speed)
        {
            if (m_Bagel.isKinematic || m_BagelLoose)
                return;
            m_Bagel.AddForce(Vector3.up * speed, ForceMode.VelocityChange);
            m_Bagel.AddTorque(Vector3.up * speed * 2f, ForceMode.VelocityChange);
        }

        // Everything back where it belongs, tied on again. What was knocked off comes back from nothing.
        public void Restock()
        {
            if (m_Turntable == null || m_Card.isKinematic)
                return;

            if (m_BagelLoose || (m_Bagel.position - World(m_BagelHome).position).sqrMagnitude > 0.01f)
                PutBack(m_Bagel, m_BagelHome, m_BagelScale, m_BagelLoose);
            m_BagelLoose = false;

            if (m_CardJoint == null)
            {
                PutBack(m_Card, m_CardHome, m_CardScale, true);
                TieTheCard();
            }
        }

        void PutBack(Rigidbody body, Pose home, Vector3 scale, bool popIn)
        {
            var pose = World(home);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = pose.position;
            body.rotation = pose.rotation;
            body.transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (popIn)
                StartCoroutine(PopIn(body.transform, scale));
        }

        static System.Collections.IEnumerator PopIn(Transform t, Vector3 scale)
        {
            const float duration = 0.3f;
            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                var x = time / duration;
                // A little past full size, and back.
                t.localScale = scale * (1f + 0.15f * Mathf.Sin(x * Mathf.PI)) * x;
                yield return null;
            }
            t.localScale = scale;
        }

        // How the lazy Susan's top turns (it's kinematic: the carousel says).
        public void Turn(Vector3 angularVelocity) => m_Spin = angularVelocity;

        void FixedUpdate()
        {
            if (m_Turntable == null || m_Bagel.isKinematic)
                return;
            HoldTheBagel();
            StandTheCard();
        }

        // The spring that holds the bagel to its spot, sideways: it moves with the plate under it.
        void HoldTheBagel()
        {
            if (m_BagelLoose)
                return;
            var home = World(m_BagelHome).position;
            var offset = home - m_Bagel.position;
            offset.y = 0f;
            if (offset.magnitude > m_BagelLooseDistance)
            {
                m_BagelLoose = true;
                return;
            }
            var plateVelocity = Vector3.Cross(m_Spin, home - m_Turntable.position);
            var slip = m_Bagel.linearVelocity - plateVelocity;
            slip.y = 0f;
            var omega = 2f * Mathf.PI * m_BagelHoldFrequency;
            m_Bagel.AddForce(offset * (omega * omega) - slip * (2f * m_BagelHoldDamping * omega), ForceMode.Acceleration);
        }

        // The spring that stands the card up on its foot, the way it stands on the top.
        void StandTheCard()
        {
            if (m_CardJoint == null)
                return;
            var lean = m_Turntable.rotation * m_CardHome.rotation * Quaternion.Inverse(m_Card.rotation);
            lean.ToAngleAxis(out var angle, out var axis);
            if (angle > 180f)
                angle -= 360f;
            if (float.IsNaN(axis.x) || Mathf.Abs(angle) < 0.01f)
                axis = Vector3.zero;
            var omega = 2f * Mathf.PI * m_CardStandFrequency;
            var swing = m_Card.angularVelocity - m_Spin;
            m_Card.AddTorque(axis * (angle * Mathf.Deg2Rad * omega * omega) - swing * (2f * m_CardStandDamping * omega), ForceMode.Acceleration);
        }
    }
}
