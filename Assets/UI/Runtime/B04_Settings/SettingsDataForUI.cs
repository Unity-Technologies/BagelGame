using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "SettingsDataForUI", menuName = "Bagel/Settings Data For UI")]
    public class SettingsDataForUI : ScriptableObject
    {
        public float fieldOfViewMin = 50.0f;
        public float fieldOfViewMax = 80.0f;
        [Range(50.0f, 80.0f)] public float fieldOfView = 65.0f;
    }
}
