using System;
using UnityEngine;

namespace Bagel
{
    public class BagelSelectionPodium : MonoBehaviour
    {
        public BagelType BagelType;

        [SerializeField] Transform m_BagelSlot;

        public event EventHandler<BagelType> OnBagelTypeChange;

        void OnEnable()
        {
            InitBagelType();
        }

        public void InitBagelType()
        {
            foreach (Transform child in m_BagelSlot)
                DestroyImmediate(child.gameObject);

            if (BagelType == null)
                return;

            Instantiate(BagelType.modelPrefab, m_BagelSlot);

            OnBagelTypeChange?.Invoke(this, BagelType);
        }
    }
}
