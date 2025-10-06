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
        public event EventHandler<int> onBagelTypeChange;

        int m_SelectedBagelIndex;
        public int BagelTypeCount => m_BagelTypeCollection.collection.Count;
        public int SelectedBagelIndex => m_SelectedBagelIndex;

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
            m_SelectedBagelIndex = 0;

            onBagelTypeCollectionChange?.Invoke(this, null);
            onBagelTypeChange?.Invoke(this, m_SelectedBagelIndex);

            m_PlayManager.state.SetBagelType(m_BagelTypeCollection.collection[m_SelectedBagelIndex]);
        }

        public void SelectBagelAndGoToPlay()
        {
            m_PlayManager.state.SetBagelType(m_BagelTypeCollection.collection[m_SelectedBagelIndex]);
            m_PlayManager.state.GoToPlay();
        }

        public void NextBagel()
        {
            var newIndex = Mathf.Min(m_SelectedBagelIndex + 1, m_BagelTypeCollection.collection.Count - 1);
            if (newIndex == m_SelectedBagelIndex)
                return;

            m_SelectedBagelIndex = newIndex;
            onBagelTypeChange?.Invoke(this, m_SelectedBagelIndex);
            m_PlayManager.state.SetBagelType(m_BagelTypeCollection.collection[m_SelectedBagelIndex]);
        }

        public void PreviousBagel()
        {
            var newIndex = Mathf.Max(m_SelectedBagelIndex - 1, 0);
            if (newIndex == m_SelectedBagelIndex)
                return;

            m_SelectedBagelIndex = newIndex;
            onBagelTypeChange?.Invoke(this, m_SelectedBagelIndex);
            m_PlayManager.state.SetBagelType(m_BagelTypeCollection.collection[m_SelectedBagelIndex]);
        }
    }
}
