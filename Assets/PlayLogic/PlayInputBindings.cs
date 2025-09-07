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
            Gamepad_Pause,
            Confirm,
            Gamepad_Confirm,
            Back,
            Gamepad_Back,
            Up,
            Gamepad_Up,
            Down,
            Gamepad_Down,
            Left,
            Gamepad_Left,
            Right,
            Gamepad_Right,
        };

        const string k_PlayerPrefsName = "BagelGame_PlayInputBindings";

        public event EventHandler OnPauseAction;

        public event EventHandler OnConfirmAction;
        public event EventHandler OnBackAction;

        public event EventHandler OnUpAction;
        public event EventHandler OnDownAction;
        public event EventHandler OnLeftAction;
        public event EventHandler OnRightAction;

        public event EventHandler OnNavigationAction;

        public event EventHandler OnBindingRebind;

        PlayerInputActions m_PlayerInputActions;

        void Awake()
        {
            m_PlayerInputActions = new PlayerInputActions();

            if (PlayerPrefs.HasKey(k_PlayerPrefsName) )
                m_PlayerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(k_PlayerPrefsName));

            m_PlayerInputActions.Player.Enable();

            m_PlayerInputActions.Player.Pause.performed += PausePerformed;
            m_PlayerInputActions.Player.Confirm.performed += ConfirmPerformed;
            m_PlayerInputActions.Player.Back.performed += BackPerformed;

            m_PlayerInputActions.Player.Up.performed += UpPerformed;
            m_PlayerInputActions.Player.Down.performed += DownPerformed;
            m_PlayerInputActions.Player.Left.performed += LeftPerformed;
            m_PlayerInputActions.Player.Right.performed += RightPerformed;
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

        void ConfirmPerformed(InputAction.CallbackContext obj)
        {
            OnConfirmAction?.Invoke(this, EventArgs.Empty);
        }

        void BackPerformed(InputAction.CallbackContext obj)
        {
            OnBackAction?.Invoke(this, EventArgs.Empty);
        }

        void UpPerformed(InputAction.CallbackContext obj)
        {
            OnUpAction?.Invoke(this, EventArgs.Empty);
            OnNavigationAction?.Invoke(this, EventArgs.Empty);
        }

        void DownPerformed(InputAction.CallbackContext obj)
        {
            OnDownAction?.Invoke(this, EventArgs.Empty);
            OnNavigationAction?.Invoke(this, EventArgs.Empty);
        }

        void LeftPerformed(InputAction.CallbackContext obj)
        {
            OnLeftAction?.Invoke(this, EventArgs.Empty);
            OnNavigationAction?.Invoke(this, EventArgs.Empty);
        }

        void RightPerformed(InputAction.CallbackContext obj)
        {
            OnRightAction?.Invoke(this, EventArgs.Empty);
            OnNavigationAction?.Invoke(this, EventArgs.Empty);
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
                case Binding.Confirm:
                    return m_PlayerInputActions.Player.Confirm.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Confirm:
                    return m_PlayerInputActions.Player.Confirm.bindings[ 1 ].ToDisplayString();
                case Binding.Back:
                    return m_PlayerInputActions.Player.Back.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Back:
                    return m_PlayerInputActions.Player.Back.bindings[ 1 ].ToDisplayString();
                case Binding.Up:
                    return m_PlayerInputActions.Player.Up.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Up:
                    return m_PlayerInputActions.Player.Up.bindings[ 1 ].ToDisplayString();
                case Binding.Down:
                    return m_PlayerInputActions.Player.Down.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Down:
                    return m_PlayerInputActions.Player.Down.bindings[ 1 ].ToDisplayString();
                case Binding.Left:
                    return m_PlayerInputActions.Player.Left.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Left:
                    return m_PlayerInputActions.Player.Left.bindings[ 1 ].ToDisplayString();
                case Binding.Right:
                    return m_PlayerInputActions.Player.Right.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Right:
                    return m_PlayerInputActions.Player.Right.bindings[ 1 ].ToDisplayString();
            }
        }

        public void RebindBinding(Binding binding, Action onActionRebound)
        {
            m_PlayerInputActions.Player.Disable();

            InputAction inputAction;
            int bindingIndex;

            switch (binding)
            {
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
                case Binding.Confirm:
                    inputAction = m_PlayerInputActions.Player.Confirm;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Confirm:
                    inputAction = m_PlayerInputActions.Player.Confirm;
                    bindingIndex = 1;
                    break;
                case Binding.Back:
                    inputAction = m_PlayerInputActions.Player.Back;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Back:
                    inputAction = m_PlayerInputActions.Player.Back;
                    bindingIndex = 1;
                    break;
                case Binding.Up:
                    inputAction = m_PlayerInputActions.Player.Up;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Up:
                    inputAction = m_PlayerInputActions.Player.Up;
                    bindingIndex = 1;
                    break;
                case Binding.Down:
                    inputAction = m_PlayerInputActions.Player.Down;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Down:
                    inputAction = m_PlayerInputActions.Player.Down;
                    bindingIndex = 1;
                    break;
                case Binding.Left:
                    inputAction = m_PlayerInputActions.Player.Left;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Left:
                    inputAction = m_PlayerInputActions.Player.Left;
                    bindingIndex = 1;
                    break;
                case Binding.Right:
                    inputAction = m_PlayerInputActions.Player.Right;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Right:
                    inputAction = m_PlayerInputActions.Player.Right;
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
