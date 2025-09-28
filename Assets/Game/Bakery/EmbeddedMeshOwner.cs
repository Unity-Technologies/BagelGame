using System;
using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed class EmbeddedMeshOwner : MonoBehaviour
    {
#if UNITY_EDITOR
        [NonSerialized] public Mesh Mesh;

        void OnDestroy()
        {
            if (!Mesh)
                return;

            DestroyImmediate(Mesh);
            Mesh = null;
        }
#endif
    }
}
