using FTR.Core.Client.EventChannels;
using FTR.Core.Common.EventChannels;
using UnityEngine;
using VContainer;

namespace FTR.UI.Hud.QuestMenu
{
    /// <summary>
    /// Bridges the PlayerInputReader QuestEvent to the QuestMenuToggleEvent channel.
    /// Lives on the same GameObject as the HUD / main UI, or anywhere in the scene.
    ///
    /// Wiring flow:
    ///     Player presses Quest key (e.g. Q)
    ///         -> PlayerInputReader.QuestEvent
    ///             -> QuestMenuToggleBridge.OnQuestToggle()
    ///                 -> QuestMenuToggleEvent.Raise()
    ///                     -> QuestMenuController.TogglePanel()
    /// </summary>
    public class QuestMenuToggleBridge : MonoBehaviour
    {
        [Inject]
        private PlayerInputReader inputReader;

        [Inject]
        private QuestMenuToggleEvent toggleEvent;

        private void OnEnable()
        {
            if (inputReader != null)
                inputReader.QuestEvent += OnQuestToggle;
            else
                Debug.LogError("PlayerInputReader not found for QuestMenuToggleBridge", this);
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.QuestEvent -= OnQuestToggle;
            else
                Debug.LogError("PlayerInputReader not found for QuestMenuToggleBridge", this);
        }

        private void OnQuestToggle()
        {
            Debug.Log("QuestMenuToggleBridge received QuestEvent, raising QuestMenuToggleEvent");
            toggleEvent?.Raise();
        }
    }
}
