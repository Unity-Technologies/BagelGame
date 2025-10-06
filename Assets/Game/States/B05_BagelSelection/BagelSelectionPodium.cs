using System;
using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    public class BagelSelectionPodium : MonoBehaviour
    {
        public BagelType BagelType;

        [SerializeField] Transform m_BagelSlot;

        public event EventHandler<BagelType> onBagelTypeChange;

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

            onBagelTypeChange?.Invoke(this, BagelType);
        }
    }
}
