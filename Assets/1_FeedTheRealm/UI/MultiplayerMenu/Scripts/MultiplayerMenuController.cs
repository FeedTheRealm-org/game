using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controlador simple que conecta el menú UI Toolkit con NetworkConnectionHandler.
/// Solo maneja la captura de valores del UI y delega la lógica de conexión.
/// </summary>
public class MultiplayerMenuController : MonoBehaviour
{
    private TextField ipInput;
    private TextField portInput;
    private Button joinButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Obtener referencias a los elementos del UI
        ipInput = root.Q<TextField>("IPInput");
        portInput = root.Q<TextField>("PortInput");
        joinButton = root.Q<Button>("JoinButton");

        // Validar que los elementos existen
        if (ipInput == null)
            Debug.LogError("[MultiplayerMenuController] IPInput TextField not found!");
        if (portInput == null)
            Debug.LogError("[MultiplayerMenuController] PortInput TextField not found!");
        if (joinButton == null)
            Debug.LogError("[MultiplayerMenuController] JoinButton not found!");

        // Asignar evento al botón
        if (joinButton != null)
        {
            joinButton.clicked += OnJoinButtonClicked;
        }
    }

    private void OnDisable()
    {
        if (joinButton != null)
        {
            joinButton.clicked -= OnJoinButtonClicked;
        }
    }

    private void OnJoinButtonClicked()
    {
        // Obtener valores del UI
        string ip = ipInput?.value ?? "127.0.0.1";
        string portStr = portInput?.value ?? "7777";

        // Validar que no sean valores vacíos
        if (string.IsNullOrWhiteSpace(ip))
        {
            Debug.LogWarning("[MultiplayerMenuController] ⚠️ IP is empty, using default 127.0.0.1");
            ip = "127.0.0.1";
        }
        
        if (string.IsNullOrWhiteSpace(portStr))
        {
            Debug.LogWarning("[MultiplayerMenuController] ⚠️ Port is empty, using default 7777");
            portStr = "7777";
        }

        // Validar y parsear el puerto
        if (!ushort.TryParse(portStr, out ushort port))
        {
            Debug.LogWarning($"[MultiplayerMenuController] Invalid port: {portStr}. Using default 7777");
            port = 7777;
        }

        // Delegar la conexión al NetworkConnectionHandler
        if (NetworkConnectionHandler.Instance != null)
        {
            NetworkConnectionHandler.Instance.ConnectToServer(ip, port);
        }
        else
        {
            Debug.LogError("[MultiplayerMenuController] NetworkConnectionHandler.Instance is null!");
        }
    }
}
