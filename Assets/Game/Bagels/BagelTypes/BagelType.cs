using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelType", menuName = "Bagel/Bagel Type")]
    public class BagelType : ScriptableObject
    {
        public string displayName = "New";

        [Space]
        public int maxToppingCount = 1000;
        public float spinToppingLossFactor = 0.005f;
        public float impactToppingLossFactor = 0.1f;
        public float impactAmortizationRate = 0.5f;

        [Space]
        public float tiltRecoverySpeed = 10.0f;
        public float rollTorque = 40.0f;
        public float turnTorque = 1.0f;
        public float mass = 4.0f;

        [Space]
        public float dynamicFriction = 0.6f;
        public float staticFriction = 0.6f;
        [Range(0.0f, 1.0f)] public float bounciness = 0.0f;

        [Space]
        public Transform modelPrefab;

        // Cummulative Stats
        public float toppings => maxToppingCount;
        public float toppingLoss => spinToppingLossFactor + impactToppingLossFactor + impactAmortizationRate;
        public float control => tiltRecoverySpeed + turnTorque;
        public float speed => rollTorque;
        public float grip => dynamicFriction + staticFriction;

        // Computed Stats
        [Space]
        [Range(0.0f, 1.0f)] public float toppingsPercentile;
        [Range(0.0f, 1.0f)] public float toppingLossPercentile;
        [Range(0.0f, 1.0f)] public float controlPercentile;
        [Range(0.0f, 1.0f)] public float speedPercentile;
        [Range(0.0f, 1.0f)] public float gripPercentile;
    }
}
