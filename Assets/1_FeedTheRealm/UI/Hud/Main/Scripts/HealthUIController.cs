using System;
using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Systems.Status;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Handles health UI updates. Attach to the same GameObject as UIDocument.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HealthUIController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    private ProgressBar _healthBar;
    private ulong _ownerNetId = 0;

    [Inject]
    private HealthChangedEvent healthChangedEvent;
    private bool _isLocalPlayer;
    private MonoBehaviour _networkIdentity;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var characterData = root.Q<VisualElement>("CharacterData");
        if (characterData == null)
        {
            logger.Log(
                "[HealthController] CharacterData element not found in UIDocument.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _healthBar = characterData.Q<ProgressBar>("HealthBar");
        if (_healthBar == null)
        {
            logger.Log(
                "[HealthController] HealthBar element not found inside CharacterData.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _healthBar.value = _healthBar.highValue;
        var behaviours = GetComponentsInParent<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b.GetType().Name == "NetworkIdentity")
            {
                _networkIdentity = b;
                break;
            }
        }

        _isLocalPlayer = false;

        if (_networkIdentity != null)
        {
            var prop = _networkIdentity.GetType().GetProperty("isLocalPlayer");
            _isLocalPlayer = prop != null && (bool)prop.GetValue(_networkIdentity);
        }

        if (_networkIdentity != null)
        {
            var propNetId = _networkIdentity.GetType().GetProperty("netId");
            if (propNetId != null)
            {
                var val = propNetId.GetValue(_networkIdentity);
                try
                {
                    _ownerNetId = Convert.ToUInt64(val);
                }
                catch
                {
                    _ownerNetId = 0;
                }
            }
        }
    }

    private void OnEnable()
    {
        healthChangedEvent.OnRaised += OnHealthChanged;
    }

    private void OnDisable()
    {
        healthChangedEvent.OnRaised -= OnHealthChanged;
    }

    private void OnHealthChanged(HealthChangedData data)
    {
        // Ignore events for other entities or when bar is not yet initialised.
        if (_healthBar == null || _networkIdentity == null)
            return;

        // Ensure this UI belongs to the local player and matches the event's NetId.
        if (!_isLocalPlayer)
            return;

        ulong ownerNetId = _ownerNetId;
        if (ownerNetId == 0)
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
            _ownerNetId = ownerNetId;
        }

        if (data.NetId != ownerNetId)
        {
            //Debug.Log($"[HealthUIController] Ignoring health event for NetId={data.NetId}, ownerNetId={ownerNetId}");
            return;
        }

        // Map current health to the progress bar's 0-highValue range.
        _healthBar.value =
            data.MaxHealth > 0
                ? data.CurrentHealth / (float)data.MaxHealth * _healthBar.highValue
                : 0;

        if (_healthBar.value < 0)
            _healthBar.value = 0;
    }
}
