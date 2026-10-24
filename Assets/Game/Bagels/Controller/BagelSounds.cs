using System;
using UnityEngine;

namespace Bagel
{
    // The bagel's own sounds: rolling (a drone that follows its speed and the ground under it), a thud
    // when it hits something hard, a sprinkle when toppings fly off, the toaster, and falling off the edge.
    [RequireComponent(typeof(BagelController))]
    public class BagelSounds : MonoBehaviour
    {
        [SerializeField, Tooltip("Speed (m/s) below which a roll makes no sound, so settling down is quiet.")]
        float m_QuietRollSpeed = 0.3f;
        [SerializeField, Tooltip("Speed (m/s) of the loudest roll.")]
        float m_FullRollSpeed = 12f;
        [SerializeField, Tooltip("Impacts (in g) from this hard make a thud...")]
        float m_ImpactThreshold = 1.5f;
        [SerializeField, Tooltip("... at full volume from this hard.")]
        float m_FullImpact = 8f;
        [SerializeField] float m_ImpactInterval = 0.12f;
        [SerializeField, Tooltip("Toppings lost within about a tenth of a second that make a sprinkle.")]
        float m_SprinkleToppings = 3f;
        [SerializeField] float m_SprinkleInterval = 0.2f;

        BagelController m_Bagel;
        Rigidbody m_Body;
        float m_NextImpactTime;
        float m_NextSprinkleTime;
        int m_LastToppingCount;
        float m_RecentlyLost;
        bool m_Fell;

        void Awake()
        {
            m_Bagel = GetComponent<BagelController>();
            m_Body = GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            m_Bagel.onImpact += Bagel_OnImpact;
            m_Bagel.onToasterHit += Bagel_OnToasterHit;
            m_Bagel.onFellOff += Bagel_OnFellOff;
        }

        void OnDisable()
        {
            m_Bagel.onImpact -= Bagel_OnImpact;
            m_Bagel.onToasterHit -= Bagel_OnToasterHit;
            m_Bagel.onFellOff -= Bagel_OnFellOff;
            BagelAudio.SetDrone(BagelAudio.rollDrone, SynthDrone.silent);
        }

        void Update()
        {
            var playing = m_Bagel.enabled && Time.timeScale > 0f;
            if (!playing)
                m_Fell = false;

            var speed = playing && m_Bagel.isGrounded ? Mathf.InverseLerp(m_QuietRollSpeed, m_FullRollSpeed, m_Body.linearVelocity.magnitude) : 0f;
            BagelAudio.SetDrone(BagelAudio.rollDrone, BagelSoundLibrary.Roll(speed, m_Bagel.groundRoughness));

            // Spinning sheds toppings slowly; a hit throws a handful off at once.
            var toppingCount = m_Bagel.currentToppingCount;
            m_RecentlyLost = m_RecentlyLost * Mathf.Exp(-Time.deltaTime / 0.1f) + Mathf.Max(0, m_LastToppingCount - toppingCount);
            m_LastToppingCount = toppingCount;
            if (playing && m_RecentlyLost >= m_SprinkleToppings && Time.time >= m_NextSprinkleTime)
            {
                BagelAudio.Play("sprinkle", gain: Mathf.Clamp(m_RecentlyLost / (m_SprinkleToppings * 10f), 0.4f, 1f));
                m_NextSprinkleTime = Time.time + m_SprinkleInterval;
                m_RecentlyLost = 0f;
            }
        }

        void Bagel_OnImpact(object sender, float shockGs)
        {
            if (shockGs < m_ImpactThreshold || Time.time < m_NextImpactTime)
                return;

            BagelAudio.Play("impact", gain: Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(m_ImpactThreshold, m_FullImpact, shockGs)));
            m_NextImpactTime = Time.time + m_ImpactInterval;
        }

        void Bagel_OnToasterHit(object sender, EventArgs e) => BagelAudio.Play("toaster");

        void Bagel_OnFellOff(object sender, EventArgs e)
        {
            if (m_Fell)
                return;
            m_Fell = true;
            BagelAudio.Play("fall");
        }
    }
}
