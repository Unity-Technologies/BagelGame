using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // Every UI Toolkit feature Bagel Game uses: what it is, how the game uses it, the game's own UI that
    // shows it, live, in the Bagel Features window, and the lines of the project it's in. The features
    // themselves are in BagelFeatureCatalog.Features.cs.
    //
    // A source finds its line by the line's text, not its number, so it stays on the right line as files
    // change. The window's tests check that every line is still there, and that every example still has
    // the parts it outlines.
    [NoAutoStaticsCleanup] // a read-only table, made once
    public static partial class BagelFeatureCatalog
    {
        public enum Category
        {
            Authoring,
            DataAndLanguage,
            Styling,
            Effects,
            Controls,
            Panels,
            EditorAndTests
        }

        public static string CategoryName(Category category) => category switch
        {
            Category.Authoring => "UXML and elements",
            Category.DataAndLanguage => "Data and language",
            Category.Styling => "Styling",
            Category.Effects => "Drawing and effects",
            Category.Controls => "Controls and input",
            Category.Panels => "Panels",
            _ => "Editor and tests"
        };

        // A line of the project that uses the feature.
        public sealed class Source
        {
            static readonly string[] s_TextExtensions = { ".cs", ".uxml", ".uss", ".tss", ".shader", ".hlsl" };

            public readonly string path;
            public readonly string find;
            public readonly string what;

            public Source(string path, string find, string what)
            {
                this.path = path;
                this.find = find;
                this.what = what;
            }

            public string fileName => Path.GetFileName(path);

            // Code and UI files open in the code editor at the line; assets (a scene, PanelSettings) show in
            // the Project window and the Inspector.
            public bool opensInCodeEditor => s_TextExtensions.Contains(Path.GetExtension(path));

            // The line's number, from 1; 0 when it isn't there any more.
            public int FindLine()
            {
                if (!File.Exists(path))
                    return 0;
                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(find, StringComparison.Ordinal))
                        return i + 1;
                }
                return 0;
            }

            public void Open()
            {
                if (opensInCodeEditor)
                    InternalEditorUtility.OpenFileAtLineExternal(Path.GetFullPath(path), Mathf.Max(1, FindLine()));
                else
                    Ping(path);
            }
        }

        // An element of an example that uses the feature: it gets an outline, the first one its label.
        public sealed class Part
        {
            public readonly string label;
            public readonly Func<VisualElement, bool> matches;

            public Part(string label, Func<VisualElement, bool> matches)
            {
                this.label = label;
                this.matches = matches;
            }
        }

        // Something to do to an example: change the state it plays on, or the screens themselves.
        public sealed class TryIt
        {
            public readonly string label;
            public readonly string tooltip;
            public readonly Action<BagelFeaturesWindow> run;

            public TryIt(string label, string tooltip, Action<BagelFeaturesWindow> run)
            {
                this.label = label;
                this.tooltip = tooltip;
                this.run = run;
            }
        }

        // Where to see the feature in the Editor.
        public sealed class Link
        {
            public enum Kind { UIBuilder, Asset, GameObject, Menu, Action }

            public readonly Kind kind;
            public readonly string label;
            public readonly string tooltip;
            // What it opens: an asset's path, a GameObject's name or a menu item's path (for the tests).
            public readonly string target;
            readonly Action m_Open;

            public Link(Kind kind, string label, string tooltip, string target, Action open)
            {
                this.kind = kind;
                this.label = label;
                this.tooltip = tooltip;
                this.target = target;
                m_Open = open;
            }

            public void Open() => m_Open?.Invoke();
        }

        // The game's UI that shows a feature: one of the Bagel All UI window's cards, at a moment of the
        // game (one of its setups), with the parts that use the feature outlined. Or, for a feature of the
        // Editor's, the Inspector of the state the window plays on.
        public sealed class Example
        {
            public readonly string card;
            public readonly string setup;
            public readonly Part[] parts;
            // Once the screens are up: what a setup can't do (open a tab).
            public readonly Action<VisualElement> prepare;
            public readonly bool inspector;

            public Example(string card, string setup, Part[] parts, Action<VisualElement> prepare = null, bool inspector = false)
            {
                this.card = card;
                this.setup = setup;
                this.parts = parts ?? Array.Empty<Part>();
                this.prepare = prepare;
                this.inspector = inspector;
            }

            public BagelAllUIWindow.CardSpec cardSpec => BagelAllUIWindow.cardSpecs.FirstOrDefault(spec => spec.title == card);
        }

        public sealed class Feature
        {
            public readonly string id;
            public readonly Category category;
            public readonly string title;
            // What the feature is, in UI Toolkit.
            public readonly string about;
            // How Bagel Game uses it.
            public readonly string usage;
            // Null for a feature with nothing to show live (one of the Editor's).
            public readonly Example example;
            public readonly Source[] sources;
            public readonly Link[] links;
            public readonly TryIt[] tries;

            public Feature(string id, Category category, string title, string about, string usage, Example example, Source[] sources, Link[] links, TryIt[] tries)
            {
                this.id = id;
                this.category = category;
                this.title = title;
                this.about = about;
                this.usage = usage;
                this.example = example;
                this.sources = sources;
                this.links = links;
                this.tries = tries;
            }

            // Everything the search looks in, lower case.
            public string searchText => m_SearchText ??= string.Join("\n",
                new[] { id, title, about, usage, CategoryName(category) }
                    .Concat(sources.SelectMany(source => new[] { source.fileName, source.what }))).ToLowerInvariant();
            string m_SearchText;
        }

        static Feature[] s_Features;

        public static IReadOnlyList<Feature> features => s_Features ??= CreateFeatures();

        public static Feature Get(string id) => features.FirstOrDefault(feature => feature.id == id);

        // ------------------------------------------------------------------ Helpers for the features

        static Feature F(string id, Category category, string title, string about, string usage, Example example, params object[] items) =>
            new(id, category, title, about, usage, example, items.OfType<Source>().ToArray(), items.OfType<Link>().ToArray(), items.OfType<TryIt>().ToArray());

        static Source Src(string path, string find, string what) => new(path, find, what);

        static Example Show(string card, string setup, params Part[] parts) => new(card, setup, parts);

        static Example Show(string card, string setup, Action<VisualElement> prepare, params Part[] parts) => new(card, setup, parts, prepare);

        static Example StateInspector() => new(null, null, null, inspector: true);

        static Part Name(string name) => new("#" + name, element => element.name == name);

        static Part Class(string className) => new("." + className, element => element.ClassListContains(className));

        static Part Is<T>() where T : VisualElement => new(typeof(T).Name, element => element is T);

        static Part Has<T>() where T : struct, IVisualElementComponent => new(typeof(T).Name, element => element.HasComponent<T>());

        static TryIt Try(string label, string tooltip, Action<BagelFeaturesWindow> run) => new(label, tooltip, run);

        static Link UIBuilder(string uxmlPath) =>
            new(Link.Kind.UIBuilder, "UI Builder: " + Path.GetFileName(uxmlPath), "Open " + uxmlPath + " in UI Builder", uxmlPath,
                () => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath)));

        static Link Asset(string path) =>
            new(Link.Kind.Asset, Path.GetFileName(path), "Show " + path + " in the Project window and the Inspector", path, () => Ping(path));

        static Link SceneObject(string gameObjectName, string label) =>
            new(Link.Kind.GameObject, label, $"Select {gameObjectName} in the Main scene's Hierarchy", gameObjectName, () => SelectInScene(gameObjectName));

        static Link Menu(string menuPath, string label) =>
            new(Link.Kind.Menu, label, "Open " + menuPath, menuPath, () => EditorApplication.ExecuteMenuItem(menuPath));

        static Link Do(string label, string tooltip, Action open) => new(Link.Kind.Action, label, tooltip, null, open);

        static void Ping(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        static void SelectInScene(string gameObjectName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    var found = root.name == gameObjectName ? root.transform : FindChild(root.transform, gameObjectName);
                    if (found == null)
                        continue;
                    Selection.activeGameObject = found.gameObject;
                    EditorGUIUtility.PingObject(found.gameObject);
                    return;
                }
            }
            Debug.Log($"[Bagel] {gameObjectName} is in the Main scene: open it to select it.");
        }

        static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
                var found = FindChild(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
