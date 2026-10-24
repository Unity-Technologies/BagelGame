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
            public string nameKey;
            public ThemeStyleSheet theme;

            // Shown next to its name: in Settings, and in the Editor's theme pickers.
            public VectorImage icon;
        }

        public List<ThemeEntry> collection;
    }
}
