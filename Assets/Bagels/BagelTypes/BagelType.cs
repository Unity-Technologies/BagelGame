using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelType", menuName = "Bagel/Bagel Type")]
    public class BagelType : ScriptableObject
    {
        public string displayName = "New";

        [Space]
        public int maxToppingCount = 1000;

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
    }
}
