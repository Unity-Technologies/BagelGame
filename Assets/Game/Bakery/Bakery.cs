using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    public class Bakery : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Room Measurements")]
        [SerializeField] float m_WestSide;
        [SerializeField] float m_EastSide;
        [SerializeField] float m_SouthSide;
        [SerializeField] float m_NorthSide;
        [SerializeField] float m_Height;

        [Space]
        [Header("Street Measurements")]
        [SerializeField] float m_SidewalkWidth;
        [SerializeField] float m_SidewalkLength;
        [SerializeField] float m_HedgeWidth;
        [SerializeField] float m_HedgeHeight;

        [Space]
        [Header("Counter Measurements")]
        [SerializeField] Rect m_CounterRect;
        [SerializeField] float m_CounterHeight;
        [SerializeField] float m_CounterTopThickness;

        [Space]
        [Header("Blackboard Measurements")]
        [SerializeField] float m_BlackboardWidth;
        [SerializeField] float m_BlackboardHeight;
        [SerializeField] float m_BlackboardThickness;
        [SerializeField] Vector2 m_BlackboardWallXY;

        [Space]
        [Header("Play States")]
        [SerializeField] Transform m_MainMenu;
        [SerializeField] Transform m_BagelSelection;
        [SerializeField] Transform m_Playing;
        [SerializeField] Transform m_GameOver;

        [Space]
        [Header("Assets")]
        [SerializeField] Transform m_EmbeddedMeshOwner;
        [SerializeField] Transform m_BoxPrefab;
        [SerializeField] Transform m_QuadPrefab;
        [SerializeField] Material m_FloorMaterial;
        [SerializeField] Material m_CeilingMaterial;
        [SerializeField] Material m_WallMaterial;
        [SerializeField] Material m_SidewalkMaterial;
        [SerializeField] Material m_HedgeMaterial;
        [SerializeField] Material m_CounterMaterial;
        [SerializeField] Material m_CounterTopMaterial;
        [SerializeField] Material m_BlackboardMaterial;

        [Space]
        [Header("Generated (Do Not Edit)")]
        [SerializeField] Transform m_Floor;
        [SerializeField] Transform m_Ceiling;
        [SerializeField] Transform m_WestWall;
        [SerializeField] Transform m_SouthWall;
        [SerializeField] Transform m_NorthWall;
        [SerializeField] Transform m_Sidewalk;
        [SerializeField] Transform m_Hedge;
        [SerializeField] Transform m_Counter;
        [SerializeField] Transform m_CounterTop;
        [SerializeField] Transform m_Blackboard;

        [Space]
        [SerializeField] float m_Left;
        [SerializeField] float m_Right;
        [SerializeField] float m_Bottom;
        [SerializeField] float m_Top;

        [SerializeField] float m_Width;
        [SerializeField] float m_Depth;
        [SerializeField] Vector2 m_Center;

        [SerializeField] float m_CounterSurface;
        [SerializeField] float m_BlackboardSurface;

        const float k_FloorY = 0.0f;
        const float k_SurfaceOffset = 0.05f;
        const float k_WallThickness = 0.2f;

        void Clear()
        {
            var childrenToDestroy = new List<GameObject>();
            foreach (Transform child in transform)
                childrenToDestroy.Add(child.gameObject);

            foreach (var child in childrenToDestroy)
                DestroyImmediate(child);
        }

        void Build()
        {
            m_Floor = GetOrCreate(m_QuadPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.up), nameof(m_Floor), m_FloorMaterial);
            m_Ceiling = GetOrCreate(m_BoxPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.down), nameof(m_Ceiling), m_CeilingMaterial);

            m_WestWall = GetOrCreate(m_BoxPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.right), nameof(m_WestWall), m_WallMaterial);
            m_SouthWall = GetOrCreate(m_BoxPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.forward), nameof(m_SouthWall), m_WallMaterial);
            m_NorthWall = GetOrCreate(m_BoxPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.back), nameof(m_NorthWall), m_WallMaterial);

            m_Sidewalk = GetOrCreate(m_EmbeddedMeshOwner, Quaternion.FromToRotation(Vector3.back, Vector3.up), nameof(m_Sidewalk), m_SidewalkMaterial);
            m_Hedge = GetOrCreate(m_EmbeddedMeshOwner, Quaternion.FromToRotation(Vector3.back, Vector3.left), nameof(m_Hedge), m_HedgeMaterial);

            m_Counter = GetOrCreate(m_BoxPrefab, Quaternion.identity, nameof(m_Counter), m_CounterMaterial);
            m_CounterTop = GetOrCreate(m_BoxPrefab, Quaternion.identity, nameof(m_CounterTop), m_CounterTopMaterial);

            m_Blackboard = GetOrCreate(m_BoxPrefab, Quaternion.identity, nameof(m_Blackboard), m_BlackboardMaterial);
        }

        public void Rebuild()
        {
            Clear();
            Build();
            UpdateMeshes();
        }

        void UpdateMeshes()
        {
            UpdateMetrics();

            UpdateInside();
            UpdateOutside();

            UpdateCounter();

            UpdateDependents();
        }

        Transform GetOrCreate(Transform prefab, Quaternion rotate, string name, Material material)
        {
            if (prefab == null)
                return null;

            var obj = transform.Find(name);
            if (obj != null)
                return obj;

            obj = Instantiate(prefab, transform);
            obj.name = name;
            obj.localRotation = rotate;

            if (material != null)
                obj.GetComponent<MeshRenderer>().material = material;

            return obj;
        }

        void UpdateTransform(Transform t, float px, float py, float pz, float sx, float sy)
        {
            if (t == null)
                return;

            var p = t.localPosition;
            var s = t.localScale;
            p.x = px;
            p.y = py;
            p.z = pz;
            s.x = sx;
            s.y = sy;
            t.localPosition = p;
            t.localScale = s;
        }

        void UpdateTransform(Transform t, float px, float py, float pz, float sx, float sy, float sz)
        {
            if (t == null)
                return;

            var p = t.localPosition;
            var s = t.localScale;
            p.x = px;
            p.y = py;
            p.z = pz;
            s.x = sx;
            s.y = sy;
            s.z = sz;
            t.localPosition = p;
            t.localScale = s;
        }

        void UpdateMetrics()
        {
            m_Left = -Mathf.Abs(m_WestSide);
            m_Right = Mathf.Abs(m_EastSide);
            m_Bottom = -Mathf.Abs(m_SouthSide);
            m_Top = Mathf.Abs(m_NorthSide);

            m_Width = m_Right - m_Left;
            m_Depth = m_Top - m_Bottom;
            m_Center = new Vector2(
                0.5f * (m_Left + m_Right),
                0.5f * (m_Bottom + m_Top));

            m_CounterSurface = k_FloorY + m_CounterHeight + m_CounterTopThickness + k_SurfaceOffset;
            m_BlackboardSurface = m_Left + m_BlackboardThickness + k_SurfaceOffset;
        }

        void UpdateInside()
        {
            float halfHeight = k_FloorY + (m_Height * 0.5f);

            float wallHeight = m_Height + (k_WallThickness * 0.5f);
            float wallLeft = m_Left - (k_WallThickness * 0.5f);
            float wallBottom = m_Bottom - (k_WallThickness * 0.5f);
            float wallTop = m_Top + (k_WallThickness * 0.5f);

            //                           px           py          pz          sx       sy        sz
            UpdateTransform(m_Floor,     m_Center.x,  k_FloorY,   m_Center.y, m_Width, m_Depth);
            UpdateTransform(m_Ceiling,   m_Center.x,  wallHeight, m_Center.y, m_Width, m_Depth,  k_WallThickness);
            UpdateTransform(m_WestWall,  wallLeft,    halfHeight, m_Center.y, m_Depth, m_Height, k_WallThickness);
            UpdateTransform(m_SouthWall, m_Center.x,  halfHeight, wallBottom, m_Width, m_Height, k_WallThickness);
            UpdateTransform(m_NorthWall, m_Center.x,  halfHeight, wallTop,    m_Width, m_Height, k_WallThickness);

            UpdateTransform(m_Blackboard,
                m_Left + (m_BlackboardThickness * 0.5f),
                m_BlackboardWallXY.y,
                m_BlackboardWallXY.x,
                m_BlackboardThickness,
                m_BlackboardHeight,
                m_BlackboardWidth);
        }

        void UpdateOutside()
        {
            if (m_Sidewalk)
            {
                var mesh = ProceduralMesh.BuildQuadGrid(
                    new Vector2(m_SidewalkWidth, m_SidewalkLength),
                    segments: new Vector2Int(1, 5),
                    tileMeters: 1.0f,
                    uvOffset: Vector2.zero,
                    plane: ProcPlane.XZ,
                    twoSided: false,
                    name: "Sidewalk_GeneratedMesh");

                ProceduralMesh.Apply(m_Sidewalk.gameObject, mesh, m_SidewalkMaterial);
                m_Sidewalk.localPosition = new Vector3(m_Right + (m_SidewalkWidth * 0.5f), 0f, m_Center.y);
                m_Sidewalk.localRotation = Quaternion.identity;
                m_Sidewalk.localScale = Vector3.one;
            }

            if (m_Hedge)
            {
                var mesh = ProceduralMesh.BuildQuadGrid(
                    new Vector2(m_SidewalkLength, m_HedgeHeight),
                    segments: new Vector2Int(5, 1),
                    tileMeters: 1.0f,
                    uvOffset: Vector2.zero,
                    plane: ProcPlane.YZ,
                    twoSided: true,
                    name: "Hedge_GeneratedMesh");
                ProceduralMesh.Apply(m_Hedge.gameObject, mesh, m_HedgeMaterial);
                m_Hedge.localPosition = new Vector3(m_Right + m_SidewalkWidth, k_FloorY + (m_HedgeHeight * 0.5f), m_Center.y);
                m_Hedge.localRotation = Quaternion.identity;
                m_Hedge.localScale = Vector3.one;
            }
        }

        void UpdateCounter()
        {
            UpdateTransform(m_Counter,
                m_CounterRect.x,
                k_FloorY + (m_CounterHeight * 0.5f),
                m_CounterRect.y,
                m_CounterRect.width,
                m_CounterHeight,
                m_CounterRect.height);
            UpdateTransform(m_CounterTop,
                m_CounterRect.x,
                k_FloorY + m_CounterHeight + (m_CounterTopThickness * 0.5f),
                m_CounterRect.y,
                m_CounterRect.width,
                m_CounterTopThickness,
                m_CounterRect.height);
        }

        void UpdateDependents()
        {
            if (m_MainMenu)
            {
                var p = m_MainMenu.localPosition;
                p.x = m_BlackboardSurface;
                m_MainMenu.localPosition = p;
            }

            if (m_BagelSelection)
            {
                var p = m_BagelSelection.localPosition;
                p.y = m_CounterSurface;
                m_BagelSelection.localPosition = p;
            }

            if (m_Playing)
            {
                var p = m_Playing.localPosition;
                p.y = m_CounterSurface;
                m_Playing.localPosition = p;
            }

            if (m_GameOver)
            {
                var p = m_GameOver.localPosition;
                p.y = m_CounterSurface;
                m_GameOver.localPosition = p;
            }
        }
#endif // UNITY_EDITOR
    }
}
