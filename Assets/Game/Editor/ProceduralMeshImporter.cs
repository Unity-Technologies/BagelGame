using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Bagel
{
    // Imports a .procmesh file as a standard Mesh. The file itself is only a placeholder: the
    // generator and its inputs live in the .meta file, where the inspector edits them.
    [ScriptedImporter(1, k_Extension)]
    public class ProceduralMeshImporter : ScriptedImporter
    {
        const string k_Extension = "procmesh";

        // Changes whenever the game's code does, so every procedural mesh is rebuilt with the new code.
        const string k_CodeDependency = "Bagel/ProceduralMeshCode";

        [SerializeReference] ProceduralMeshGenerator m_Generator = new BoxMeshGenerator();

        public ProceduralMeshGenerator generator
        {
            get => m_Generator;
            set => m_Generator = value;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            ctx.DependsOnCustomDependency(k_CodeDependency);

            var mesh = (m_Generator ?? new BoxMeshGenerator()).Generate(Path.GetFileNameWithoutExtension(ctx.assetPath));
            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }

        [InitializeOnLoadMethod]
        static void RegisterCodeDependency()
        {
            // With deterministic compilation, the assembly's id only changes when its code does.
            var id = typeof(ProceduralMeshGenerator).Assembly.ManifestModule.ModuleVersionId;
            AssetDatabase.RegisterCustomDependency(k_CodeDependency, Hash128.Compute(id.ToString()));
        }

        [MenuItem("Assets/Create/Bagel/Procedural Mesh", priority = 300)]
        static void CreateProceduralMesh()
        {
            ProjectWindowUtil.CreateAssetWithTextContent($"New Mesh.{k_Extension}", "Procedural mesh: its inputs are in the .meta file.\n");
        }
    }
}
