using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the game scene initialization, UI, and cursor state.
/// Ensures the cursor is properly locked for gameplay.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("UI Configuration")]
    public Button disconnectButton;
    public GameObject returnToMenuPanel;

    [SerializeField] private Logging.Logger logger;

    private void Start()
    {
        logger.Log("Game scene loaded successfully", this);

        // Inicializar el cursor para gameplay
        InitializeCursor();

        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(DisconnectAndReturnToMenu);
        }

        if (returnToMenuPanel != null)
        {
            returnToMenuPanel.SetActive(false);
        }
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

    public void DisconnectAndReturnToMenu()
    {
        // Liberar el cursor antes de volver al menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        logger.Log("Cursor unlocked - Returning to menu", this);
        
        NetworkSceneManager.Instance.DisconnectAndReturnToMenu();
    }
}