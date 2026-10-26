using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Bagel
{
    // The level editor's live pictures of the library's things: one of each, only its looks (no bodies, colliders or
    // scripts), each on a little turntable in a grid, tipped toward the camera to be seen from a little above, all
    // taken by one orthographic camera into one texture. They turn slowly on their own, and spin when they're dragged,
    // slowing back down. In the game they're far below the bakery, on a layer only their camera sees (the bakery's
    // light lights them); in the Editor they're in a scene of their own, with their own light, and the window renders
    // them as it updates.
    public class BagelLevelStudio : IBagelLevelThumbnails, IDisposable
    {
        public const string layerName = "LevelStudio";
        public static int runtimeLayer => LayerMask.NameToLayer(layerName);

        const int k_CellPixels = 192;
        const float k_Cell = 2f;
        const float k_Fill = 0.86f;
        const float k_Tip = 24f;
        const float k_IdleSpeed = 28f;
        const float k_SlowDown = 360f;
        const float k_MaxSpeed = 900f;
        static readonly Vector3 s_RuntimePlace = new(0f, -1000f, 0f);
        static readonly Color s_Background = new(0.17f, 0.12f, 0.09f, 1f);

        class Turntable
        {
            public Transform spinner;
            public float angle;
            public float speed = k_IdleSpeed;
            public Rect uv;
        }

        readonly Dictionary<string, Turntable> m_Turntables = new();
        GameObject m_Root;
        Camera m_Camera;
        RenderTexture m_Texture;
        bool m_RendersItself;

        public Texture texture => m_Texture;
        public Camera camera => m_Camera;
        public GameObject root => m_Root;

        // In the game: on the given layer, far away, the camera rendering every frame by itself. In the Editor: in the
        // given scene (a preview scene), lit, rendered by Render.
        public static BagelLevelStudio Create(BagelLevelLibrary library, int layer, Scene? scene = null)
        {
            var studio = new BagelLevelStudio();
            studio.Build(library, layer, scene);
            return studio;
        }

        void Build(BagelLevelLibrary library, int layer, Scene? scene)
        {
            var entries = library != null ? library.entries : Array.Empty<BagelLevelLibrary.Entry>();
            var count = Mathf.Max(1, entries.Count);
            var columns = Mathf.CeilToInt(Mathf.Sqrt(count * 1.5f));
            var rows = Mathf.CeilToInt(count / (float)columns);

            m_Root = new GameObject("LevelStudio") { hideFlags = HideFlags.HideAndDontSave, layer = Mathf.Max(0, layer) };
            if (scene.HasValue)
                SceneManager.MoveGameObjectToScene(m_Root, scene.Value);
            else
                m_Root.transform.position = s_RuntimePlace;

            // A spare parent, inactive, so nothing in a copy wakes up before it's only its looks.
            var holder = new GameObject("Holder") { hideFlags = HideFlags.HideAndDontSave };
            holder.SetActive(false);
            holder.transform.SetParent(m_Root.transform, false);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.prefab == null || m_Turntables.ContainsKey(entry.id))
                    continue;
                int column = i % columns, row = i / columns;
                var cell = new GameObject(entry.id) { hideFlags = HideFlags.HideAndDontSave }.transform;
                cell.SetParent(m_Root.transform, false);
                cell.localPosition = new Vector3((column + 0.5f) * k_Cell, -(row + 0.5f) * k_Cell, 0f);
                // Tipped toward the camera, to be seen from a little above.
                cell.localRotation = Quaternion.Euler(-k_Tip, 0f, 0f);
                var spinner = new GameObject("Spinner") { hideFlags = HideFlags.HideAndDontSave }.transform;
                spinner.SetParent(cell, false);

                var copy = Object.Instantiate(entry.prefab, holder.transform);
                copy.name = entry.id;
                Strip(copy);
                copy.transform.SetParent(spinner, false);
                Fit(copy.transform, spinner);
                SetLayerAndFlags(copy, m_Root.layer);
                copy.SetActive(true);

                m_Turntables[entry.id] = new Turntable
                {
                    spinner = spinner,
                    angle = i * 37f,
                    uv = new Rect(column / (float)columns, 1f - (row + 1) / (float)rows, 1f / columns, 1f / rows)
                };
            }
            Object.DestroyImmediate(holder);

            m_Texture = new RenderTexture(columns * k_CellPixels, rows * k_CellPixels, 24, RenderTextureFormat.ARGB32)
            {
                name = "LevelStudio",
                hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 4
            };
            m_Texture.Create();

            var cameraObject = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave, layer = m_Root.layer };
            cameraObject.transform.SetParent(m_Root.transform, false);
            cameraObject.transform.localPosition = new Vector3(columns * k_Cell * 0.5f, -rows * k_Cell * 0.5f, -20f);
            m_Camera = cameraObject.AddComponent<Camera>();
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = rows * k_Cell * 0.5f;
            m_Camera.nearClipPlane = 1f;
            m_Camera.farClipPlane = 40f;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = s_Background;
            m_Camera.allowHDR = false;
            m_Camera.allowMSAA = true;
            m_Camera.targetTexture = m_Texture;
            m_Camera.cullingMask = 1 << m_Root.layer;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;

            if (scene.HasValue)
            {
                m_Camera.scene = scene.Value;
                m_Camera.enabled = false;
                AddLight(Quaternion.Euler(40f, -30f, 0f), 1.2f, new Color(1f, 0.96f, 0.88f));
                AddLight(Quaternion.Euler(-10f, 150f, 0f), 0.5f, new Color(0.85f, 0.9f, 1f));
            }
            else
            {
                m_RendersItself = true;
            }
        }

        void AddLight(Quaternion rotation, float intensity, Color color)
        {
            var go = new GameObject("Light") { hideFlags = HideFlags.HideAndDontSave, layer = m_Root.layer };
            go.transform.SetParent(m_Root.transform, false);
            go.transform.rotation = rotation;
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
        }

        // Only its looks: what draws it, nothing that moves or thinks.
        static void Strip(GameObject copy)
        {
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(behaviour);
            foreach (var collider in copy.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (var body in copy.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(body);
            foreach (var component in copy.GetComponentsInChildren<Component>(true))
            {
                if (component is Transform || component is MeshFilter || component is MeshRenderer)
                    continue;
                Object.DestroyImmediate(component);
            }
            foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // Its middle on the turntable's axis, as big as fits in its cell however it's turned.
        static void Fit(Transform copy, Transform spinner)
        {
            var renderers = copy.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
                return;
            var bounds = new Bounds();
            var first = true;
            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                var local = filter.sharedMesh.bounds;
                var matrix = spinner.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                foreach (var corner in Corners(local))
                {
                    var point = matrix.MultiplyPoint3x4(corner);
                    if (first)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
            if (first)
                return;
            var radius = Mathf.Max(0.01f, new Vector2(Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)), Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z))).magnitude);
            var half = Mathf.Max(radius, bounds.extents.y + Mathf.Abs(bounds.center.y));
            var scale = k_Cell * 0.5f * k_Fill / half;
            copy.localScale *= scale;
            copy.localPosition = new Vector3(0f, -bounds.center.y * scale, 0f);
        }

        static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (int i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x, (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z);
        }

        static void SetLayerAndFlags(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            foreach (Transform child in gameObject.transform)
                SetLayerAndFlags(child.gameObject, layer);
        }

        public Rect UV(string kind) => kind != null && m_Turntables.TryGetValue(kind, out var turntable) ? turntable.uv : Rect.zero;

        public void Spin(string kind, float degrees)
        {
            if (kind == null || !m_Turntables.TryGetValue(kind, out var turntable))
                return;
            turntable.angle += degrees;
            turntable.speed = Mathf.Clamp(degrees * 30f, -k_MaxSpeed, k_MaxSpeed);
            turntable.spinner.localRotation = Quaternion.Euler(0f, turntable.angle, 0f);
        }

        // Turns: spun ones slow back down to the idle turn.
        public void Update(float deltaTime)
        {
            foreach (var turntable in m_Turntables.Values)
            {
                turntable.speed = Mathf.MoveTowards(turntable.speed, k_IdleSpeed, k_SlowDown * deltaTime);
                turntable.angle = Mathf.Repeat(turntable.angle + turntable.speed * deltaTime, 360f);
                if (turntable.spinner != null)
                    turntable.spinner.localRotation = Quaternion.Euler(0f, turntable.angle, 0f);
            }
        }

        // In the Editor, where nothing renders it by itself.
        public void Render()
        {
            if (m_Camera != null && !m_RendersItself)
                m_Camera.Render();
        }

        public void Dispose()
        {
            if (m_Camera != null)
                m_Camera.targetTexture = null;
            if (m_Root != null)
                Destroy(m_Root);
            if (m_Texture != null)
            {
                m_Texture.Release();
                Destroy(m_Texture);
            }
            m_Root = null;
            m_Camera = null;
            m_Texture = null;
            m_Turntables.Clear();
        }

        static void Destroy(Object o)
        {
            if (Application.isPlaying)
                Object.Destroy(o);
            else
                Object.DestroyImmediate(o);
        }
    }
}
