using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

/// <summary>
/// Maneja la conexión del cliente al servidor dedicado.
/// Configura el transporte con IP y puerto antes de conectar.
/// </summary>
public class ClientConnectionManager : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string defaultIP = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;
    
    [Header("UI References (Optional)")]
    [Tooltip("Input field para la IP del servidor. Si está vacío, usa defaultIP")]
    [SerializeField] private TMP_InputField ipInputField;
    
    [Tooltip("Input field para el puerto del servidor. Si está vacío, usa defaultPort")]
    [SerializeField] private TMP_InputField portInputField;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private void Awake()
    {
        // Inicializar campos de input con valores por defecto
        if (ipInputField != null)
        {
            ipInputField.text = defaultIP;
        }
        
        if (portInputField != null)
        {
            portInputField.text = defaultPort.ToString();
        }
    }

    /// <summary>
    /// Conecta al servidor usando los valores de los input fields o valores por defecto
    /// </summary>
    public void ConnectToServer()
    {
        string ip = GetIPAddress();
        ushort port = GetPort();
        
        ConnectToServer(ip, port);
    }

    /// <summary>
    /// Conecta al servidor con IP y puerto específicos
    /// </summary>
    /// <param name="ipAddress">Dirección IP del servidor</param>
    /// <param name="port">Puerto del servidor</param>
    public void ConnectToServer(string ipAddress, ushort port)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ClientConnectionManager] NetworkManager.Singleton is null!");
            return;
        }

        // Verificar que no estamos ya conectados
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("[ClientConnectionManager] Already connected or connecting!");
            return;
        }

        LogInfo($"Attempting to connect to server at {ipAddress}:{port}");

        // Configurar el transporte con la IP y puerto del servidor
        if (ConfigureTransport(ipAddress, port))
        {
            // Iniciar el cliente
            bool success = NetworkManager.Singleton.StartClient();
            
            if (success)
            {
                LogInfo($"✅ Client connection initiated to {ipAddress}:{port}");
            }
            else
            {
                Debug.LogError($"[ClientConnectionManager] ❌ Failed to start client!");
            }
        }
    }

    /// <summary>
    /// Configura el Unity Transport con la dirección IP y puerto del servidor
    /// </summary>
    private bool ConfigureTransport(string ipAddress, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        if (transport == null)
        {
            Debug.LogError("[ClientConnectionManager] UnityTransport component not found on NetworkManager!");
            return false;
        }

        // Configurar los datos de conexión
        transport.ConnectionData.Address = ipAddress;
        transport.ConnectionData.Port = port;
        
        LogInfo($"UnityTransport configured: {ipAddress}:{port}");
        return true;
    }

    /// <summary>
    /// Desconecta del servidor
    /// </summary>
    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            LogInfo("Disconnecting from server...");
            NetworkManager.Singleton.Shutdown();
        }
    }

    #region Input Field Helpers

    /// <summary>
    /// Obtiene la dirección IP del input field o usa el valor por defecto
    /// </summary>
    private string GetIPAddress()
    {
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            return ipInputField.text.Trim();
        }
        
        return defaultIP;
    }

    /// <summary>
    /// Obtiene el puerto del input field o usa el valor por defecto
    /// </summary>
    private ushort GetPort()
    {
        if (portInputField != null && !string.IsNullOrWhiteSpace(portInputField.text))
        {
            if (ushort.TryParse(portInputField.text, out ushort port))
            {
                return port;
            }
            else
            {
                Debug.LogWarning($"[ClientConnectionManager] Invalid port in input field: {portInputField.text}. Using default: {defaultPort}");
            }
        }
        
        return defaultPort;
    }

    #endregion

    #region Public Getters/Setters

    /// <summary>
    /// Establece la IP por defecto (útil para testing o configuración programática)
    /// </summary>
    public void SetDefaultIP(string ip)
    {
        defaultIP = ip;
        if (ipInputField != null)
        {
            ipInputField.text = ip;
        }
    }

    /// <summary>
    /// Establece el puerto por defecto (útil para testing o configuración programática)
    /// </summary>
    public void SetDefaultPort(ushort port)
    {
        defaultPort = port;
        if (portInputField != null)
        {
            portInputField.text = port.ToString();
        }
    }

    /// <summary>
    /// Verifica si el cliente está conectado
    /// </summary>
    public bool IsConnected()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    }

    #endregion

    #region Logging

    private void LogInfo(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[ClientConnectionManager] {message}");
        }
    }

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        // Cleanup si es necesario
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            LogInfo("ClientConnectionManager destroyed - disconnecting...");
        }
    }

    #endregion
}
