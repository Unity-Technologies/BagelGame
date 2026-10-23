using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Bagel
{
    // Shows the keys that drive the bagel and lets the player change them (interactive rebinding).
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManagerForControls : BagelPane
    {
        [NoAutoStaticsCleanup] // a read-only table
        static readonly (string name, PlayInputBindings.Binding binding)[] s_Rows =
        {
            ("move-up", PlayInputBindings.Binding.Move_Up),
            ("move-down", PlayInputBindings.Binding.Move_Down),
            ("move-left", PlayInputBindings.Binding.Move_Left),
            ("move-right", PlayInputBindings.Binding.Move_Right),
            ("pause", PlayInputBindings.Binding.Pause),
            ("gamepad-pause", PlayInputBindings.Binding.Gamepad_Pause),
        };

        readonly Dictionary<PlayInputBindings.Binding, Button> m_Buttons = new();
        PlayInputBindings m_PlayInputBindings;
        Button m_ResetButton;

        public Button resetButton => m_ResetButton ??= this.Q<Button>("reset-controls-button");
        public Button GetButton(PlayInputBindings.Binding binding) => m_Buttons.TryGetValue(binding, out var button) ? button : null;

        // Set by the in-game Drivers. Opened on its own, the pane shows the default keys.
        public PlayInputBindings playInputBindings
        {
            get => m_PlayInputBindings;
            set
            {
                if (m_PlayInputBindings != null)
                    m_PlayInputBindings.onBindingRebind -= PlayInputBindings_OnBindingRebind;
                m_PlayInputBindings = value;
                if (m_PlayInputBindings != null)
                    m_PlayInputBindings.onBindingRebind += PlayInputBindings_OnBindingRebind;
                UpdateButtons();
            }
        }

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            foreach (var (name, binding) in s_Rows)
            {
                var button = this.Q<Button>(name + "-button");
                if (button == null)
                    continue;
                m_Buttons[binding] = button;
                button.clicked += () => Rebind(binding);
            }
            resetButton.clicked += () => m_PlayInputBindings?.ResetBindings();

            UpdateButtons();
        }

        protected override void OnDetached() => playInputBindings = null;

        void Rebind(PlayInputBindings.Binding binding)
        {
            if (m_PlayInputBindings == null)
                return;

            var button = m_Buttons[binding];
            button.text = BagelLocalization.Get("controls.press-key", "Press a key...");
            m_PlayInputBindings.RebindBinding(binding, () =>
            {
                UpdateButtons();
                button.Focus();
            });
        }

        void PlayInputBindings_OnBindingRebind(object sender, System.EventArgs e) => UpdateButtons();

        void UpdateButtons()
        {
            foreach (var (binding, button) in m_Buttons)
            {
                button.SetEnabled(m_PlayInputBindings != null);
                if (m_PlayInputBindings != null)
                    button.text = m_PlayInputBindings.GetBindingText(binding);
            }
            resetButton?.SetEnabled(m_PlayInputBindings != null);
        }
    }
}
