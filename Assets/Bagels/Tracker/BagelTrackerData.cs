using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTrackerData", menuName = "Bagel/Bagel Tracker Data")]
    public class BagelTrackerData : ScriptableObject
    {
        public BagelTrackerConstants constants;

        public int toppingsCount;
        public int toppingsMaxCount;

        public float input;
        public float speed;
        public float force;
        public float spin;
    }
}
