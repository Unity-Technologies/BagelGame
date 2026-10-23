using System;
using UnityEngine;

namespace Bagel
{
    // A box with each face's UVs running 0 to 1, laid out like the built-in cube's.
    [Serializable]
    public class BoxMeshGenerator : ProceduralMeshGenerator
    {
        public Vector3 size = Vector3.one;

        public override void Build(MeshBuilder builder)
        {
            var half = size * 0.5f;
            AddFace(builder, Vector3.forward * half.z, Vector3.left, Vector3.up, new Vector2(size.x, size.y));
            AddFace(builder, Vector3.up * half.y, Vector3.left, Vector3.back, new Vector2(size.x, size.z));
            AddFace(builder, Vector3.back * half.z, Vector3.left, Vector3.down, new Vector2(size.x, size.y));
            AddFace(builder, Vector3.down * half.y, Vector3.left, Vector3.forward, new Vector2(size.x, size.z));
            AddFace(builder, Vector3.left * half.x, Vector3.back, Vector3.up, new Vector2(size.z, size.y));
            AddFace(builder, Vector3.right * half.x, Vector3.forward, Vector3.up, new Vector2(size.z, size.y));
        }

        static void AddFace(MeshBuilder builder, Vector3 center, Vector3 right, Vector3 up, Vector2 faceSize)
        {
            builder.AddPlane(center, right, up, faceSize, Vector2Int.one, Vector2.one, Vector2.zero);
        }
    }
}
