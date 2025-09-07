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

        public PlayManager PlayManager => m_PlayManager;
        public BagelTypeCollection BagelTypeCollection => m_BagelTypeCollection;
        public float PodiumXOffset => m_PodiumXOffset;
        public event EventHandler OnBagelTypeCollectionChange;
        public event EventHandler<int> OnBagelTypeChange;

        int m_SelectedBagelIndex;
        public int BagelTypeCount => m_BagelTypeCollection.collection.Count;
        public int SelectedBagelIndex => m_SelectedBagelIndex;

        void OnEnable()
        {
            InitBagelCollection();
        }

        public void InitBagelCollection()
        {
            m_SelectedBagelIndex = 0;

            OnBagelTypeCollectionChange?.Invoke(this, null);
            OnBagelTypeChange?.Invoke(this, m_SelectedBagelIndex);

            m_PlayManager.State.SetBagelType(m_BagelTypeCollection.collection[m_SelectedBagelIndex]);
        }

        public void SelectBagelAndGoToPlay()
        {
            m_PlayManager.State.SetBagelType(m_BagelTypeCollection.collection[m_SelectedBagelIndex]);
            m_PlayManager.State.GoToPlay();
        }

        public void NextBagel()
        {
            var newIndex = Mathf.Min(m_SelectedBagelIndex + 1, m_BagelTypeCollection.collection.Count - 1);
            if (newIndex == m_SelectedBagelIndex)
                return;

            m_SelectedBagelIndex = newIndex;
            OnBagelTypeChange?.Invoke(this, m_SelectedBagelIndex);
        }

        public void PreviousBagel()
        {
            var newIndex = Mathf.Max(m_SelectedBagelIndex - 1, 0);
            if (newIndex == m_SelectedBagelIndex)
                return;

            m_SelectedBagelIndex = newIndex;
            OnBagelTypeChange?.Invoke(this, m_SelectedBagelIndex);
        }
    }
}
