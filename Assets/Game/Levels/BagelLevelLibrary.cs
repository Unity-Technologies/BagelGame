using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    // Everything a level can put on the counter: the course's ramps, platforms and spilled milk, and the breakfast
    // things (TableItem) the bagel knocks about or rolls into. Each is a prefab whose root is the middle of its
    // footprint, on the counter, unturned; a level places it by id.
    [CreateAssetMenu(fileName = "BagelLevelLibrary", menuName = "Bagel/Bagel Level Library")]
    public class BagelLevelLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string nameKey;
            public string displayName;
            public GameObject prefab;
            [Tooltip("Its footprint on the counter, unturned (meters, across and along).")]
            public Vector2 size = Vector2.one;
            [Tooltip("Round, like a jar seen from above, or square.")]
            public bool round;
            [Tooltip("Its color on the maps of the levels.")]
            public Color color = Color.white;
            [Tooltip("Part of the course (it stays when the table is cleared), or a breakfast thing on the table.")]
            public bool course;

            // Half its footprint turned to an angle, as a box along the counter's sides.
            public Vector2 HalfExtents(float angle)
            {
                var radians = angle * Mathf.Deg2Rad;
                var cos = Mathf.Abs(Mathf.Cos(radians));
                var sin = Mathf.Abs(Mathf.Sin(radians));
                var half = size * 0.5f;
                if (round)
                {
                    // An ellipse's box, turned.
                    return new Vector2(Mathf.Sqrt(half.x * half.x * cos * cos + half.y * half.y * sin * sin),
                        Mathf.Sqrt(half.x * half.x * sin * sin + half.y * half.y * cos * cos));
                }
                return new Vector2(half.x * cos + half.y * sin, half.x * sin + half.y * cos);
            }

            // Whether a point on the counter is on this piece's footprint, placed as it is.
            public bool Contains(BagelLevelPiece piece, Vector2 point)
            {
                var local = Turn(point - piece.position, -piece.angle);
                var half = size * 0.5f;
                if (round)
                    return half.x > 0f && half.y > 0f && (local.x * local.x) / (half.x * half.x) + (local.y * local.y) / (half.y * half.y) <= 1f;
                return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
            }
        }

        [SerializeField] List<Entry> m_Entries = new();

        Dictionary<string, Entry> m_ById;

        public IReadOnlyList<Entry> entries => m_Entries;

        public Entry Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            if (m_ById == null)
            {
                m_ById = new Dictionary<string, Entry>();
                foreach (var entry in m_Entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.id))
                        m_ById[entry.id] = entry;
                }
            }
            return m_ById.TryGetValue(id, out var found) ? found : null;
        }

        // A point on the counter turned round the middle, clockwise as seen from above (as a piece's angle turns it).
        public static Vector2 Turn(Vector2 point, float angle)
        {
            var radians = angle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            // Clockwise from above, with x to the right and z ahead: Unity's turn about y.
            return new Vector2(point.x * cos + point.y * sin, -point.x * sin + point.y * cos);
        }

        void OnValidate() => m_ById = null;
    }
}
