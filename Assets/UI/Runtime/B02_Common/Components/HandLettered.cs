using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // UI Component: the text in an element, and in everything inside it, looks lettered by hand. Each
    // letter leans and sits a little off the line, as if written in chalk. Add it in UXML
    // (<Bagel.HandLettered lean="4" bounce="0.06"/>), in UI Builder, or with AddComponent from C#.
    //
    // It's a mesh modifier: once the text's meshes are made, it turns each letter's quad a little around
    // its middle and moves it up or down. It only runs when the text changes, and the lean comes from the
    // letter's place, so the same text always leans the same way: no shimmering.
    [VisualElementComponent]
    [NoAutoStaticsCleanup] // a callback made once, the same for every element
    public partial struct HandLettered
    {
        // How far a letter turns, at most, in degrees.
        [UxmlAttribute] public float lean;

        // How far a letter leaves the line, at most, as a part of its height.
        [UxmlAttribute] public float bounce;

        static readonly MeshModificationCallback s_Letter = Letter;

        [UxmlCreateInstanceMethod]
        internal static HandLettered CreateDefault() => new HandLettered { lean = 4f, bounce = 0.06f };

        [OnComponentAdded]
        static void OnAdded(ref HandLettered self, VisualElement owner) => owner.AddMeshModifier(s_Letter, recursive: true);

        // A new lean or bounce: the text inside is made again, with the new values.
        [OnComponentChanged]
        static void OnChanged(ref HandLettered self, VisualElement owner)
        {
            owner.RemoveMeshModifier(s_Letter);
            owner.AddMeshModifier(s_Letter, recursive: true);
        }

        [OnComponentRemoved]
        static void OnRemoved(ref HandLettered self, VisualElement owner) => owner.RemoveMeshModifier(s_Letter);

        static void Letter(MeshModificationContext context)
        {
            // The closest element with the component has the values: this one, or an ancestor.
            var settings = CreateDefault();
            for (var element = context.element; element != null; element = element.hierarchy.parent)
            {
                if (element.HasComponent<HandLettered>())
                {
                    settings = element.GetComponent<HandLettered>();
                    break;
                }
            }

            foreach (var draw in context.draws)
            {
                if (draw.renderType != RenderType.SdfText && draw.renderType != RenderType.BitmapText)
                    continue;

                // Text is made of quads, one per letter: four corners each.
                var vertices = draw.vertices;
                for (int quad = 0; quad + 3 < vertices.Length; quad += 4)
                {
                    var a = vertices[quad];
                    var b = vertices[quad + 1];
                    var c = vertices[quad + 2];
                    var d = vertices[quad + 3];
                    var center = (a.position + b.position + c.position + d.position) * 0.25f;
                    var height = Mathf.Max(Mathf.Abs(a.position.y - c.position.y), Mathf.Abs(b.position.y - d.position.y));

                    // The same letter in the same place leans the same way every time.
                    var seed = Mathf.RoundToInt(center.x * 7f) * 73856093 ^ Mathf.RoundToInt(center.y * 7f) * 19349663;
                    var angle = (Hash(seed) - 0.5f) * 2f * settings.lean * Mathf.Deg2Rad;
                    var lift = (Hash(seed + 1) - 0.5f) * 2f * settings.bounce * height;
                    var cos = Mathf.Cos(angle);
                    var sin = Mathf.Sin(angle);

                    vertices[quad] = Turn(a, center, cos, sin, lift);
                    vertices[quad + 1] = Turn(b, center, cos, sin, lift);
                    vertices[quad + 2] = Turn(c, center, cos, sin, lift);
                    vertices[quad + 3] = Turn(d, center, cos, sin, lift);
                }
            }
        }

        static Vertex Turn(Vertex vertex, Vector3 center, float cos, float sin, float lift)
        {
            var offset = vertex.position - center;
            vertex.position = new Vector3(
                center.x + offset.x * cos - offset.y * sin,
                center.y + offset.x * sin + offset.y * cos + lift,
                vertex.position.z);
            return vertex;
        }

        static float Hash(int seed)
        {
            unchecked
            {
                var h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }
    }
}
