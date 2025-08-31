using UnityEngine;

namespace Bagel
{
    public class BagelSelectionConveyor : MonoBehaviour
    {
        [SerializeField] BagelSelectionRoom m_BagelSelectionRoom;
        [SerializeField] Transform m_BagelPodiumPrefab;
        [SerializeField] float m_PodiumXOffset = 3.0f;

        void Start()
        {
            m_BagelSelectionRoom.OnBagelTypeCollectionChange += BagelSelectionRoom_OnBagelTypeCollectionChange;
            m_BagelSelectionRoom.OnBagelTypeChange += BagelSelectionRoom_OnBagelTypeChange;
        }

        void OnEnable()
        {
            CreatePodiums();
        }

        void BagelSelectionRoom_OnBagelTypeChange(object sender, int index)
        {
            transform.localPosition = new Vector3(-((float)index * m_PodiumXOffset), 0, 0);
        }

        void BagelSelectionRoom_OnBagelTypeCollectionChange(object sender, System.EventArgs e)
        {
            CreatePodiums();
        }

        void CreatePodiums()
        {
            transform.localPosition = Vector3.zero;

            foreach (Transform child in transform)
                DestroyImmediate(child.gameObject);

            float xOffset = 0.0f;
            foreach (BagelType bagelType in m_BagelSelectionRoom.BagelTypeCollection.collection)
            {
                var podiumObj = Instantiate(m_BagelPodiumPrefab, transform);
                podiumObj.localPosition = new Vector3(xOffset, 0, 0);

                var podium = podiumObj.GetComponent<BagelSelectionPodium>();
                podium.BagelType = bagelType;
                podium.InitBagelType();

                xOffset += m_PodiumXOffset;
            }
        }
    }
}
