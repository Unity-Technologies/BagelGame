using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
    public class GameOverScreenDriver : MonoBehaviour
    {
        public PlayManager PlayManager;

        UIDocument m_UIDocument;

        void OnEnable()
        {
            m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument.rootVisualElement;

            Button button;

            button = root.Q<Button>("main-menu-button");
            if (button != null)
                button.clicked += PlayManager.GoToMainMenu;
        }
    }
}
