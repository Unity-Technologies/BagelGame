using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bagel
{
    // Collects vertices and triangles for a ProceduralMeshGenerator, one triangle list per submesh
    // (one submesh per material), then writes them into a standard Mesh.
    public class MeshBuilder
    {
        readonly List<Vector3> m_Positions = new();
        readonly List<Vector3> m_Normals = new();
        readonly List<Vector2> m_UVs = new();
        readonly List<List<int>> m_Submeshes = new() { new List<int>() };

        int m_Submesh;

        public int vertexCount => m_Positions.Count;

        // Triangles go to this submesh; setting it makes sure every submesh before it exists.
        public int submesh
        {
            get => m_Submesh;
            set
            {
                m_Submesh = Mathf.Max(0, value);
                while (m_Submeshes.Count <= m_Submesh)
                    m_Submeshes.Add(new List<int>());
            }
        }

        // Moves and turns what's added next, so one generator can place another's shape in its own.
        public Matrix4x4 transform { get; set; } = Matrix4x4.identity;

        public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            m_Positions.Add(transform.MultiplyPoint3x4(position));
            m_Normals.Add(transform.MultiplyVector(normal).normalized);
            m_UVs.Add(uv);
            return m_Positions.Count - 1;
        }

        // Clockwise, as seen from the side the face is visible from.
        public void AddTriangle(int a, int b, int c)
        {
            var triangles = m_Submeshes[m_Submesh];
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        // Clockwise corners, as seen from the side the face is visible from.
        public void AddQuad(int a, int b, int c, int d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        // A (columns + 1) x (rows + 1) grid of vertices laid out row by row, as from AddVertex in two
        // nested loops. Rows go up and columns go right, as seen from the visible side.
        public void AddGrid(int firstVertex, int columns, int rows)
        {
            int stride = columns + 1;
            for (int row = 0; row < rows; ++row)
            {
                for (int column = 0; column < columns; ++column)
                {
                    int i = firstVertex + row * stride + column;
                    AddQuad(i, i + stride, i + stride + 1, i + 1);
                }
            }
        }

        // A flat grid of quads centered on center, visible from the side right and up are seen from
        // (its normal is up x right). The UVs run from uvOffset to uvOffset + uvSize.
        public void AddPlane(Vector3 center, Vector3 right, Vector3 up, Vector2 size, Vector2Int segments, Vector2 uvSize, Vector2 uvOffset)
        {
            int columns = Mathf.Max(1, segments.x);
            int rows = Mathf.Max(1, segments.y);
            var normal = Vector3.Cross(up, right).normalized;
            int first = vertexCount;
            for (int row = 0; row <= rows; ++row)
            {
                float v = (float)row / rows;
                for (int column = 0; column <= columns; ++column)
                {
                    float u = (float)column / columns;
                    var position = center + right * ((u - 0.5f) * size.x) + up * ((v - 0.5f) * size.y);
                    AddVertex(position, normal, uvOffset + new Vector2(u * uvSize.x, v * uvSize.y));
                }
            }
            AddGrid(first, columns, rows);
        }

        // A box with every edge rounded to radius. Each topOpening (x and z around the center) leaves a
        // hole in the flat part of the top, for the caller to line.
        public void AddRoundedBox(Vector3 center, Vector3 size, float radius, int roundingSegments, IReadOnlyList<Rect> topOpenings = null)
        {
            var half = size * 0.5f;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(half.x, Mathf.Min(half.y, half.z)));
            roundingSegments = Mathf.Max(1, roundingSegments);

            AddRoundedFace(center, half, radius, roundingSegments, Vector3.right, Vector3.forward, topOpenings);
            AddRoundedFace(center, half, radius, roundingSegments, Vector3.right, Vector3.back, null);
            AddRoundedFace(center, half, radius, roundingSegments, Vector3.left, Vector3.up, null);
            AddRoundedFace(center, half, radius, roundingSegments, Vector3.right, Vector3.up, null);
            AddRoundedFace(center, half, radius, roundingSegments, Vector3.forward, Vector3.up, null);
            AddRoundedFace(center, half, radius, roundingSegments, Vector3.back, Vector3.up, null);
        }

        // One face of a box as a grid, its points pushed onto the rounded edges and corners. The grid is
        // finer toward the edges, so the rounding gets an even spread of angles.
        void AddRoundedFace(Vector3 center, Vector3 half, float radius, int roundingSegments, Vector3 right, Vector3 up, IReadOnlyList<Rect> openings)
        {
            var normal = Vector3.Cross(up, right);
            var inner = half - Vector3.one * radius;
            float halfRight = Mathf.Abs(Vector3.Dot(right, half));
            float halfUp = Mathf.Abs(Vector3.Dot(up, half));
            float halfNormal = Mathf.Abs(Vector3.Dot(normal, half));

            var columns = GridLines(halfRight, radius, roundingSegments, openings, true);
            var rows = GridLines(halfUp, radius, roundingSegments, openings, false);

            int first = vertexCount;
            foreach (var v in rows)
            {
                foreach (var u in columns)
                {
                    var point = normal * halfNormal + right * u + up * v;
                    var core = Vector3.Max(-inner, Vector3.Min(inner, point));
                    var outward = point - core;
                    var pointNormal = outward.sqrMagnitude > 1e-12f ? outward.normalized : normal;
                    AddVertex(center + core + pointNormal * radius, pointNormal, new Vector2(u / (halfRight * 2f) + 0.5f, v / (halfUp * 2f) + 0.5f));
                }
            }

            int stride = columns.Count;
            for (int row = 0; row < rows.Count - 1; ++row)
            {
                for (int column = 0; column < stride - 1; ++column)
                {
                    var cellCenter = new Vector2((columns[column] + columns[column + 1]) * 0.5f, (rows[row] + rows[row + 1]) * 0.5f);
                    if (InsideAny(openings, cellCenter))
                        continue;

                    int i = first + row * stride + column;
                    AddQuad(i, i + stride, i + stride + 1, i + 1);
                }
            }
        }

        static List<float> GridLines(float halfExtent, float radius, int roundingSegments, IReadOnlyList<Rect> openings, bool alongX)
        {
            var lines = new List<float>();
            for (int k = 0; k <= roundingSegments; ++k)
            {
                // Where the rounding's angle reaches 45 + 45 * k / roundingSegments degrees.
                float angle = (45f + 45f * k / roundingSegments) * Mathf.Deg2Rad;
                float fromEdge = radius * (1f - Mathf.Cos(angle) / Mathf.Sin(angle));
                lines.Add(-halfExtent + fromEdge);
                lines.Add(halfExtent - fromEdge);
            }

            if (openings != null)
            {
                foreach (var opening in openings)
                {
                    lines.Add(alongX ? opening.xMin : opening.yMin);
                    lines.Add(alongX ? opening.xMax : opening.yMax);
                }
            }

            lines.Sort();
            for (int i = lines.Count - 1; i > 0; --i)
            {
                if (lines[i] - lines[i - 1] < 1e-5f)
                    lines.RemoveAt(i);
            }
            return lines;
        }

        static bool InsideAny(IReadOnlyList<Rect> openings, Vector2 point)
        {
            if (openings == null)
                return false;
            foreach (var opening in openings)
            {
                if (opening.Contains(point))
                    return true;
            }
            return false;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = m_Positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(m_Positions);
            mesh.SetNormals(m_Normals);
            mesh.SetUVs(0, m_UVs);
            mesh.subMeshCount = m_Submeshes.Count;
            for (int i = 0; i < m_Submeshes.Count; ++i)
                mesh.SetTriangles(m_Submeshes[i], i, calculateBounds: false);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
