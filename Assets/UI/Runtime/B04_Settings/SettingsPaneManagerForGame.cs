using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForGame : BagelPane
    {
        Slider m_FieldOfViewSlider;
        Toggle m_SoundToggle;
        Slider m_VolumeSlider;

        // Two-way bound to settings.fieldOfView in the UXML; the camera follows the state.
        public Slider fieldOfViewSlider => m_FieldOfViewSlider ??= this.Q<Slider>("fov-slider");

        // Two-way bound to settings.sound and settings.soundVolume; GameSounds follows the state.
        public Toggle soundToggle => m_SoundToggle ??= this.Q<Toggle>("sound-toggle");
        public Slider volumeSlider => m_VolumeSlider ??= this.Q<Slider>("volume-slider");
    }
}
