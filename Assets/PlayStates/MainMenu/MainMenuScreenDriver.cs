using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [RequireComponent(typeof(UIDocument))]
	public class MainMenuScreenDriver : MonoBehaviour
	{
		public PlayManager PlayManager;

		UIDocument m_UIDocument;

		void OnEnable() {
			m_UIDocument = GetComponent<UIDocument>();
			var root = m_UIDocument.rootVisualElement;

			var playButton = root.Q<Button>("play-button");
			if (playButton != null)
				playButton.clicked += PlayManager.GoToPlay;

			var quitButton = root.Q<Button>("quit-button");
			if (quitButton != null)
				quitButton.clicked += Application.Quit;
		}
	}
}
