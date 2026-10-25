using UnityEngine;

namespace Bagel
{
    // What a player can't ask the engine at runtime: the full Unity version with its revision (Application.unityVersion
    // is only the version) and when it was built. A build step (BagelBuildInfoStamp) writes them before each player
    // build and clears them again once it's done, so builds don't change the asset. Settings › About shows them.
    [CreateAssetMenu(fileName = "BagelBuildInfo", menuName = "Bagel/Build Info")]
    public class BagelBuildInfo : ScriptableObject
    {
        [Tooltip("The Editor's full version and revision. Written by the build.")]
        [SerializeField] string m_FullUnityVersion;

        [Tooltip("When the player was built, in UTC. Written by the build, empty between builds.")]
        [SerializeField] string m_BuildDate;

        public string fullUnityVersion
        {
            get => m_FullUnityVersion;
            set => m_FullUnityVersion = value;
        }

        public string buildDate
        {
            get => m_BuildDate;
            set => m_BuildDate = value;
        }
    }
}
