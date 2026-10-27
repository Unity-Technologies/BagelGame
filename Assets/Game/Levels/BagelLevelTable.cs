using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // The counter, set for the chosen level: the gingham cloth, the course and the breakfast things on it (from the
    // level's table of pieces, BagelLevelLibrary prefabs), and the toaster at the end, with the room as long as the
    // counter needs (Bakery). Choosing another level shoves everything off the table (TableClearing), and the toaster
    // slides to the new end with the room stretching or shrinking behind it, the new level coming up out of the table
    // as the cloth gets to it. Changing this one changes only what differs: what the level no longer has sinks into
    // the table, and what's new comes up out of it. A new game shoves the table off once the level's all there. In the
    // Editor, out of play, the chosen level is only shown, never saved with the scene, and the room is left as it is.
    [ExecuteAlways]
    [RequireComponent(typeof(TableClearing))]
    public class BagelLevelTable : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] Bakery m_Bakery;
        [Tooltip("The toaster, on its stand: at the end of every level.")]
        [SerializeField] Transform m_Toaster;
        [Tooltip("The gingham cloth, as long as the level: its three materials are the checks' three shades.")]
        [SerializeField] MeshFilter m_Cloth;
        [SerializeField] BoxCollider m_ClothCollider;

        [Header("Room")]
        [Tooltip("How far past the toaster the counter goes, and the room (meters).")]
        [SerializeField] float m_CounterBeyond = 3.14f;
        [SerializeField] float m_RoomBeyond = 13.5f;

        [Header("Changing Levels")]
        [Tooltip("How long the toaster takes to slide to the new end, the room with it (seconds).")]
        [SerializeField] float m_StretchTime = 1.6f;
        [Tooltip("How far below the table things come up from and go down to, and how long they take.")]
        [SerializeField] float m_RiseDepth = 1.4f;
        [SerializeField] float m_RiseTime = 0.7f;
        [SerializeField] float m_SinkTime = 0.45f;

        // A piece on the table, as the level has it, and what's there for it.
        class Placed
        {
            public BagelLevelPiece piece;
            public GameObject instance;
        }

        PlayManagerState m_State;
        TableClearing m_Clearing;
        BagelLevel m_Level;
        int m_LevelVersion = -1;
        readonly List<Placed> m_Placed = new();
        // Waiting for the cloth to get to them, nearest first.
        readonly List<BagelLevelPiece> m_Waiting = new();
        readonly List<GameObject> m_Sinking = new();
        float m_Length = -1f;
        float m_TargetLength;
        float m_StretchFrom;
        float m_StretchTime01 = 1f;
        float m_ComeAfter;
        Coroutine m_Changing;
        Mesh m_ClothMesh;
        readonly List<Vector3> m_ClothVertices = new();
        readonly List<Vector3> m_ClothNormals = new();
        readonly List<Vector2> m_ClothUVs = new();
        readonly List<int>[] m_ClothChecks = { new(), new(), new() };
        int m_ClothRows = -1;

        // The level on the table (or on its way), and how long the table is now, as it slides.
        public BagelLevel level => m_Level;
        public float length => m_Length;
        public bool changing => m_Changing != null;
        public Transform toaster => m_Toaster;
        public IEnumerable<GameObject> pieces
        {
            get
            {
                foreach (var placed in m_Placed)
                {
                    if (placed.instance != null)
                        yield return placed.instance;
                }
            }
        }
        public int pieceCount => m_Placed.Count + m_Waiting.Count;

        public BagelLevelLibrary library => m_State != null ? m_State.levelLibrary : null;

        void OnEnable()
        {
            m_Clearing = GetComponent<TableClearing>();
            m_State = m_PlayManager != null ? m_PlayManager.state : null;
            if (m_State == null)
                return;
            m_State.onSetLevel += State_OnSetLevel;
            m_State.onStateChange += State_OnStateChange;
            Show(m_State.selectedLevel, false);
        }

        void OnDisable()
        {
            if (m_State != null)
            {
                m_State.onSetLevel -= State_OnSetLevel;
                m_State.onStateChange -= State_OnStateChange;
            }
            Watch(null);
            m_Changing = null;
            Clear();
        }

        void OnDestroy()
        {
            if (m_ClothMesh != null)
                DestroyImmediate(m_ClothMesh);
        }

        void State_OnSetLevel(object sender, BagelLevel level) => Show(level, Application.isPlaying && m_State.isLevelSelection);

        void State_OnStateChange(object sender, PlayManagerState.State state)
        {
            if (state != PlayManagerState.State.Playing || !Application.isPlaying)
                return;

            // The whole level's there for the new game, before the table's shoved off and set again.
            Finish();
            if (m_Clearing != null)
                m_Clearing.ClearForANewGame();
        }

        void Level_OnChanged(BagelLevel level) => Show(level, Application.isPlaying);

        void Watch(BagelLevel level)
        {
            if (m_Level == level)
                return;
            if (m_Level != null)
                m_Level.changed -= Level_OnChanged;
            m_Level = level;
            if (m_Level != null)
                m_Level.changed += Level_OnChanged;
        }

        // The table as this level has it: changed bit by bit as it's watched, or all at once.
        public void Show(BagelLevel level, bool animate)
        {
            var another = level != m_Level;
            Watch(level);
            m_LevelVersion = level != null ? level.version : -1;
            var wanted = new List<BagelLevelPiece>();
            if (level != null)
                wanted.AddRange(level.pieces);

            // Another level: everything's shoved off the table, and the new level's set from scratch.
            m_Waiting.Clear();
            var going = false;
            if (animate && another && m_Placed.Count > 0 && Application.isPlaying && m_Clearing != null)
            {
                var pieces = new List<GameObject>(this.pieces);
                m_Placed.Clear();
                FinishSinking();
                m_Clearing.ClearForAnotherLevel(pieces);
                foreach (var piece in pieces)
                    Destroy(piece);
                going = true;
            }

            // What's there already stays; what isn't wanted any more goes.
            for (int i = m_Placed.Count - 1; i >= 0; i--)
            {
                var placed = m_Placed[i];
                var index = placed.instance != null ? wanted.FindIndex(piece => piece.Equals(placed.piece)) : -1;
                if (index >= 0)
                {
                    wanted.RemoveAt(index);
                    continue;
                }
                m_Placed.RemoveAt(i);
                Remove(placed.instance, animate);
                going = true;
            }

            m_TargetLength = level != null ? level.length : BagelLevel.newLength;
            if (!animate || m_Length < 0f)
            {
                foreach (var piece in wanted)
                    Place(piece, false);
                SetLength(m_TargetLength);
                m_StretchTime01 = 1f;
                if (m_Changing != null)
                    StopCoroutine(m_Changing);
                m_Changing = null;
                FinishSinking();
                return;
            }

            // What goes, goes first; then the toaster slides, and what's new comes up as the cloth gets to it.
            wanted.Sort((a, b) => a.z.CompareTo(b.z));
            m_Waiting.AddRange(wanted);
            m_StretchFrom = m_Length;
            m_StretchTime01 = Mathf.Approximately(m_Length, m_TargetLength) ? 1f : 0f;
            m_ComeAfter = Time.time + (going ? m_SinkTime * 0.6f : 0f);
            m_Changing ??= StartCoroutine(Change());
        }

        // Done now: the toaster at the end, the room as long as it'll be, everything up.
        public void Finish()
        {
            if (m_Changing == null && m_Waiting.Count == 0)
                return;
            if (m_Changing != null)
                StopCoroutine(m_Changing);
            m_Changing = null;
            foreach (var piece in m_Waiting)
                Place(piece, false);
            m_Waiting.Clear();
            SetLength(m_TargetLength);
            m_StretchTime01 = 1f;
            FinishSinking();
        }

        IEnumerator Change()
        {
            while (m_StretchTime01 < 1f || m_Waiting.Count > 0)
            {
                if (Time.time < m_ComeAfter)
                {
                    yield return null;
                    continue;
                }
                if (m_StretchTime01 < 1f)
                {
                    m_StretchTime01 = Mathf.Min(1f, m_StretchTime01 + Time.deltaTime / m_StretchTime);
                    var t = m_StretchTime01;
                    SetLength(Mathf.Lerp(m_StretchFrom, m_TargetLength, t * t * (3f - 2f * t)));
                }
                var reach = m_Length + BagelLevel.clothBeyond;
                while (m_Waiting.Count > 0 && m_Waiting[0].z <= reach)
                {
                    Place(m_Waiting[0], true);
                    m_Waiting.RemoveAt(0);
                }
                yield return null;
            }
            m_Changing = null;
        }

        void Place(BagelLevelPiece piece, bool rise)
        {
            var entry = library != null ? library.Find(piece.kind) : null;
            if (entry == null || entry.prefab == null)
                return;

            // Made where it goes: a body that moves smoothly (interpolated) would stay where it was made.
            var rotation = Quaternion.Euler(0f, piece.angle, 0f);
            var instance = Instantiate(entry.prefab, transform.TransformPoint(piece.x, 0f, piece.z), transform.rotation * rotation, transform);
            instance.name = entry.prefab.name;
            if (!Application.isPlaying)
                SetHideFlags(instance, HideFlags.DontSave);
            m_Placed.Add(new Placed { piece = piece, instance = instance });
            if (!rise)
                return;

            // What's still flying off the table goes through it.
            if (m_Clearing != null)
                m_Clearing.IgnoreLeftovers(instance);

            var items = instance.GetComponentsInChildren<TableItem>();
            if (items.Length > 0)
            {
                foreach (var item in items)
                    item.Restock(m_RiseDepth, m_RiseTime);
            }
            else
            {
                StartCoroutine(Move(instance.transform, -m_RiseDepth, 0f, m_RiseTime, true));
            }
        }

        void Remove(GameObject instance, bool sink)
        {
            if (instance == null)
                return;
            if (!sink || !Application.isPlaying)
            {
                DestroyImmediate(instance);
                return;
            }

            var items = instance.GetComponentsInChildren<TableItem>();
            if (items.Length > 0)
            {
                foreach (var item in items)
                    item.Sink(m_RiseDepth, m_SinkTime);
            }
            else
            {
                StartCoroutine(Move(instance.transform, instance.transform.localPosition.y, -m_RiseDepth, m_SinkTime, false));
            }
            m_Sinking.Add(instance);
            StartCoroutine(Gone(instance, m_SinkTime));
        }

        IEnumerator Gone(GameObject instance, float after)
        {
            yield return new WaitForSeconds(after);
            m_Sinking.Remove(instance);
            if (instance != null)
                Destroy(instance);
        }

        void FinishSinking()
        {
            foreach (var instance in m_Sinking)
            {
                if (instance != null)
                    DestroyImmediate(instance);
            }
            m_Sinking.Clear();
        }

        // A course piece (no table things in it, nothing loose): moved up or down by its root.
        static IEnumerator Move(Transform piece, float fromY, float toY, float duration, bool up)
        {
            for (var time = 0f; time < duration && piece != null; time += Time.deltaTime)
            {
                var t = time / duration;
                var eased = up ? t * t * (3f - 2f * t) : t * t;
                var position = piece.localPosition;
                position.y = Mathf.Lerp(fromY, toY, eased);
                piece.localPosition = position;
                yield return null;
            }
            if (piece == null)
                yield break;
            var end = piece.localPosition;
            end.y = toY;
            piece.localPosition = end;
        }

        void Clear()
        {
            foreach (var placed in m_Placed)
            {
                if (placed.instance != null)
                    DestroyImmediate(placed.instance);
            }
            m_Placed.Clear();
            m_Waiting.Clear();
            foreach (var instance in m_Sinking)
            {
                if (instance != null)
                    DestroyImmediate(instance);
            }
            m_Sinking.Clear();
            m_Length = -1f;
        }

        // The toaster this far from the start, the cloth to just past it, and (in play) the room as long as it needs.
        void SetLength(float length)
        {
            m_Length = length;
            if (m_Toaster != null)
            {
                var position = m_Toaster.localPosition;
                position.z = length;
                m_Toaster.localPosition = position;
            }
            UpdateCloth(length);
            if (Application.isPlaying && m_Bakery != null)
                m_Bakery.Stretch(transform.TransformPoint(0f, 0f, length + m_RoomBeyond).z, transform.TransformPoint(0f, 0f, length + m_CounterBeyond).z);
        }

        // Half-meter checks from the cloth's start, the last row cut short, on three submeshes: none, one or two of
        // the gingham's stripes crossing.
        void UpdateCloth(float length)
        {
            var start = BagelLevel.clothStart;
            var end = length + BagelLevel.clothBeyond;
            var cell = BagelLevel.cellSize;
            var columns = Mathf.RoundToInt(BagelLevel.halfWidth * 2f / cell);
            var rows = Mathf.Max(1, Mathf.CeilToInt((end - start) / cell - 0.001f));

            if (m_ClothCollider != null)
            {
                m_ClothCollider.center = new Vector3(0f, -0.05f, (start + end) * 0.5f);
                m_ClothCollider.size = new Vector3(BagelLevel.halfWidth * 2f, 0.1f, end - start);
            }
            if (m_Cloth == null)
                return;

            if (m_ClothMesh == null)
            {
                m_ClothMesh = new Mesh { name = "Cloth", hideFlags = HideFlags.DontSave };
                m_ClothMesh.MarkDynamic();
                m_ClothRows = -1;
            }
            if (m_Cloth.sharedMesh != m_ClothMesh)
                m_Cloth.sharedMesh = m_ClothMesh;

            m_ClothVertices.Clear();
            m_ClothNormals.Clear();
            m_ClothUVs.Clear();
            for (int row = 0; row <= rows; row++)
            {
                var z = Mathf.Min(start + row * cell, end);
                for (int column = 0; column <= columns; column++)
                {
                    var x = -BagelLevel.halfWidth + column * cell;
                    m_ClothVertices.Add(new Vector3(x, 0f, z));
                    m_ClothNormals.Add(Vector3.up);
                    m_ClothUVs.Add(new Vector2(x / cell, z / cell));
                }
            }

            var rebuild = rows != m_ClothRows;
            if (rebuild)
            {
                m_ClothMesh.Clear();
                foreach (var checks in m_ClothChecks)
                    checks.Clear();
                var stride = columns + 1;
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        var i = row * stride + column;
                        var checks = m_ClothChecks[(column % 2) + (row % 2)];
                        checks.Add(i);
                        checks.Add(i + stride);
                        checks.Add(i + stride + 1);
                        checks.Add(i);
                        checks.Add(i + stride + 1);
                        checks.Add(i + 1);
                    }
                }
            }
            m_ClothMesh.SetVertices(m_ClothVertices);
            m_ClothMesh.SetNormals(m_ClothNormals);
            m_ClothMesh.SetUVs(0, m_ClothUVs);
            if (rebuild)
            {
                m_ClothMesh.subMeshCount = m_ClothChecks.Length;
                for (int i = 0; i < m_ClothChecks.Length; i++)
                    m_ClothMesh.SetTriangles(m_ClothChecks[i], i, false);
                m_ClothRows = rows;
            }
            m_ClothMesh.RecalculateBounds();
        }

        void Update()
        {
            // In the Editor, a level changed in its inspector or the Bagel Levels window shows here too.
            if (!Application.isPlaying && m_Level != null && m_Level.version != m_LevelVersion)
                Show(m_Level, false);
        }

        static void SetHideFlags(GameObject gameObject, HideFlags flags)
        {
            gameObject.hideFlags = flags;
            foreach (Transform child in gameObject.transform)
                SetHideFlags(child.gameObject, flags);
        }
    }
}
