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

        public event EventHandler OnPauseAction;
        public event EventHandler OnBindingRebind;

        PlayerInputActions m_PlayerInputActions;

        void Awake()
        {
            m_PlayerInputActions = new PlayerInputActions();

            if (PlayerPrefs.HasKey(k_PlayerPrefsName) )
                m_PlayerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(k_PlayerPrefsName) );

            m_PlayerInputActions.Player.Enable();
            m_PlayerInputActions.Player.Pause.performed += PausePerformed;
        }

        void OnDestroy()
        {
            m_PlayerInputActions.Player.Pause.performed -= PausePerformed;
            m_PlayerInputActions.Player.Disable();
            m_PlayerInputActions.Dispose();
        }

        void PausePerformed(InputAction.CallbackContext obj)
        {
            OnPauseAction?.Invoke(this, EventArgs.Empty);
        }

        public Vector2 GetMovementVectorNormalized()
        {
            return m_PlayerInputActions.Player.Move.ReadValue<Vector2>();
        }

        public string GetBindingText(Binding binding)
        {
            switch (binding)
            {
                default:
                case Binding.Move_Up:
                    return m_PlayerInputActions.Player.Move.bindings[ 1 ].ToDisplayString();
                case Binding.Move_Down:
                    return m_PlayerInputActions.Player.Move.bindings[ 2 ].ToDisplayString();
                case Binding.Move_Left:
                    return m_PlayerInputActions.Player.Move.bindings[ 3 ].ToDisplayString();
                case Binding.Move_Right:
                    return m_PlayerInputActions.Player.Move.bindings[ 4 ].ToDisplayString();
                case Binding.Pause:
                    return m_PlayerInputActions.Player.Pause.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Pause:
                    return m_PlayerInputActions.Player.Pause.bindings[ 1 ].ToDisplayString();
            }
        }

        public void RebindBinding(Binding binding, Action onActionRebound)
        {
            m_PlayerInputActions.Player.Disable();

            InputAction inputAction;
            int bindingIndex;

            switch (binding) {
                default:
                case Binding.Move_Up:
                    inputAction = m_PlayerInputActions.Player.Move;
                    bindingIndex = 1;
                    break;
                case Binding.Move_Down:
                    inputAction = m_PlayerInputActions.Player.Move;
                    bindingIndex = 2;
                    break;
                case Binding.Move_Left:
                    inputAction = m_PlayerInputActions.Player.Move;
                    bindingIndex = 3;
                    break;
                case Binding.Move_Right:
                    inputAction = m_PlayerInputActions.Player.Move;
                    bindingIndex = 4;
                    break;
                case Binding.Pause:
                    inputAction = m_PlayerInputActions.Player.Pause;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Pause:
                    inputAction = m_PlayerInputActions.Player.Pause;
                    bindingIndex = 1;
                    break;
            }

            inputAction.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(callback =>
                {
                    m_PlayerInputActions.Player.Enable();
                    onActionRebound();

                    var inputJson = m_PlayerInputActions.SaveBindingOverridesAsJson();
                    PlayerPrefs.SetString(k_PlayerPrefsName, inputJson);
                    PlayerPrefs.Save();

                    OnBindingRebind?.Invoke( this, EventArgs.Empty );
                })
                .Start();
        }

    }
}
