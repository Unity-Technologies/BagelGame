using UnityEngine.UIElements;

namespace Bagel
{
    // Settings › Graphics: post-processing, anti-aliasing, the game over shot's depth of field, shadows and the frame
    // rate (GraphicsQuality puts them on screen). The toggles are two-way bound in the UXML; the two choices are
    // picked here, since a button group's value is the buttons it has on, not a number.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForGraphics : BagelPane
    {
        ToggleButtonGroup m_AntiAliasingChoice;
        ToggleButtonGroup m_FrameRateChoice;

        // Off, FXAA, SMAA: settings.antiAliasing.
        public ToggleButtonGroup antiAliasingChoice => m_AntiAliasingChoice ??= this.Q<ToggleButtonGroup>("anti-aliasing-choice");

        // SettingsData.frameRates, in order: settings.frameRate.
        public ToggleButtonGroup frameRateChoice => m_FrameRateChoice ??= this.Q<ToggleButtonGroup>("frame-rate-choice");

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            antiAliasingChoice?.RegisterValueChangedCallback(evt =>
            {
                var index = Choice.Selected(evt.newValue);
                if (index >= 0 && state != null)
                    state.settings.antiAliasing = index;
            });
            frameRateChoice?.RegisterValueChangedCallback(evt =>
            {
                var index = Choice.Selected(evt.newValue);
                if (index >= 0 && index < SettingsData.frameRates.Length && state != null)
                    state.settings.frameRate = SettingsData.frameRates[index];
            });
            UpdateChoices();
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateChoices();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == SettingsData.antiAliasingPath || property == SettingsData.frameRatePath)
                UpdateChoices();
        }

        void UpdateChoices()
        {
            if (state == null)
                return;
            antiAliasingChoice?.SetValueWithoutNotify(Choice.One(state.settings.antiAliasing, 3));
            frameRateChoice?.SetValueWithoutNotify(Choice.One(System.Array.IndexOf(SettingsData.frameRates, state.settings.frameRate), SettingsData.frameRates.Length));
        }
    }
}
