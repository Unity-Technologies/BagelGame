using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Steam rising off the cup of coffee by the toaster, moved on the GPU by a UI Shader Graph's vertex stage.
    //
    // Each puff is a cloud drawn once with Painter2D into a VectorImage, and drawn once, where it starts at the
    // bottom of the element. A mesh modifier writes each puff's motion into its vertices' extra channels (UV1 to
    // UV3: where it starts, when, how fast, how far it sways, how big it grows), and b-steam--gpu sets
    // -unity-material to Steam.mat, whose shader graph (Steam.hlsl) moves the puffs up every frame. The CPU only
    // sets the shader global _BagelTime: the puffs are never drawn again while they rise.
    //
    // The material needs a panel with those channels (PanelSettings.extraVertexChannels; the steam has panel
    // settings of its own, so no other panel pays for them). Without them, in UI Builder say, the puffs stand
    // still at their places along the way up. The puffs are in the element's color (USS color).
    [UxmlElement(libraryPath = "Bagel/Kitchen")]
    [NoAutoStaticsCleanup] // read-only
    public partial class SteamPuffs : VisualElement
    {
        public static readonly string ussClassName = "b-steam";

        // On when the panel has the vertex channels: the class sets the material that moves the puffs.
        public static readonly UniqueStyleString gpuUssClassName = new(ussClassName + "--gpu");

        // The extra vertex channels Steam.shadergraph reads.
        public const ExtraVertexChannels vertexChannels = ExtraVertexChannels.TexCoord1 | ExtraVertexChannels.TexCoord2 | ExtraVertexChannels.TexCoord3;

        public static readonly int timeId = Shader.PropertyToID("_BagelTime");

        // _BagelTime starts again from 0 after this many seconds, so a float keeps its precision however long
        // the game runs.
        const double k_TimeLap = 1024.0;

        // Where the puffs start and where they're gone, as fractions of the height.
        const float k_Bottom = 0.96f;
        const float k_Top = 0.06f;

        struct Puff
        {
            public float x;
            public float offset;
            public float rise;
            public float sway;
            public float swayFrequency;
            public float grow;
            public float size;
        }

        int m_Count = 9;
        int m_Seed = 1;
        Puff[] m_Puffs;
        VectorImage m_Shape;
        Color m_ShapeColor;
        float m_ShapeExtent;
        bool m_DrawnWithChannels;
        Vector2 m_DrawnSize;

        [UxmlAttribute]
        public int count
        {
            get => m_Count;
            set
            {
                m_Count = Mathf.Clamp(value, 1, 32);
                Respawn();
            }
        }

        // Which puffs: each seed spreads them its own way.
        [UxmlAttribute]
        public int seed
        {
            get => m_Seed;
            set
            {
                m_Seed = value;
                Respawn();
            }
        }

        // Whether the shader moves the puffs: drawn last with the panel's vertex channels.
        public bool movesOnTheGpu => m_DrawnWithChannels;

        public SteamPuffs()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            AddMeshModifier(WriteMotion);
            RegisterCallback<AttachToPanelEvent>(_ => EnableInClassList(gpuUssClassName, HasChannels(panel)));
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseShape());
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
            Respawn();
            schedule.Execute(Tick).Every(0);
        }

        static bool HasChannels(IPanel panel) => panel != null && (panel.extraVertexChannels & vertexChannels) == vertexChannels;

        // The shader's clock, now; set as the global the material reads.
        public static float UpdateShaderTime()
        {
            var now = (float)(Time.realtimeSinceStartupAsDouble % k_TimeLap);
            Shader.SetGlobalFloat(timeId, now);
            return now;
        }

        void Respawn()
        {
            m_Puffs = new Puff[m_Count];
            for (int i = 0; i < m_Count; i++)
            {
                var key = i * 977 + m_Seed * 7919;
                ref var p = ref m_Puffs[i];
                p.x = Range(key, 1, 0.38f, 0.62f);
                p.rise = Range(key, 2, 2.6f, 3.6f);
                // Spread over the way up, so the column of steam is never empty.
                p.offset = p.rise * i / m_Count;
                p.sway = Range(key, 3, 0.08f, 0.16f);
                p.swayFrequency = Range(key, 4, 1.2f, 2.2f);
                p.grow = Range(key, 5, 1.6f, 2.4f);
                p.size = Range(key, 6, 0.8f, 1.2f);
            }
            MarkDirtyRepaint();
        }

        void Tick()
        {
            var panel = this.panel;
            if (panel == null)
                return;

            var channels = HasChannels(panel);
            EnableInClassList(gpuUssClassName, channels);
            if (channels != m_DrawnWithChannels)
                MarkDirtyRepaint();
            if (!channels)
                return;

            UpdateShaderTime();

            // A runtime panel in Play mode draws every frame; an Editor window, or any panel outside Play mode, only
            // when something in it changed. Its root draws next to nothing, so marking it has it draw the next frame.
            if (panel.contextType == ContextType.Editor || !Application.isPlaying)
                panel.visualTree.MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var size = contentRect.size;
            m_DrawnWithChannels = HasChannels(panel);
            m_DrawnSize = size;
            if (!(size.x >= 4f) || !(size.y >= 4f))
                return;

            var color = resolvedStyle.color;
            var extent = Mathf.Min(size.x * 0.22f, size.y * 0.1f);
            if (m_Shape == null || m_ShapeColor != color || m_ShapeExtent != extent)
                BuildShape(color, extent);

            for (int i = 0; i < m_Count; i++)
            {
                ref var p = ref m_Puffs[i];
                if (m_DrawnWithChannels)
                {
                    // Where it starts, at the bottom; the shader moves it. userData tells the mesh modifier which one it is.
                    var start = new Vector2(p.x * size.x, size.y * k_Bottom);
                    context.DrawVectorImage(m_Shape, start - Vector2.one * extent * p.size, Angle.Degrees(0f), Vector2.one * p.size, i + 1);
                }
                else
                {
                    // Standing still, spread along the way up as the shader would show them at time 0.
                    var life = Mathf.Repeat(p.offset / p.rise, 1f);
                    var position = new Vector2(p.x * size.x + Mathf.Sin(p.offset * 3f) * p.sway * size.x * life, Mathf.Lerp(size.y * k_Bottom, size.y * k_Top, life));
                    var scale = p.size * Mathf.Lerp(0.45f, p.grow, life);
                    context.DrawVectorImage(m_Shape, position - Vector2.one * extent * scale, Angle.Degrees(0f), Vector2.one * scale);
                }
            }
        }

        // The mesh modifier: every vertex of puff i gets puff i's motion (Steam.hlsl reads it).
        void WriteMotion(MeshModificationContext context)
        {
            if (!m_DrawnWithChannels)
                return;

            var element = new Vector4(m_DrawnSize.x, m_DrawnSize.y, m_DrawnSize.y * k_Bottom, m_DrawnSize.y * k_Top);
            foreach (var draw in context.draws)
            {
                var index = draw.userData - 1;
                if (index < 0 || index >= m_Count)
                    continue;

                ref var p = ref m_Puffs[index];
                var start = new Vector4(p.x, p.offset, p.rise, p.sway);
                var motion = new Vector4(p.swayFrequency, p.grow, 0f, 1f);
                var uv1 = draw.GetUv1(allocate: true);
                var uv2 = draw.GetUv2(allocate: true);
                var uv3 = draw.GetUv3(allocate: true);
                for (int v = 0; v < uv1.Length; v++)
                {
                    uv1[v] = start;
                    uv2[v] = motion;
                    uv3[v] = element;
                }
            }
        }

        // A puff of steam: three overlapping lobes, each a radial gradient from the color in its middle to clear at
        // its edge, centered in a clear square (the VectorImage's bounds) so the image's middle is the puff's.
        void BuildShape(Color color, float extent)
        {
            ReleaseShape();
            m_ShapeColor = color;
            m_ShapeExtent = extent;
            using var painter = new Painter2D();
            painter.fillColor = Color.clear;
            painter.BeginPath();
            painter.MoveTo(new Vector2(-extent, -extent));
            painter.LineTo(new Vector2(extent, -extent));
            painter.LineTo(new Vector2(extent, extent));
            painter.LineTo(new Vector2(-extent, extent));
            painter.ClosePath();
            painter.Fill();

            var soft = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(color.a * 0.5f, 0f), new GradientAlphaKey(color.a * 0.3f, 0.45f), new GradientAlphaKey(0f, 1f) },
            };
            foreach (var (x, y, r) in new[] { (-0.3f, 0.1f, 0.6f), (0.28f, 0.04f, 0.64f), (0f, -0.2f, 0.74f) })
            {
                var center = new Vector2(x, y) * extent;
                painter.fillGradient = FillGradient.MakeRadialGradient(soft, center, r * extent, center);
                painter.BeginPath();
                painter.Arc(center, r * extent, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
            }

            m_Shape = ScriptableObject.CreateInstance<VectorImage>();
            m_Shape.hideFlags = HideFlags.HideAndDontSave;
            painter.SaveToVectorImage(m_Shape);
        }

        void ReleaseShape()
        {
            if (m_Shape == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(m_Shape);
            else
                Object.DestroyImmediate(m_Shape);
            m_Shape = null;
        }

        static float Range(int key, int salt, float min, float max)
        {
            unchecked
            {
                var h = (uint)(key * 73856093 ^ salt * 19349663);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return Mathf.Lerp(min, max, (h & 0xFFFFFF) / 16777215f);
            }
        }
    }
}
