using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The icons of the Editor's Bagel strips (BagelOptionStrip): the game's own SVGs for the themes and
    // languages, the Bagel All UI window's SVGs for the game states, and the Editor's icons for Reset and
    // the Inspector. Each is a small element with the image as its background.
    public static class BagelEditorIcons
    {
        const string k_RuntimeIcons = "Assets/UI/Runtime/B02_Common/Icons/";
        const string k_EditorIcons = "Assets/UI/Editor/B10_EditorTools/Icons/";

        // A theme's own icon (the theme collection has it): a chalkboard, a lightning bolt, a sun...
        public static VisualElement Theme(BagelThemeCollection.ThemeEntry entry) => Vector(entry.icon);

        // A language: a speech bubble with its letters. One the game has no icon for shows its code.
        public static VisualElement Language(string localeCode)
        {
            var image = AssetDatabase.LoadAssetAtPath<VectorImage>($"{k_RuntimeIcons}language-{localeCode}.svg");
            if (image != null)
                return Vector(image);

            var label = new Label(localeCode.ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 8f;
            label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0f;
            label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0f;
            return label;
        }

        // A game state of the Bagel All UI window, or one of its tools: state-main-menu, tool-simulate-roll...
        public static VisualElement Window(string iconName) =>
            Vector(AssetDatabase.LoadAssetAtPath<VectorImage>($"{k_EditorIcons}{iconName}.svg"));

        // Start again: the Editor's Refresh icon, turned to go back.
        public static VisualElement Reset()
        {
            var icon = Editor("Refresh");
            icon.style.scale = new Scale(new Vector2(-1f, 1f));
            return icon;
        }

        public static VisualElement Inspector() => Editor("UnityEditor.InspectorWindow");

        static VisualElement Vector(VectorImage image)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            if (image != null)
                icon.style.backgroundImage = Background.FromVectorImage(image);
            return icon;
        }

        // One of the Editor's icons, for its skin (EditorGUIUtility picks the dark one in the dark skin).
        static VisualElement Editor(string iconName)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            if (EditorGUIUtility.IconContent(iconName)?.image is Texture2D texture)
                icon.style.backgroundImage = texture;
            return icon;
        }
    }
}
