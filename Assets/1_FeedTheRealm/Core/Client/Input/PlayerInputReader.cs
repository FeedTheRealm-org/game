using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[CreateAssetMenu(fileName = "PlayerInputReader", menuName = "Scriptable Objects/PlayerInputReader")]
public class PlayerInputReader : ScriptableObject, PlayerControls.IPlayerActions
{
    public event Action<Vector2> MoveEvent;
    public event Action DashEvent;
    public event Action InventoryEvent;
    public event Action UseEvent;
    public event Action CursorToggleEvent;
    public event Action InteractEvent;
    public event Action<int> FastSlotEvent;
    public event Action ChatToggleEvent;

    private PlayerControls controls;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new PlayerControls();
            controls.Player.SetCallbacks(this);
        }
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    public void OnUse(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            UseEvent?.Invoke();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            MoveEvent?.Invoke(context.ReadValue<Vector2>());
        }
        else if (context.canceled)
        {
            MoveEvent?.Invoke(Vector2.zero);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DashEvent?.Invoke();
        }
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InventoryEvent?.Invoke();
        }
    }

    public void OnCursorToggle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CursorToggleEvent?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InteractEvent?.Invoke();
        }
    }

    public void OnChatToggle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ChatToggleEvent?.Invoke();
        }
    }

    public void OnFastSlots(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        int slotIndex = GetFastSlotIndex(context);
        if (slotIndex <= 0)
            return;

        FastSlotEvent?.Invoke(slotIndex);
    }

    private static int GetFastSlotIndex(InputAction.CallbackContext context)
    {
        if (context.control is KeyControl keyControl)
        {
            switch (keyControl.keyCode)
            {
                case Key.Digit1:
                case Key.Numpad1:
                    return 1;
                case Key.Digit2:
                case Key.Numpad2:
                    return 2;
                case Key.Digit3:
                case Key.Numpad3:
                    return 3;
                case Key.Digit4:
                case Key.Numpad4:
                    return 4;
                case Key.Digit5:
                case Key.Numpad5:
                    return 5;
                case Key.Digit6:
                case Key.Numpad6:
                    return 6;
                case Key.Digit7:
                case Key.Numpad7:
                    return 7;
                case Key.Digit8:
                case Key.Numpad8:
                    return 8;
                case Key.Digit9:
                case Key.Numpad9:
                    return 9;
            }
        }

        return 0;
    }
}
