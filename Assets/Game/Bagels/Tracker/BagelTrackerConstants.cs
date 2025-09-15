using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTrackerConstants", menuName = "Bagel/Bagel Tracker Constants")]
    public class BagelTrackerConstants : ScriptableObject
    {
        public float inputMin;
        public float inputMax;

        public float speedMin;
        public float speedMax;

        public float forceMax;
        public float spinMax;
    }
}
