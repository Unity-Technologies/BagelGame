using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "PanelSettingsCollection", menuName = "Bagel/Panel Settings Collection")]
    public class PanelSettingsCollection : ScriptableObject
    {
        public List<PanelSettings> collection;
    }
}
