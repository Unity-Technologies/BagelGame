using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bagel
{
    public class PlayInputBindings : MonoBehaviour
    {
        public enum Binding {
            Move_Up,
            Move_Down,
            Move_Left,
            Move_Right,
            Pause,
            Gamepad_Pause
        };

        const string k_PlayerPrefsName = "BagelGame_PlayInputBindings";

        public event EventHandler onPauseAction;

        public event EventHandler onBindingRebind;

        PlayInputActions m_PlayInputActions;

        // Made on first use: a screen can ask for the bindings' names before this Awake runs.
        PlayInputActions actions
        {
            get
            {
                if (m_PlayInputActions != null)
                    return m_PlayInputActions;

                m_PlayInputActions = new PlayInputActions();

                if (PlayerPrefs.HasKey(k_PlayerPrefsName) )
                    m_PlayInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(k_PlayerPrefsName));

                m_PlayInputActions.Player.Enable();
                m_PlayInputActions.Player.Pause.performed += PausePerformed;
                return m_PlayInputActions;
            }
        }

        void Awake() => _ = actions;

        void OnDestroy()
        {
            if (m_PlayInputActions == null)
                return;

            m_PlayInputActions.Player.Disable();
            m_PlayInputActions.Player.Pause.performed -= PausePerformed;

            m_PlayInputActions.Dispose();
            m_PlayInputActions = null;
        }

        void PausePerformed(InputAction.CallbackContext ctx)
        {
            onPauseAction?.Invoke(this, EventArgs.Empty);
        }

        // A touch screen's thumb stick, while it's held (the touch controls set it): it moves the bagel like the keys and
        // the gamepad's stick, and as they do (the Move action normalizes them), says only which way.
        public Vector2 touchMove { get; set; }

        const float k_TouchDeadZone = 0.2f;

        public Vector2 GetMovementVectorNormalized()
        {
            if (touchMove.sqrMagnitude > k_TouchDeadZone * k_TouchDeadZone)
                return touchMove.normalized;
            return actions.Player.Move.ReadValue<Vector2>();
        }

        public string GetBindingText(Binding binding)
        {
            switch (binding)
            {
                default:
                case Binding.Move_Up:
                    return actions.Player.Move.bindings[ 1 ].ToDisplayString();
                case Binding.Move_Down:
                    return actions.Player.Move.bindings[ 2 ].ToDisplayString();
                case Binding.Move_Left:
                    return actions.Player.Move.bindings[ 3 ].ToDisplayString();
                case Binding.Move_Right:
                    return actions.Player.Move.bindings[ 4 ].ToDisplayString();
                case Binding.Pause:
                    return actions.Player.Pause.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Pause:
                    return actions.Player.Pause.bindings[ 1 ].ToDisplayString();
            }
        }

        public void RebindBinding(Binding binding, Action onActionRebound)
        {
            actions.Player.Disable();

            InputAction inputAction;
            int bindingIndex;

            switch (binding)
            {
                default:
                case Binding.Move_Up:
                    inputAction = actions.Player.Move;
                    bindingIndex = 1;
                    break;
                case Binding.Move_Down:
                    inputAction = actions.Player.Move;
                    bindingIndex = 2;
                    break;
                case Binding.Move_Left:
                    inputAction = actions.Player.Move;
                    bindingIndex = 3;
                    break;
                case Binding.Move_Right:
                    inputAction = actions.Player.Move;
                    bindingIndex = 4;
                    break;
                case Binding.Pause:
                    inputAction = actions.Player.Pause;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Pause:
                    inputAction = actions.Player.Pause;
                    bindingIndex = 1;
                    break;
            }

            // While waiting for a key, the UI doesn't listen either: the key would also move the focus.
            var uiActions = InputSystem.actions;
            uiActions?.Disable();

            inputAction.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(operation =>
                {
                    Finish(operation);

                    var inputJson = actions.SaveBindingOverridesAsJson();
                    PlayerPrefs.SetString(k_PlayerPrefsName, inputJson);
                    PlayerPrefs.Save();

                    onBindingRebind?.Invoke( this, EventArgs.Empty );
                })
                .OnCancel(Finish)
                .Start();

            void Finish(InputActionRebindingExtensions.RebindingOperation operation)
            {
                operation.Dispose();
                actions.Player.Enable();
                uiActions?.Enable();
                onActionRebound?.Invoke();
            }
        }

        public void ResetBindings()
        {
            actions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(k_PlayerPrefsName);
            PlayerPrefs.Save();

            onBindingRebind?.Invoke(this, EventArgs.Empty);
        }

    }
}
