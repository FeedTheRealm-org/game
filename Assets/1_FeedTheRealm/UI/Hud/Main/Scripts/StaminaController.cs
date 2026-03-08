using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles stamina UI updates. Attach to the same GameObject as UIDocument.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StaminaController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    private ProgressBar _staminaBar;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var characterData = root.Q<VisualElement>("CharacterData");
        if (characterData == null)
        {
            logger.Log(
                "[StaminaController] CharacterData element not found in UIDocument.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _staminaBar = characterData.Q<ProgressBar>("StaminaBar");
        if (_staminaBar == null)
        {
            logger.Log(
                "[StaminaController] StaminaBar element not found inside CharacterData.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _staminaBar.value = _staminaBar.highValue;
    }

    private void OnEnable()
    {
        StaminaView.StaminaChangedEvent += OnStaminaChanged;
    }

    private void OnDisable()
    {
        StaminaView.StaminaChangedEvent -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float current)
    {
        if (_staminaBar != null)
            _staminaBar.value = current;
    }
}
