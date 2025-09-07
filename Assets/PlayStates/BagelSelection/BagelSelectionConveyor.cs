using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bagel
{
    [ExecuteInEditMode]
    public class BagelSelectionConveyor : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] BagelSelectionRoom m_BagelSelectionRoom;
        [SerializeField] GameObject m_BagelPodiumPrefab;

        [Space]
        [SerializeField] List<Transform> m_BagelPodiums;

        void Start()
        {
            m_BagelSelectionRoom.OnBagelTypeCollectionChange += BagelSelectionRoom_OnBagelTypeCollectionChange;
        }

        void OnEnable()
        {
            m_BagelSelectionRoom.OnBagelTypeCollectionChange += BagelSelectionRoom_OnBagelTypeCollectionChange;

            CreatePodiums();
        }

        void OnDisable()
        {
            m_BagelSelectionRoom.OnBagelTypeCollectionChange -= BagelSelectionRoom_OnBagelTypeCollectionChange;
        }

        void Update()
        {
            UpdatePodiums();
        }

        void BagelSelectionRoom_OnBagelTypeCollectionChange(object sender, System.EventArgs e)
        {
            CreatePodiums();
        }

        void Clear()
        {
            m_BagelPodiums.Clear();

            var childrenToDestroy = new List<GameObject>();
            foreach (Transform child in transform)
                childrenToDestroy.Add(child.gameObject);

            foreach (var child in childrenToDestroy)
                DestroyImmediate(child);
        }

        void CreatePodiums()
        {
            Clear();

            foreach (BagelType bagelType in m_BagelSelectionRoom.BagelTypeCollection.collection)
            {
                var podiumObj = PrefabUtility.InstantiatePrefab(m_BagelPodiumPrefab, transform) as GameObject;
                podiumObj.name = bagelType.name;

                var podium = podiumObj.GetComponent<BagelSelectionPodium>();
                podium.BagelType = bagelType;
                podium.InitBagelType();

                m_BagelPodiums.Add(podiumObj.transform);
            }

            UpdatePodiums();
        }

        void UpdatePodiums()
        {
            float bagelTypeCount = m_BagelPodiums.Count;
            float midpointShift = 0.5f * (bagelTypeCount - 1) * m_BagelSelectionRoom.PodiumXOffset;

            for (int i = 0; i < bagelTypeCount; ++i)
            {
                float offset = ((float)i * m_BagelSelectionRoom.PodiumXOffset) - midpointShift;
                m_BagelPodiums[i].localPosition = new Vector3(offset, 0, 0);
            }
        }
#endif // UNITY_EDITOR
    }
}
