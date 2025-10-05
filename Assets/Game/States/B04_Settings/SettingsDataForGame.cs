using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "SettingsDataForGame", menuName = "Bagel/Settings Data For Game")]
    public class SettingsDataForGame : ScriptableObject
    {
        public float fieldOfViewMin = 50.0f;
        public float fieldOfViewMax = 80.0f;
        [Range(50.0f, 80.0f)] public float fieldOfView = 60.0f;
    }
}
