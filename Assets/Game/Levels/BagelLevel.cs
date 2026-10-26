using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

namespace Bagel
{
    // One thing on the counter: what it is (a BagelLevelLibrary id), where its middle is, and how it's turned. Meters
    // from where the bagel starts: x across the counter (to the bagel's right), z along it (toward the toaster);
    // degrees round, clockwise as seen from above.
    [Serializable]
    public struct BagelLevelPiece : IEquatable<BagelLevelPiece>
    {
        public string kind;
        public float x;
        public float z;
        public float angle;

        public BagelLevelPiece(string kind, float x, float z, float angle = 0f)
        {
            this.kind = kind;
            this.x = x;
            this.z = z;
            this.angle = angle;
        }

        public Vector2 position => new(x, z);

        public bool Equals(BagelLevelPiece other) => kind == other.kind &&
            Mathf.Abs(x - other.x) < 0.001f && Mathf.Abs(z - other.z) < 0.001f && Mathf.Abs(Mathf.DeltaAngle(angle, other.angle)) < 0.01f;

        public override bool Equals(object obj) => obj is BagelLevelPiece other && Equals(other);

        public override int GetHashCode() => (kind ?? "").GetHashCode();

        public override string ToString() => $"{kind} ({x:0.##}, {z:0.##}) {angle:0.#} deg";
    }

    // A table set for the bagel: a simple table of what stands on the long counter and where, from the start by the
    // lazy Susan to the toaster at the end, by the television. The toaster's always there, at the level's length from
    // the start; the gingham cloth runs from a little behind the start to a little past the toaster, and the room is
    // as long as the counter needs. The game's own levels are assets, edited in the Bagel Levels window; the player's
    // are made on the payment tablet and kept in the save.
    [CreateAssetMenu(fileName = "BagelLevel", menuName = "Bagel/Bagel Level")]
    public class BagelLevel : ScriptableObject
    {
        // The counter's grid: the gingham's checks, half a meter, across the cloth's width.
        public const float cellSize = 0.5f;
        public const float halfWidth = 5.5f;
        // Where the cloth starts, behind the bagel (a baguette can lie there), and how far it goes past the toaster.
        public const float clothStart = -8f;
        public const float clothBeyond = 2.5f;
        // Nothing stands where the bagel starts, nor on the toaster's stand (half across and along, round the end).
        public const float startRadius = 1.2f;
        public static readonly Vector2 toasterHalfSize = new(1.6f, 1.6f);
        // How long a level can be, and the steps it's made longer or shorter in. The room's no shorter than for the
        // tutorial: the blackboard runs along the wall nearly to the corner, with the bagel's picture just past it.
        public const float minLength = 27.5f;
        public const float maxLength = 80f;
        public const float lengthStep = 2.5f;
        public const float newLength = 30f;

        [Tooltip("How the game knows it, in the save (the player's levels are \"custom-\" and a number).")]
        [SerializeField] string m_Id;
        [Tooltip("Its name, from the string table; the player's levels have their own.")]
        [SerializeField] string m_NameKey;
        [SerializeField] string m_DisplayName;
        [Tooltip("From the start to the toaster (meters).")]
        [SerializeField] float m_Length = newLength;
        [SerializeField] List<BagelLevelPiece> m_Pieces = new();

        bool m_Custom;
        int m_Version;

        public string id => m_Id;
        public string nameKey => m_NameKey;
        // Made in the game, by the player: only these are edited on the tablet.
        public bool custom => m_Custom;
        // Goes up with every change, so what shows the level knows to show it again.
        public int version => m_Version;
        public IReadOnlyList<BagelLevelPiece> pieces => m_Pieces;
        public int pieceCount => m_Pieces.Count;

        [CreateProperty]
        public string displayName
        {
            get => m_DisplayName;
            set
            {
                if (m_DisplayName == value)
                    return;
                m_DisplayName = value;
                Changed();
            }
        }

        [CreateProperty]
        public float length
        {
            get => m_Length;
            set
            {
                value = ClampLength(value);
                if (Mathf.Approximately(m_Length, value))
                    return;
                m_Length = value;
                // What would be off the cloth or on the toaster now goes.
                m_Pieces.RemoveAll(piece => piece.z > m_Length + clothBeyond || OnTheToaster(piece.position, Vector2.zero));
                Changed();
            }
        }

