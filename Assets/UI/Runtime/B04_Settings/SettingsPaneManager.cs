using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManager : BagelPane
    {
        SettingsPaneManagerForGame m_SettingsPaneManagerForGame;
        SettingsPaneManagerForUI m_SettingsPaneManagerForUI;
        Button m_BackButton;

        public SettingsPaneManagerForGame forGame => m_SettingsPaneManagerForGame ??= this.Q<SettingsPaneManagerForGame>();
        public SettingsPaneManagerForUI forUI => m_SettingsPaneManagerForUI ??= this.Q<SettingsPaneManagerForUI>();
        public Button backButton => m_BackButton ??= this.Q<Button>("back-button");

        public struct Callbacks
        {
            public Action onSettingsBack;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (backButton != null && callbacks.onSettingsBack != null)
                backButton.clicked += callbacks.onSettingsBack;
        }

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            // The page title comes from the Page template, so its binding is added here.
            BagelLocalization.Bind(this.Q<Label>("page-title"), "text", "settings.title");
        }
    }
}
