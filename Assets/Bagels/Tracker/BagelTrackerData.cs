using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTrackerData", menuName = "Bagel/Bagel Tracker Data")]
    public class BagelTrackerData : BagelControllerConstants
    {
        public int toppingsCount;
        public int toppingsMaxCount;

        public float speed;
        public float force;

        public void CopyFrom(BagelControllerConstants src)
        {
            speedMin = src.speedMin;
            speedMax = src.speedMax;
            forceMin = src.forceMin;
            forceMax = src.forceMax;
        }
    }
}
