using UnityEngine;

/// <summary>
/// Manages the game scene initialization, UI, and cursor state.
/// Ensures the cursor is properly locked for gameplay.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("HUD Configuration")]
    [SerializeField]
    private SettingsMenuController settingsMenu;

    [SerializeField]
    [Tooltip(
        "InventoryController para el jugador local. Los jugadores remotos no usan esta referencia."
    )]
    private InventoryController inventoryMenu;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    public PlayerInputReader inputReader;

    private void Start()
    {
        logger.Log("Game scene loaded successfully", this);

        // Ya no es necesario desactivar el GameObject del inventario
        // El InventoryController se encarga de ocultar su UI internamente
        if (inventoryMenu != null)
        {
            logger.Log("Inventory reference found in GameSceneManager", this);
        }

        if (inputReader != null)
        {
            inputReader.CursorToggleEvent += toggleSettings;
            inputReader.InventoryEvent += toggleInventory;
            logger.Log("GameSceneManager subscribed to input events", this);
        }
        else
        {
            logger.Log("Input reader is null in GameSceneManager", this, Logging.LogType.Warning);
        }

        InitializeCursor();
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.CursorToggleEvent -= toggleSettings;
            inputReader.InventoryEvent -= toggleInventory;
        }
    }

    private void toggleSettings()
    {
        if (inventoryMenu != null && inventoryMenu.IsOpen())
        {
            return;
        }
        settingsMenu.ToggleSettings();
    }

    private void toggleInventory()
    {
        if (settingsMenu.IsOpen())
        {
            logger.Log("Inventory toggle blocked - Settings menu is open", this);
            return;
        }

        if (inventoryMenu == null)
        {
            logger.Log(
                "InventoryController no está asignado en GameSceneManager",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        logger.Log(
            $"Toggling inventory - Current state: {(inventoryMenu.IsOpen() ? "Open" : "Closed")}",
            this
        );
        inventoryMenu.ToggleInventory();
    }

    /// <summary>
    /// Obtiene el InventoryController del jugador local
    /// </summary>
    public InventoryController GetLocalPlayerInventory()
    {
        return inventoryMenu;
    }

    /// <summary>
    /// Initializes the cursor state for gameplay.
    /// Called during Start to ensure it runs after all other initializations.
    /// </summary>
    private void InitializeCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        logger.Log("Cursor initialized - Locked for gameplay", this);
    }
}
