using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bagel.B00_Meshes
{
    // Every mesh in the game comes from a .procmesh: its inputs are in the .meta file and its code
    // is a ProceduralMeshGenerator.
    public class ProceduralMeshTests
    {
        const string k_BagelHull = "Assets/Game/Bagels/Controller/BagelHull.procmesh";

        static readonly Regex s_MeshReference = new(@"m_Mesh: (\{[^}]*\})|propertyPath: m_Mesh\s+value:[^\n]*\n\s+objectReference: (\{[^}]*\})");
        static readonly Regex s_Reference = new(@"fileID: (-?\d+)(?:, guid: ([0-9a-f]+))?");

        public static IEnumerable<string> proceduralMeshes => AssetDatabase.FindAssets("glob:\"*.procmesh\"", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath);

        static IEnumerable<string> scenesAndPrefabs => AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct();

        [Test]
        public void EveryMeshInTheGameIsProcedural()
        {
            var problems = new List<string>();
            foreach (var path in scenesAndPrefabs)
            {
                foreach (Match match in s_MeshReference.Matches(File.ReadAllText(path).Replace("\r\n", "\n")))
                {
                    var reference = s_Reference.Match(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
                    if (reference.Groups[1].Value == "0")
                        continue;

                    var meshPath = reference.Groups[2].Success ? AssetDatabase.GUIDToAssetPath(reference.Groups[2].Value) : "(saved in the file)";
                    if (!meshPath.EndsWith(".procmesh"))
                        problems.Add($"{path} uses a mesh from {meshPath}");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryMeshImportsAsItsInputsBuildIt([ValueSource(nameof(proceduralMeshes))] string path)
        {
            var importer = (ProceduralMeshImporter)AssetImporter.GetAtPath(path);
            var imported = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var built = importer.generator.Generate("Built");
            try
            {
                Assert.AreEqual(built.vertices, imported.vertices, "The imported mesh is out of date: reimport it.");
                Assert.AreEqual(built.normals, imported.normals);
                Assert.AreEqual(built.subMeshCount, imported.subMeshCount);
                for (int i = 0; i < built.subMeshCount; ++i)
                    Assert.AreEqual(built.GetTriangles(i), imported.GetTriangles(i));
            }
            finally
            {
                Object.DestroyImmediate(built);
            }
        }

        [Test]
        public void EveryMeshIsWellFormed([ValueSource(nameof(proceduralMeshes))] string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Assert.Greater(mesh.vertexCount, 0);
            foreach (var position in mesh.vertices)
                Assert.IsFalse(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z), "A vertex isn't a number");
            foreach (var normal in mesh.normals)
                Assert.AreEqual(1f, normal.magnitude, 1e-3f, "A normal isn't unit length");
            for (int i = 0; i < mesh.subMeshCount; ++i)
                Assert.Greater(mesh.GetIndexCount(i), 0, $"Submesh {i} is empty");
        }

        // Each triangle is wound to be seen from the side its normals point to.
        [Test]
        public void EveryTriangleFacesItsNormals([ValueSource(nameof(proceduralMeshes))] string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            int against = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var faceNormal = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
                var vertexNormals = normals[triangles[i]] + normals[triangles[i + 1]] + normals[triangles[i + 2]];
                if (Vector3.Dot(faceNormal, vertexNormals) < 0f)
                    ++against;
            }
            Assert.AreEqual(0, against, "Triangles are wound against their normals");
        }

        // Convex shapes centered on their bounds: every face points away from the center.
        [TestCase("Assets/Game/Meshes/Cube.procmesh")]
        [TestCase("Assets/Game/Meshes/Cylinder.procmesh")]
        [TestCase(k_BagelHull)]
        public void ConvexMeshesFaceOutward(string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var faceNormal = Vector3.Cross(b - a, c - a);
                Assert.Greater(Vector3.Dot(faceNormal, (a + b + c) / 3f - mesh.bounds.center), 0f, $"Triangle {i / 3} faces inward");
            }
        }

        [Test]
        public void TheBagelColliderFitsInAConvexHull()
        {
            var hull = AssetDatabase.LoadAssetAtPath<Mesh>(k_BagelHull);
            Assert.LessOrEqual(hull.triangles.Length / 3, BagelHullMeshGenerator.maxTriangles);
            Assert.LessOrEqual(hull.vertices.Distinct().Count(), 255);

            Physics.BakeMesh(hull.GetEntityId(), true);
            LogAssert.NoUnexpectedReceived();

            var controller = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Bagels/Controller/BagelController.prefab");
            var collider = controller.GetComponent<MeshCollider>();
            Assert.AreEqual(hull, collider.sharedMesh);
            Assert.IsTrue(collider.convex);
        }

        // The collider's rim is what rolls, so it should be the size of the bagel that's seen.
        [Test]
        public void TheBagelColliderMatchesTheBagel()
        {
            var hull = AssetDatabase.LoadAssetAtPath<Mesh>(k_BagelHull);
            foreach (var path in proceduralMeshes.Where(path => path.Contains("BagelTypes")))
            {
                var bagel = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Assert.AreEqual(hull.bounds.extents.z, bagel.bounds.extents.z, 0.05f, path);
                Assert.AreEqual(0f, bagel.bounds.min.y, 0.01f, $"{path} doesn't sit on its base");
            }
        }

        [Test]
        public void EveryRendererHasAMaterialForEachSubmesh()
        {
            var problems = new List<string>();
            foreach (var path in scenesAndPrefabs.Where(path => path.EndsWith(".prefab")))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (filter.sharedMesh != null && renderer != null && renderer.sharedMaterials.Length != filter.sharedMesh.subMeshCount)
                        problems.Add($"{path}: {filter.name} has {renderer.sharedMaterials.Length} materials for {filter.sharedMesh.subMeshCount} submeshes");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // A Mesh Collider's collision is cooked when the game is built: the .procmesh says so in its inputs.
        [Test]
        public void EveryMeshColliderMeshIsPreBaked()
        {
            var problems = new List<string>();
            foreach (var path in scenesAndPrefabs.Where(path => path.EndsWith(".prefab")))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var collider in prefab.GetComponentsInChildren<MeshCollider>(true))
                {
                    if (collider.sharedMesh != null && !collider.sharedMesh.HasPreBakeCollisionMesh(collider.convex))
                        problems.Add($"{path}: {collider.name}'s {collider.sharedMesh.name} isn't pre-baked for {(collider.convex ? "convex" : "triangle")} collision");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void TheSameInputsAlwaysBuildTheSameBagel()
        {
            var generator = new BagelMeshGenerator { seed = 7 };
            generator.toppings.Add(new BagelMeshGenerator.Topping { count = 50, size = new Vector3(0.04f, 0.01f, 0.02f), sides = 6, submesh = 1 });
            var first = generator.Generate("First");
            var second = generator.Generate("Second");
            generator.seed = 8;
            var other = generator.Generate("Other");
            try
            {
                Assert.AreEqual(first.vertices, second.vertices);
                Assert.AreNotEqual(first.vertices, other.vertices);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(other);
            }
        }
    }
}
