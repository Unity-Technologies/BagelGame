using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [CreateAssetMenu(fileName = "SettingsDataForUI", menuName = "Bagel/Settings Data For UI")]
    public class SettingsDataForUI : ScriptableObject
    {
        public int themeIndex = 0;
    }
}
