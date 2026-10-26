using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // The game's own levels, the first the tutorial, and the library of things they're set with.
    [CreateAssetMenu(fileName = "BagelLevelCollection", menuName = "Bagel/Bagel Level Collection")]
    public class BagelLevelCollection : ScriptableObject
    {
        public List<BagelLevel> collection = new();
        public BagelLevelLibrary library;
    }
}