        public event Action<BagelLevel> changed;

        public static float ClampLength(float value) => Mathf.Clamp(value, minLength, maxLength);

        // The nearest point on the grid, x and z, from a point anywhere.
        public static Vector2 Snap(Vector2 position) =>
            new(Mathf.Round(position.x / cellSize) * cellSize, Mathf.Round(position.y / cellSize) * cellSize);

        // Where a piece's middle can be, for it to be on the cloth.
        public Rect PlaceableArea(Vector2 halfSize) =>
            Rect.MinMaxRect(-halfWidth + halfSize.x, clothStart + halfSize.y, halfWidth - halfSize.x, m_Length + clothBeyond - halfSize.y);

        // Whether a piece this big (half its turned footprint) can stand there: on the cloth, not on the bagel's
        // start, not on the toaster.
        public bool CanPlace(Vector2 position, Vector2 halfSize)
        {
            var area = PlaceableArea(halfSize);
            const float slack = 0.001f;
            if (position.x < area.xMin - slack || position.x > area.xMax + slack || position.y < area.yMin - slack || position.y > area.yMax + slack)
                return false;
            if (OnTheToaster(position, halfSize))
                return false;
            var near = new Vector2(Mathf.Clamp(0f, position.x - halfSize.x, position.x + halfSize.x), Mathf.Clamp(0f, position.y - halfSize.y, position.y + halfSize.y));
            return near.magnitude >= startRadius;
        }

        bool OnTheToaster(Vector2 position, Vector2 halfSize) =>
            Mathf.Abs(position.x) < toasterHalfSize.x + halfSize.x && Mathf.Abs(position.y - m_Length) < toasterHalfSize.y + halfSize.y;

        public int Add(BagelLevelPiece piece)
        {
            m_Pieces.Add(piece);
            Changed();
            return m_Pieces.Count - 1;
        }

        public void Set(int index, BagelLevelPiece piece)
        {
            if (index < 0 || index >= m_Pieces.Count || m_Pieces[index].Equals(piece))
                return;
            m_Pieces[index] = piece;
            Changed();
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_Pieces.Count)
                return;
            m_Pieces.RemoveAt(index);
            Changed();
        }

        public void SetPieces(IEnumerable<BagelLevelPiece> pieces)
        {
            m_Pieces.Clear();
            m_Pieces.AddRange(pieces);
            Changed();
        }

        // The piece whose footprint is under a point, the one on top (the last placed) first; -1 for none.
        public int PieceAt(Vector2 position, BagelLevelLibrary library)
        {
            for (int i = m_Pieces.Count - 1; i >= 0; i--)
            {
                var entry = library != null ? library.Find(m_Pieces[i].kind) : null;
                if (entry != null && entry.Contains(m_Pieces[i], position))
                    return i;
            }
            return -1;
        }

        void Changed()
        {
            m_Version++;
            changed?.Invoke(this);
        }

        void OnValidate()
        {
            m_Length = ClampLength(m_Length);
            Changed();
        }

        // A level of the player's own, empty but for the toaster.
        public static BagelLevel CreateCustom(string id, string name, float length = newLength)
        {
            var level = CreateInstance<BagelLevel>();
            level.name = id;
            level.hideFlags = HideFlags.DontSave;
            level.m_Id = id;
            level.m_DisplayName = name;
            level.m_Length = ClampLength(length);
            level.m_Custom = true;
            return level;
        }

        // What the save keeps of the player's own level, for JsonUtility.
        [Serializable]
        public class Saved
        {
            public string id;
            public string name;
            public float length;
            public List<BagelLevelPiece> pieces = new();
        }

        public Saved ToSaved() => new() { id = m_Id, name = m_DisplayName, length = m_Length, pieces = new List<BagelLevelPiece>(m_Pieces) };

        public static BagelLevel FromSaved(Saved saved)
        {
            if (saved == null || string.IsNullOrEmpty(saved.id))
                return null;
            var level = CreateCustom(saved.id, saved.name, saved.length);
            if (saved.pieces != null)
                level.m_Pieces.AddRange(saved.pieces);
            return level;
        }
    }
}
