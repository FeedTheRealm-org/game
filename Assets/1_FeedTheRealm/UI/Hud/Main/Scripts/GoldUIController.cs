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

        [SerializeField]
        private API.PaymentService paymentService;

        private Label _goldAmount;
        private Label _gemAmount;
        private int _currentGemBalance = -1;

        [Inject]
        [SerializeField]
        private GoldChangedEvent goldChangedEvent;

        [Inject]
        private GemBalanceChangedEvent gemBalanceChangedEvent;

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

            _gemAmount = characterData.Q<Label>("GemsAmount");
            if (_gemAmount == null)
            {
                logger.Log(
                    "[GoldUIController] GemsAmount element not found inside CharacterData.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            RefreshGemBalance();
        }

        private void OnEnable()
        {
            goldChangedEvent.OnRaised += OnGoldChanged;
            gemBalanceChangedEvent.OnRaised += OnGemBalanceChanged;
        }

        private void OnDisable()
        {
            goldChangedEvent.OnRaised -= OnGoldChanged;
            gemBalanceChangedEvent.OnRaised -= OnGemBalanceChanged;
        }

        private void OnGoldChanged(int current)
        {
            if (_goldAmount == null)
            {
                logger.Log(
                    "[GoldUIController] GoldAmount label is not initialized yet. Skipping gold update.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            _goldAmount.text = current.ToString();
        }

        private async void RefreshGemBalance()
        {
            if (_gemAmount == null)
            {
                logger.Log(
                    "[GoldUIController] GemsAmount label is not initialized yet. Skipping gem balance refresh.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            try
            {
                var response = await paymentService.GetGemBalance();
                var (success, _, balance) = response;

                if (success && balance != null)
                {
                    _currentGemBalance = balance.gems;
                    UpdateGemBalanceLabel();
                    return;
                }

                _gemAmount.text = "?";
            }
            catch (System.Exception ex)
            {
                logger.Log(
                    $"[GoldUIController] Failed to fetch gem balance: {ex.Message}",
                    this,
                    Logging.LogType.Error
                );
            }
        }

        private void UpdateGemBalanceLabel()
        {
            if (_gemAmount == null)
                return;

            _gemAmount.text = _currentGemBalance >= 0 ? _currentGemBalance.ToString("N0") : "—";
        }

        private void OnGemBalanceChanged((int currentBalance, int delta) gemBalanceUpdate)
        {
            if (_gemAmount == null)
            {
                logger.Log(
                    "[GoldUIController] GemsAmount label is not initialized yet. Skipping gem update.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            _currentGemBalance = gemBalanceUpdate.currentBalance;
            UpdateGemBalanceLabel();
        }

        public void SetGemBalance(int gems)
        {
            _currentGemBalance = gems;
            UpdateGemBalanceLabel();
        }
    }
}
