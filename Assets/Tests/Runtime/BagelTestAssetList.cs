using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelTestAssetList", menuName = "Bagel/Bagel Test Asset List")]
    public class BagelTestAssetList : ScriptableObject
    {
        public VisualTreeAsset mainMenuUxml;
        public VisualTreeAsset pauseScreenUxml;
    }
}
