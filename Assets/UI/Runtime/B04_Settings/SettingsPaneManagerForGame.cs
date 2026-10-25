using System;
using UnityEngine.UIElements;

namespace Bagel
{
    // Settings › Game: the field of view, the sound, the language (each one named in itself), and two buttons you
    // hold down to be sure: one clears the leaderboard, one puts every setting back.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForGame : BagelPane
    {
        Slider m_FieldOfViewSlider;
        Toggle m_SoundToggle;
        Slider m_VolumeSlider;
        ToggleButtonGroup m_LanguageChoice;
        LongPressButton m_ClearLeaderboardButton;
        LongPressButton m_ResetSettingsButton;

        // Two-way bound to settings.fieldOfView in the UXML; the camera follows the state.
        public Slider fieldOfViewSlider => m_FieldOfViewSlider ??= this.Q<Slider>("fov-slider");

        // Two-way bound to settings.sound and settings.soundVolume; GameSounds follows the state.
        public Toggle soundToggle => m_SoundToggle ??= this.Q<Toggle>("sound-toggle");
        public Slider volumeSlider => m_VolumeSlider ??= this.Q<Slider>("volume-slider");

        // One button for each of the game's languages, in BagelLocalization.languages order.
        public ToggleButtonGroup languageChoice => m_LanguageChoice ??= this.Q<ToggleButtonGroup>("language-choice");
        public LongPressButton clearLeaderboardButton => m_ClearLeaderboardButton ??= this.Q<LongPressButton>("clear-leaderboard-button");
        public LongPressButton resetSettingsButton => m_ResetSettingsButton ??= this.Q<LongPressButton>("reset-settings-button");

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            if (languageChoice != null)
            {
                // Each language in its own words: whoever reads only that one can find it.
                foreach (var (code, name) in BagelLocalization.languages)
                    languageChoice.Add(new Button { text = name, name = "language-" + code });
                UpdateLanguage();
                languageChoice.RegisterValueChangedCallback(evt =>
                {
                    var index = Choice.Selected(evt.newValue);
                    if (index >= 0 && index < BagelLocalization.languages.Count)
                        BagelLocalization.SetLocale(BagelLocalization.languages[index].code);
                });
            }

            if (clearLeaderboardButton != null)
                clearLeaderboardButton.clicked += () => state?.leaderboard.Clear();
            if (resetSettingsButton != null)
                resetSettingsButton.clicked += () => state?.settings.Reset();
        }

        protected override void OnLocaleChanged() => UpdateLanguage();

        void UpdateLanguage()
        {
            var languages = BagelLocalization.languages;
            var index = -1;
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i].code == BagelLocalization.localeCode)
                    index = i;
            }
            languageChoice?.SetValueWithoutNotify(Choice.One(Math.Max(0, index), languages.Count));
        }
    }
}
