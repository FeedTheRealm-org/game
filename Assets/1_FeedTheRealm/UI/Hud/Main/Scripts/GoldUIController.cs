using FTR.Core.Client.EventChannels.Gold;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.Main
{
    /// <summary>
    /// Handles gold UI updates. Attach to the same GameObject as UIDocument.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GoldUIController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        private Label _goldAmount;

        [Inject]
        [SerializeField]
        private GoldChangedEvent goldChangedEvent;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var characterData = root.Q<VisualElement>("CharacterData");
            if (characterData == null)
            {
                logger.Log(
                    "[GoldUIController] CharacterData element not found in UIDocument.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _goldAmount = characterData.Q<Label>("GoldAmount");
            if (_goldAmount == null)
            {
                logger.Log(
                    "[GoldUIController] GoldAmount element not found inside CharacterData.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
        }

        private void OnEnable()
        {
            goldChangedEvent.OnRaised += OnGoldChanged;
        }

        private void OnDisable()
        {
            goldChangedEvent.OnRaised -= OnGoldChanged;
        }

        private void OnGoldChanged(int current)
        {
            if (_goldAmount != null)
                logger.Log($"[GoldUIController] Updating gold amount to {current}", this);
            _goldAmount.text = current.ToString();
        }
    }
}
