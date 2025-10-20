// Script: SimpleMultiplayerMenu.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class SimpleMultiplayerMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;
    public TextMeshProUGUI statusText;
    public GameObject menuPanel;

    [Header("Multiplayer Settings")]
    public string ipAddress = "127.0.0.1";
    public ushort port = 7777;

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        
        UpdateStatus("Menú listo - Elige una opción");
    }

    public void StartHost()
    {
        UpdateStatus("Iniciando como Host...");
        Debug.Log("Iniciando servidor en puerto: " + port);
        
        NetworkManager.Singleton.StartHost();
    
        // Ocultar menú cuando se conecte
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
            {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                menuPanel.SetActive(false);
                UpdateStatus("Host ejecutándose");
            }
        };
    }

    public void StartClient()
    {
        UpdateStatus("Conectando como Cliente...");
        Debug.Log("Conectando a: " + ipAddress + ":" + port);
        
        NetworkManager.Singleton.StartClient();
    
        // Ocultar menú cuando se conecte
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                menuPanel.SetActive(false);
                UpdateStatus("Conectado al servidor");
            }
        };
    }

    private void OnHostStarted()
    {
        UpdateStatus("Host ejecutándose en puerto: " + port);
        menuPanel.SetActive(false); // Ocultar menú cuando esté conectado
    }

    private void OnClientConnected()
    {
        UpdateStatus("Conectado al servidor");
        menuPanel.SetActive(false); // Ocultar menú cuando esté conectado
    }

    public void ShowMenu()
    {
        menuPanel.SetActive(true);
        UpdateStatus("Menú disponible");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log("Multiplayer Status: " + message);
    }
}