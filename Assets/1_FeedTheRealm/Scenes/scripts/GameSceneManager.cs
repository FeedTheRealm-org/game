using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    [Header("UI Configuration")]
    public Button disconnectButton;
    public GameObject returnToMenuPanel;

    private void Start()
    {
        Debug.Log("Escena del juego cargada correctamente");

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DisconnectAndReturnToMenu();
        }
    }
}