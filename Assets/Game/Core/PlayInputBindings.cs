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

        public event EventHandler<Vector2> OnNavigateAction;
        public event EventHandler OnUpAction;
        public event EventHandler OnDownAction;
        public event EventHandler OnLeftAction;
        public event EventHandler OnRightAction;

        public event EventHandler OnSubmitAction;
        public event EventHandler OnCancelAction;

        public event EventHandler OnBindingRebind;

        PlayInputActions m_PlayInputActions;

        void Awake()
        {
            m_PlayInputActions = new PlayInputActions();

            if (PlayerPrefs.HasKey(k_PlayerPrefsName) )
                m_PlayInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(k_PlayerPrefsName));

            m_PlayInputActions.Player.Enable();

            m_PlayInputActions.Player.Pause.performed += PausePerformed;

            m_PlayInputActions.UI.Enable();

            m_PlayInputActions.UI.Navigate.performed += NavigatePerformed;
            m_PlayInputActions.UI.Submit.performed += SubmitPerformed;
            m_PlayInputActions.UI.Cancel.performed += CancelPerformed;
        }

        void OnDestroy()
        {
            m_PlayInputActions.Player.Disable();
            m_PlayInputActions.Player.Pause.performed -= PausePerformed;

            m_PlayInputActions.UI.Disable();
            m_PlayInputActions.UI.Navigate.performed -= NavigatePerformed;
            m_PlayInputActions.UI.Submit.performed -= SubmitPerformed;
            m_PlayInputActions.UI.Cancel.performed -= CancelPerformed;

            m_PlayInputActions.Dispose();
        }

        void PausePerformed(InputAction.CallbackContext ctx)
        {
            OnPauseAction?.Invoke(this, EventArgs.Empty);
        }

        void NavigatePerformed(InputAction.CallbackContext ctx)
        {
            var vec = ctx.ReadValue<Vector2>();
            OnNavigateAction?.Invoke(this, vec);

            if (vec.y > 0)
                OnUpAction?.Invoke(this, EventArgs.Empty);
            else if (vec.y < 0)
                OnDownAction?.Invoke(this, EventArgs.Empty);

            if (vec.x < 0 )
                OnLeftAction?.Invoke(this, EventArgs.Empty);
            else if (vec.x > 0)
                OnRightAction?.Invoke(this, EventArgs.Empty);
        }

        void SubmitPerformed(InputAction.CallbackContext ctx)
        {
            OnSubmitAction?.Invoke(this, EventArgs.Empty);
        }

        void CancelPerformed(InputAction.CallbackContext ctx)
        {
            OnCancelAction?.Invoke(this, EventArgs.Empty);
        }

        public Vector2 GetMovementVectorNormalized()
        {
            return m_PlayInputActions.Player.Move.ReadValue<Vector2>();
        }

        public string GetBindingText(Binding binding)
        {
            switch (binding)
            {
                default:
                case Binding.Move_Up:
                    return m_PlayInputActions.Player.Move.bindings[ 1 ].ToDisplayString();
                case Binding.Move_Down:
                    return m_PlayInputActions.Player.Move.bindings[ 2 ].ToDisplayString();
                case Binding.Move_Left:
                    return m_PlayInputActions.Player.Move.bindings[ 3 ].ToDisplayString();
                case Binding.Move_Right:
                    return m_PlayInputActions.Player.Move.bindings[ 4 ].ToDisplayString();
                case Binding.Pause:
                    return m_PlayInputActions.Player.Pause.bindings[ 0 ].ToDisplayString();
                case Binding.Gamepad_Pause:
                    return m_PlayInputActions.Player.Pause.bindings[ 1 ].ToDisplayString();
            }
        }

        public void RebindBinding(Binding binding, Action onActionRebound)
        {
            m_PlayInputActions.Player.Disable();

            InputAction inputAction;
            int bindingIndex;

            switch (binding)
            {
                default:
                case Binding.Move_Up:
                    inputAction = m_PlayInputActions.Player.Move;
                    bindingIndex = 1;
                    break;
                case Binding.Move_Down:
                    inputAction = m_PlayInputActions.Player.Move;
                    bindingIndex = 2;
                    break;
                case Binding.Move_Left:
                    inputAction = m_PlayInputActions.Player.Move;
                    bindingIndex = 3;
                    break;
                case Binding.Move_Right:
                    inputAction = m_PlayInputActions.Player.Move;
                    bindingIndex = 4;
                    break;
                case Binding.Pause:
                    inputAction = m_PlayInputActions.Player.Pause;
                    bindingIndex = 0;
                    break;
                case Binding.Gamepad_Pause:
                    inputAction = m_PlayInputActions.Player.Pause;
                    bindingIndex = 1;
                    break;
            }

            inputAction.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(callback =>
                {
                    m_PlayInputActions.Player.Enable();
                    onActionRebound();

                    var inputJson = m_PlayInputActions.SaveBindingOverridesAsJson();
                    PlayerPrefs.SetString(k_PlayerPrefsName, inputJson);
                    PlayerPrefs.Save();

                    OnBindingRebind?.Invoke( this, EventArgs.Empty );
                })
                .Start();
        }

    }
}
