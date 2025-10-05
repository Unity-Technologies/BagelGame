using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "BagelThemeCollection", menuName = "Bagel/Bagel Theme Collection")]
    public class BagelThemeCollection : ScriptableObject
    {
        [Serializable]
        public struct ThemeEntry
        {
            public string themeName;
            public ThemeStyleSheet theme;
        }

        public List<ThemeEntry> collection;
    }
}
