using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Picks a .procmesh's generator and edits its inputs; Apply rebuilds the mesh.
    [CustomEditor(typeof(ProceduralMeshImporter))]
    public class ProceduralMeshImporterEditor : ScriptedImporterEditor
    {
        const string k_GeneratorPath = "m_Generator";

        VisualElement m_Inputs;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var generatorTypes = TypeCache.GetTypesDerivedFrom<ProceduralMeshGenerator>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(type => type.Name)
                .ToList();

            var generator = serializedObject.FindProperty(k_GeneratorPath);
            var names = generatorTypes.Select(DisplayName).ToList();
            var current = generatorTypes.FindIndex(type => generator.managedReferenceFullTypename == FullTypename(type));
            var typeField = new DropdownField("Generator", names, Mathf.Max(0, current));
            typeField.RegisterValueChangedCallback(evt =>
            {
                var type = generatorTypes[names.IndexOf(evt.newValue)];
                serializedObject.Update();
                serializedObject.FindProperty(k_GeneratorPath).managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
                ShowInputs();
            });
            root.Add(typeField);

            m_Inputs = new VisualElement();
            root.Add(m_Inputs);
            ShowInputs();

            // For a Mesh Collider: cooking its collision at build time.
            var collision = new Foldout { text = "Mesh Collider", value = false };
            collision.Add(new PropertyField(serializedObject.FindProperty("m_PreBakeTriangleCollision"), "Pre-Bake Triangle Collision"));
            collision.Add(new PropertyField(serializedObject.FindProperty("m_PreBakeConvexCollision"), "Pre-Bake Convex Collision"));
            collision.Bind(serializedObject);
            root.Add(collision);

            var stats = new HelpBox(Stats(), HelpBoxMessageType.None);
            stats.style.marginTop = 6;
            root.Add(stats);

            root.Add(new ApplyRevertButtonContainer(this));
            return root;
        }

        // One field per input, without the generator's own foldout around them.
        void ShowInputs()
        {
            m_Inputs.Unbind();
            m_Inputs.Clear();

            var generator = serializedObject.FindProperty(k_GeneratorPath);
            var end = generator.GetEndProperty();
            var input = generator.Copy();
            if (input.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(input, end))
                {
                    m_Inputs.Add(new PropertyField(input.Copy()));
                    if (!input.NextVisible(false))
                        break;
                }
            }
            m_Inputs.Bind(serializedObject);
        }

        string Stats()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(((AssetImporter)target).assetPath);
            if (mesh == null)
                return "Not imported yet.";
            return $"{mesh.vertexCount} vertices, {TriangleCount(mesh)} triangles, {mesh.subMeshCount} submesh(es), size {mesh.bounds.size}";
        }

        static int TriangleCount(Mesh mesh)
        {
            long indices = 0;
            for (int i = 0; i < mesh.subMeshCount; ++i)
                indices += mesh.GetIndexCount(i);
            return (int)(indices / 3);
        }

        static string DisplayName(Type type) => ObjectNames.NicifyVariableName(type.Name.Replace("MeshGenerator", ""));

        static string FullTypename(Type type) => $"{type.Assembly.GetName().Name} {type.FullName}";
    }
}
