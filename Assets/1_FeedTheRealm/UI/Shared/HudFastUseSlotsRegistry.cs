using System;
using UnityEngine;

/// <summary>
/// Runtime registry to share the active HudFastUseSlotsController instance.
/// This avoids prefab->scene references and avoids FindObjectOfType.
///
/// Create one asset via: Create > Scriptable Objects > UI > Hud Fast Use Slots Registry
/// Assign that asset to both the HUD and the Inventory prefab instances.
/// </summary>
[CreateAssetMenu(
    fileName = "HudFastUseSlotsRegistry",
    menuName = "Scriptable Objects/UI/Hud Fast Use Slots Registry"
)]
public sealed class HudFastUseSlotsRegistry : ScriptableObject
{
    public event Action<HudFastUseSlotsController> Changed;

    public HudFastUseSlotsController Current => _current;

    [NonSerialized]
    private HudFastUseSlotsController _current;

    public void Register(HudFastUseSlotsController controller)
    {
        if (ReferenceEquals(_current, controller))
            return;

        _current = controller;
        Changed?.Invoke(_current);
    }

    public void Unregister(HudFastUseSlotsController controller)
    {
        if (!ReferenceEquals(_current, controller))
            return;

        _current = null;
        Changed?.Invoke(_current);
    }
}
