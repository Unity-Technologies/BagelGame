using UnityEngine;
using System.Collections.Generic;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTypeCollection", menuName = "Bagel/Bagel Type Collection")]
    public class BagelTypeCollection : ScriptableObject
    {
        public List<BagelType> collection;
    }
}
