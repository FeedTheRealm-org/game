using UnityEngine;

/// <summary>
/// Manages the game scene initialization, UI, and cursor state.
/// Ensures the cursor is properly locked for gameplay.
/// </summary>
public class GameSceneManager : MonoBehaviour {
    [Header("HUD Configuration")]
    [SerializeField]
    private SettingsMenuController settingsMenu;

    [SerializeField]
    private InventoryController inventoryMenu;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    public PlayerInputReader inputReader;

    private void Start() {
        logger.Log("Game scene loaded successfully", this);
        if (inputReader != null) {
            inputReader.CursorToggleEvent += toggleSettings;
            inputReader.InventoryEvent += toggleInventory;
        } else {
            logger.Log("Input reader is null in GameSceneManager", this, Logging.LogType.Warning);
        }

        InitializeCursor();
    }

    private void OnDestroy() {
        if (inputReader != null) {
            inputReader.CursorToggleEvent -= toggleSettings;
            inputReader.InventoryEvent -= toggleInventory;
        }
    }

    private void toggleSettings() {
        if (inventoryMenu.IsOpen()) {
            return;
        }
        settingsMenu.ToggleSettings();
    }

    private void toggleInventory() {
        if (settingsMenu.IsOpen()) {
            return;
        }
        inventoryMenu.ToggleInventory();
    }

    /// <summary>
    /// Initializes the cursor state for gameplay.
    /// Called during Start to ensure it runs after all other initializations.
    /// </summary>
    private void InitializeCursor() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        logger.Log("Cursor initialized - Locked for gameplay", this);
    }

    /// <summary>
    /// TODO: remove deprecated
    /// </summary>
    public void DisconnectAndReturnToMenu() {
        // Liberar el cursor antes de volver al menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        logger.Log("Cursor unlocked - Returning to menu", this);

        NetworkSceneManager.Instance.DisconnectAndReturnToMenu();
    }
}
