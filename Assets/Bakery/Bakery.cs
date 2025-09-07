using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    public class Bakery : MonoBehaviour
    {
        [SerializeField] float m_WestSide;
        [SerializeField] float m_EastSide;
        [SerializeField] float m_SouthSide;
        [SerializeField] float m_NorthSide;
        [SerializeField] float m_Height;

        [Space]
        [SerializeField] float m_SidewalkWidth;
        [SerializeField] float m_SidewalkLength;
        [SerializeField] float m_HedgeWidth;
        [SerializeField] float m_HedgeHeight;

        [Space]
        [SerializeField] Rect m_CounterRect;
        [SerializeField] float m_CounterHeight;

        [Space]
        [SerializeField] Transform m_Blackboard;

        [Space]
        [SerializeField] Transform m_BoxPrefab;
        [SerializeField] Transform m_QuadPrefab;
        [SerializeField] Material m_FloorMaterial;
        [SerializeField] Material m_CeilingMaterial;
        [SerializeField] Material m_WallMaterial;
        [SerializeField] Material m_SidewalkMaterial;
        [SerializeField] Material m_HedgeMaterial;
        [SerializeField] Material m_CounterMaterial;

        [Space]
        [SerializeField] Transform m_Floor;
        [SerializeField] Transform m_Ceiling;
        [SerializeField] Transform m_WestWall;
        [SerializeField] Transform m_SouthWall;
        [SerializeField] Transform m_NorthWall;
        [SerializeField] Transform m_Sidewalk;
        [SerializeField] Transform m_Hedge;
        [SerializeField] Transform m_Counter;

        [Space]
        [SerializeField] float m_Left;
        [SerializeField] float m_Right;
        [SerializeField] float m_Bottom;
        [SerializeField] float m_Top;

        [SerializeField] float m_Width;
        [SerializeField] float m_Depth;
        [SerializeField] Vector2 m_Center;

        const float k_FloorY = 0.0f;
        const float k_SurfaceOffset = 0.5f;

        void OnEnable() => Build();
        void Reset() => Build();

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
            m_Ceiling = GetOrCreate(m_QuadPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.down), nameof(m_Ceiling), m_CeilingMaterial);

            m_WestWall = GetOrCreate(m_QuadPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.right), nameof(m_WestWall), m_WallMaterial);
            m_SouthWall = GetOrCreate(m_QuadPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.forward), nameof(m_SouthWall), m_WallMaterial);
            m_NorthWall = GetOrCreate(m_QuadPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.back), nameof(m_NorthWall), m_WallMaterial);

            m_Sidewalk = GetOrCreate(m_QuadPrefab, Quaternion.FromToRotation(Vector3.back, Vector3.up), nameof(m_Sidewalk), m_SidewalkMaterial);
            m_Hedge = GetOrCreate(m_BoxPrefab, Quaternion.identity, nameof(m_Hedge), m_HedgeMaterial);

            m_Counter = GetOrCreate(m_BoxPrefab, Quaternion.identity, nameof(m_Counter), m_CounterMaterial);
        }

        public void Rebuild()
        {
            Clear();
            Build();
        }

        void Update()
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
        }

        void UpdateInside()
        {
            float halfHeight = m_Height * 0.5f;

            if (m_Floor)
            {
                var p = m_Floor.localPosition;
                var s = m_Floor.localScale;
                p.x = m_Center.x;
                p.y = k_FloorY;
                p.z = m_Center.y;
                s.x = m_Width;
                s.y = m_Depth;
                m_Floor.localPosition = p;
                m_Floor.localScale = s;
            }

            if (m_Ceiling)
            {
                var p = m_Ceiling.localPosition;
                var s = m_Ceiling.localScale;
                p.x = m_Center.x;
                p.y = m_Height;
                p.z = m_Center.y;
                s.x = m_Width;
                s.y = m_Depth;
                m_Ceiling.localPosition = p;
                m_Ceiling.localScale = s;
            }

            if (m_WestWall)
            {
                var p = m_WestWall.localPosition;
                var s = m_WestWall.localScale;
                p.x = m_Left;
                p.y = k_FloorY + halfHeight;
                p.z = m_Center.y;
                s.x = m_Depth;
                s.y = m_Height;
                m_WestWall.localPosition = p;
                m_WestWall.localScale = s;
            }

            if (m_SouthWall)
            {
                var p = m_SouthWall.localPosition;
                var s = m_SouthWall.localScale;
                p.x = m_Center.x;
                p.y = k_FloorY + halfHeight;
                p.z = m_Bottom;
                s.x = m_Width;
                s.y = m_Height;
                m_SouthWall.localPosition = p;
                m_SouthWall.localScale = s;
            }

            if (m_NorthWall)
            {
                var p = m_NorthWall.localPosition;
                var s = m_NorthWall.localScale;
                p.x = m_Center.x;
                p.y = k_FloorY + halfHeight;
                p.z = m_Top;
                s.x = m_Width;
                s.y = m_Height;
                m_NorthWall.localPosition = p;
                m_NorthWall.localScale = s;
            }
        }

        void UpdateOutside()
        {
            if (m_Sidewalk)
            {
                var p = m_Sidewalk.localPosition;
                var s = m_Sidewalk.localScale;
                p.x = m_Right + (m_SidewalkWidth * 0.5f);
                p.y = k_FloorY;
                p.z = m_Center.y;
                s.x = m_SidewalkWidth;
                s.y = m_SidewalkLength;
                m_Sidewalk.localPosition = p;
                m_Sidewalk.localScale = s;
            }

            if (m_Hedge)
            {
                var p = m_Hedge.localPosition;
                var s = m_Hedge.localScale;
                p.x = m_Right + m_SidewalkWidth + (m_HedgeWidth * 0.5f);
                p.y = k_FloorY + (m_HedgeHeight * 0.5f);
                p.z = m_Center.y;
                s.x = m_HedgeWidth;
                s.y = m_HedgeHeight;
                s.z = m_SidewalkLength;
                m_Hedge.localPosition = p;
                m_Hedge.localScale = s;
            }
        }

        void UpdateCounter()
        {
            if (m_Counter)
            {
                var p = m_Counter.localPosition;
                var s = m_Counter.localScale;
                p.x = m_CounterRect.x;
                p.y = k_FloorY + (m_CounterHeight * 0.5f);
                p.z = m_CounterRect.y;
                s.x = m_CounterRect.width;
                s.y = m_CounterHeight;
                s.z = m_CounterRect.height;
                m_Counter.localPosition = p;
                m_Counter.localScale = s;
            }
        }

        void UpdateDependents()
        {
            if (m_Blackboard)
            {
                var p = m_Blackboard.localPosition;
                p.x = m_Left + k_SurfaceOffset;
                m_Blackboard.localPosition = p;
            }
        }
    }
}
