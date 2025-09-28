using UnityEngine;
using UnityEditor;

namespace Bagel
{
#if UNITY_EDITOR
    public enum ProcPlane { XY, XZ, YZ }

    public static class ProceduralMesh
    {
        public static void Apply(GameObject go, Mesh mesh, Material mat = null, bool castShadows = true, bool receiveShadows = true)
        {
            if (!go)
                return;

            var owner = go.GetComponent<EmbeddedMeshOwner>();
            if (!owner)
                owner = go.AddComponent<EmbeddedMeshOwner>();

            if (owner.Mesh && owner.Mesh != mesh)
                Object.DestroyImmediate(owner.Mesh);
            owner.Mesh = mesh;

            var mf = go.GetComponent<MeshFilter>();
            if (!mf)
                mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.GetComponent<MeshRenderer>();
            if (!mr)
                mr = go.AddComponent<MeshRenderer>();
            if (mat)
                mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = receiveShadows;

            EditorUtility.SetDirty(go);
        }

        public static Mesh BuildQuadGrid(
            Vector2 sizeMeters,
            Vector2Int segments,
            float tileMeters = 1f,
            Vector2 uvOffset = default,
            ProcPlane plane = ProcPlane.XZ,
            bool twoSided = false,
            string name = "Embedded_QuadGrid")
        {
            int sx = Mathf.Max(1, segments.x);
            int sy = Mathf.Max(1, segments.y);
            if (tileMeters <= 0f)
                tileMeters = 1f;

            // Right-handed basis: N = R × U
            Vector3 R, U;
            switch (plane)
            {
                case ProcPlane.XY: R = Vector3.right;  U = Vector3.up;    break; // N = +Z
                case ProcPlane.XZ: R = Vector3.right;  U = Vector3.back;  break; // N = +Y
                default:           R = Vector3.forward;U = Vector3.down;  break; // N = +X (YZ)
            }
            var N = Vector3.Normalize(Vector3.Cross(R, U));

            float hx = sizeMeters.x * 0.5f;
            float hy = sizeMeters.y * 0.5f;
            int nx = sx + 1, ny = sy + 1;
            int vFront = nx * ny;
            int vCount = twoSided ? vFront * 2 : vFront;

            var verts = new Vector3[vCount];
            var norms = new Vector3[vCount];
            var tangs = new Vector4[vCount];
            var uvs = new Vector2[vCount];

            // Front face vertices
            int vi = 0;
            for (int y = 0; y < ny; y++)
            {
                float yCoord = Mathf.Lerp(-hy, hy, (float)y / sy); // meters along U
                for (int x = 0; x < nx; x++)
                {
                    float xCoord = Mathf.Lerp(-hx, hx, (float)x / sx); // meters along R
                    verts[vi] = (R * xCoord) + (U * yCoord);
                    norms[vi] = N;
                    tangs[vi] = new Vector4(R.x, R.y, R.z, 1f);
                    uvs[vi] = new Vector2(
                        xCoord / tileMeters + uvOffset.x, yCoord / tileMeters + uvOffset.y);
                    vi++;
                }
            }

            // Optional back face (duplicated verts so culling works without special shaders)
            if (twoSided)
            {
                for (int i = 0; i < vFront; i++)
                {
                    int j = vFront + i;
                    verts[j] = verts[i];
                    norms[j] = -norms[i];
                    var t = new Vector3(-tangs[i].x, -tangs[i].y, -tangs[i].z);
                    tangs[j] = new Vector4(t.x, t.y, t.z, 1f);
                    uvs[j] = uvs[i];
                }
            }

            // Triangles
            int frontTris = sx * sy * 6;
            int triCount = twoSided ? frontTris * 2 : frontTris;
            var tris = new int[triCount];

            int ti = 0;
            for (int y = 0; y < sy; y++)
            {
                for (int x = 0; x < sx; x++)
                {
                    int i0 = (y * nx) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + nx + 1;
                    int i3 = i0 + nx;

                    tris[ti++] = i0; tris[ti++] = i1; tris[ti++] = i2;
                    tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i3;
                }
            }

            if (twoSided)
            {
                int o = vFront;
                for (int y = 0; y < sy; y++)
                {
                    for (int x = 0; x < sx; x++)
                    {
                        int i0 = o + (y * nx) + x;
                        int i1 = i0 + 1;
                        int i2 = i0 + nx + 1;
                        int i3 = i0 + nx;

                        // reverse winding
                        tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i1;
                        tris[ti++] = i0; tris[ti++] = i3; tris[ti++] = i2;
                    }
                }
            }

            var mesh = new Mesh { name = name, hideFlags = HideFlags.None };
            mesh.indexFormat = (vCount > 65535)
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTangents(tangs);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();

            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        public static Mesh BuildBox(
            Vector3 sizeMeters, float tileMeters = 1f, Vector2 uvOffset = default,
            string name = "Embedded_Box")
        {
            if (tileMeters <= 0f)
                tileMeters = 1f;

            float hx = sizeMeters.x * 0.5f;
            float hy = sizeMeters.y * 0.5f;
            float hz = sizeMeters.z * 0.5f;

            // 24 vertices (4 per face), normals per face, tangents per face.
            var verts = new Vector3[24];
            var norms = new Vector3[24];
            var tans = new Vector4[24];
            var uvs = new Vector2[24];
            int vi = 0;

            // Helper local function to add a face given its corners and basis
            void AddFace(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, Vector3 normal, Vector3 tangentR, Vector3 tangentU)
            {
                verts[vi + 0] = bl;
                verts[vi + 1] = br;
                verts[vi + 2] = tr;
                verts[vi + 3] = tl;

                norms[vi + 0] = normal;
                norms[vi + 1] = normal;
                norms[vi + 2] = normal;
                norms[vi + 3] = normal;

                var t = new Vector4(tangentR.x, tangentR.y, tangentR.z, 1f);
                tans[vi + 0] = t;
                tans[vi + 1] = t;
                tans[vi + 2] = t;
                tans[vi + 3] = t;

                // Project to (R,U) to compute UVs in meters / tileMeters
                // bl is (-R,-U), br (+R,-U), tr (+R,+U), tl (-R,+U) in face plane
                // Compute scalar extents along R and U:
                float rLen = (br - bl).magnitude;
                float uLen = (tl - bl).magnitude;

                float u0 = 0f / tileMeters + uvOffset.x;
                float u1 = rLen / tileMeters + uvOffset.x;
                float v0 = 0f / tileMeters + uvOffset.y;
                float v1 = uLen / tileMeters + uvOffset.y;

                uvs[vi + 0] = new Vector2(u0, v0);
                uvs[vi + 1] = new Vector2(u1, v0);
                uvs[vi + 2] = new Vector2(u1, v1);
                uvs[vi + 3] = new Vector2(u0, v1);

                vi += 4;
            }

            // Faces (CCW when looking at the face)
            // +Y (top)
            AddFace(
                new Vector3(-hx,  hy, -hz),
                new Vector3( hx,  hy, -hz),
                new Vector3( hx,  hy,  hz),
                new Vector3(-hx,  hy,  hz),
                Vector3.up, Vector3.right, Vector3.forward);
            // -Y (bottom)
            AddFace(
                new Vector3(-hx, -hy,  hz),
                new Vector3( hx, -hy,  hz),
                new Vector3( hx, -hy, -hz),
                new Vector3(-hx, -hy, -hz),
                Vector3.down, Vector3.right, Vector3.back);
            // +X (right)
            AddFace(
                new Vector3( hx, -hy, -hz),
                new Vector3( hx, -hy,  hz),
                new Vector3( hx,  hy,  hz),
                new Vector3( hx,  hy, -hz),
                Vector3.right, Vector3.forward, Vector3.up);
            // -X (left)
            AddFace(
                new Vector3(-hx, -hy,  hz),
                new Vector3(-hx, -hy, -hz),
                new Vector3(-hx,  hy, -hz),
                new Vector3(-hx,  hy,  hz),
                Vector3.left, Vector3.back, Vector3.up);
            // +Z (front)
            AddFace(
                new Vector3(-hx, -hy,  hz),
                new Vector3( hx, -hy,  hz),
                new Vector3( hx,  hy,  hz),
                new Vector3(-hx,  hy,  hz),
                Vector3.forward, Vector3.right, Vector3.up);
            // -Z (back)
            AddFace(
                new Vector3( hx, -hy, -hz),
                new Vector3(-hx, -hy, -hz),
                new Vector3(-hx,  hy, -hz),
                new Vector3( hx,  hy, -hz),
                Vector3.back, Vector3.left, Vector3.up);

            var tris = new int[36];
            int ti = 0;
            int baseV = 0;
            for (int f = 0; f < 6; f++)
            {
                tris[ti + 0] = baseV + 0;
                tris[ti + 1] = baseV + 1;
                tris[ti + 2] = baseV + 2;
                tris[ti + 3] = baseV + 0;
                tris[ti + 4] = baseV + 2;
                tris[ti + 5] = baseV + 3;
                ti += 6; baseV += 4;
            }

            var mesh = new Mesh { name = name, hideFlags = HideFlags.None };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTangents(tans);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
#endif
}
