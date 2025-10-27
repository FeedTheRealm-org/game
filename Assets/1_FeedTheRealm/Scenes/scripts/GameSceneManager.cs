using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Agregar esto

public class GameSceneManager : MonoBehaviour
{
    [Header("UI Configuration")]
    public Button disconnectButton;
    public GameObject returnToMenuPanel;

    [SerializeField] private Logging.Logger logger;

    // Agregar controles para el Input System
    private PlayerControls uiControls;

    private void Start()
    {
        logger.Log("Game scene loaded successfully", this);

        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(DisconnectAndReturnToMenu);
        }

        if (returnToMenuPanel != null)
        {
            returnToMenuPanel.SetActive(false);
        }
    }

    public void DisconnectAndReturnToMenu()
    {
        NetworkSceneManager.Instance.DisconnectAndReturnToMenu();
    }
}