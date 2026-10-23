using System;
using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    public class BagelSelectionRoom : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;
        [SerializeField] BagelTypeCollection m_BagelTypeCollection;
        [SerializeField] float m_PodiumXOffset = 3.0f;

        public PlayManager playManager => m_PlayManager;
        public BagelTypeCollection bagelTypeCollection => m_BagelTypeCollection;
        public float podiumXOffset => m_PodiumXOffset;
        public float podiumXMidpointShift => 0.5f * (m_BagelTypeCollection.collection.Count - 1) * podiumXOffset;
        public event EventHandler onBagelTypeCollectionChange;

        public int BagelTypeCount => m_BagelTypeCollection.collection.Count;

        void OnEnable()
        {
            InitBagelCollection();
        }

        public float GetOffsetFromIndex(int index)
        {
            return ((float)index * podiumXOffset) - podiumXMidpointShift;
        }

        public void InitBagelCollection()
        {
            onBagelTypeCollectionChange?.Invoke(this, null);
        }
    }
}
