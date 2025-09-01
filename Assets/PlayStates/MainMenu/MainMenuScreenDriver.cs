using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuScreenDriver : MonoBehaviour
    {
        [SerializeField] PlayManager m_PlayManager;

        UIDocument m_UIDocument;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            Button button;

            button = root.Q<Button>("play-button");
            if (button != null)
                button.clicked += m_PlayManager.State.GoToBagelSelection;

            button = root.Q<Button>("quit-button");
            if (button != null)
                button.clicked += Application.Quit;
        }
    }
}
