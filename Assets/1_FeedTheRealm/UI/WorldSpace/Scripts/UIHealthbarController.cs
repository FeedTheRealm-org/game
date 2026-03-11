using System;
using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Systems.Status;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIHealthbar : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    /// <summary>
    /// Assign the HealthChangedEvent SO asset from the ClientEventRegistry.
    /// </summary>
    [SerializeField]
    private HealthChangedEvent healthChangedEvent;

    // Containers
    private VisualElement _root;

    // Progress Bars
    private ProgressBar _healthBar;

    // Use MonoBehaviour and reflection to avoid a hard compile-time dependency on Mirror's NetworkIdentity.
    private MonoBehaviour _networkIdentity;

    /// <summary>True when this bar belongs to the local player (who has a HUD instead).</summary>
    private bool _isLocalPlayer;

    void Start()
    {
        // _root = GetComponent<UIDocument>().rootVisualElement;

        // _healthBar = _root.Q<ProgressBar>("WorldHealthBar");
        // if (_healthBar == null)
        // {
        //     logger.Log("WorldHealthBar not found in UIDocument.", this, Logging.LogType.Error);
        //     return;
        // }

        // // Resolve the owning NetworkIdentity (UIHealthbar lives on a child object).
        // // We find it by name and hold it as a MonoBehaviour to avoid requiring Mirror at compile time.
        // _networkIdentity = null;
        // var behaviours = GetComponentsInParent<MonoBehaviour>();
        // foreach (var b in behaviours)
        // {
        //     if (b.GetType().Name == "NetworkIdentity")
        //     {
        //         _networkIdentity = b;
        //         break;
        //     }
        // }

        // // The local player already has a dedicated HUD; hide the floating bar for them.
        // _isLocalPlayer = false;
        // if (_networkIdentity != null)
        // {
        //     var prop = _networkIdentity.GetType().GetProperty("isLocalPlayer");
        //     if (prop != null)
        //     {
        //         var val = prop.GetValue(_networkIdentity);
        //         _isLocalPlayer = val is bool && (bool)val;
        //     }
        // }
        // if (_isLocalPlayer)
        // {
        //     _root.style.display = DisplayStyle.None;
        //     return;
        // }

        // // Start hidden; visibility is toggled by HandleHealthChange.
        // _healthBar.value = _healthBar.highValue;
        // _root.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        if (healthChangedEvent != null)
            healthChangedEvent.OnRaised += HandleHealthChange;
    }

    private void OnDisable()
    {
        if (healthChangedEvent != null)
            healthChangedEvent.OnRaised -= HandleHealthChange;
    }

    /// <summary>
    /// Handles a global HealthChangedEvent, filtering to only react when the
    /// event belongs to the entity that owns this health bar.
    /// </summary>
    private void HandleHealthChange(HealthChangedData data)
    {
        // Ignore events for other entities or when bar is not yet initialised.
        if (_isLocalPlayer || _healthBar == null || _networkIdentity == null)
            return;

        // Compare NetId via reflection to avoid a direct compile-time dependency on Mirror's type.
        if (_networkIdentity == null)
            return;
        ulong ownerNetId = 0;
        {
            var prop = _networkIdentity.GetType().GetProperty("netId");
            if (prop != null)
            {
                var val = prop.GetValue(_networkIdentity);
                try
                {
                    ownerNetId = Convert.ToUInt64(val);
                }
                catch
                {
                    ownerNetId = 0;
                }
            }
        }
        if (data.NetId != ownerNetId)
            return;

        // Map current health to the progress bar's 0-highValue range.
        _healthBar.value =
            data.MaxHealth > 0
                ? data.CurrentHealth / (float)data.MaxHealth * _healthBar.highValue
                : 0;

        if (_healthBar.value < 0)
            _healthBar.value = 0;

        ToggleUIVisibility();
    }

    /// <summary>
    /// Hidden when at full health, visible otherwise.
    /// </summary>
    private void ToggleUIVisibility()
    {
        bool isFull = _healthBar.value >= _healthBar.highValue;
        _root.style.display = isFull ? DisplayStyle.None : DisplayStyle.Flex;
    }
}
