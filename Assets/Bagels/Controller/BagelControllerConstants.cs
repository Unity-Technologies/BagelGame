using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelControllerConstants", menuName = "Bagel/Bagel Controller Constants")]
    public class BagelControllerConstants : ScriptableObject
    {
        public float speedMin;
        public float speedMax;

        public float forceMin;
        public float forceMax;
    }
}
