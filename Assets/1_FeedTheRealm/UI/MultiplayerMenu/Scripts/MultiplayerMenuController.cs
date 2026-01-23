using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// controller that connects the UI Toolkit menu with NetworkConnectionHandler.
/// Only handles capturing UI values and delegates connection logic.
/// </summary>
public class MultiplayerMenuController : MonoBehaviour
{
    private TextField ipInput;
    private TextField portInput;
    private Button joinButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        ipInput = root.Q<TextField>("IPInput");
        portInput = root.Q<TextField>("PortInput");
        joinButton = root.Q<Button>("JoinButton");

        if (ipInput == null)
            Debug.LogError("[MultiplayerMenuController] IPInput TextField not found!");
        if (portInput == null)
            Debug.LogError("[MultiplayerMenuController] PortInput TextField not found!");
        if (joinButton == null)
            Debug.LogError("[MultiplayerMenuController] JoinButton not found!");

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

    private void ConnectToServer(string ip, ushort port)
    {
        if (NetworkConnectionHandler.Instance != null)
        {
            NetworkConnectionHandler.Instance.ConnectToServer(ip, port);
        }
        else
        {
            Debug.LogError(
                "[MultiplayerMenuController] NetworkConnectionHandler.Instance is null!"
            );
        }
    }

    private void OnJoinButtonClicked()
    {
        string ip = ipInput?.value ?? "127.0.0.1";
        string portStr = portInput?.value ?? "7777";

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

        if (!ushort.TryParse(portStr, out ushort port))
        {
            Debug.LogWarning(
                $"[MultiplayerMenuController] Invalid port: {portStr}. Using default 7777"
            );
            port = 7777;
        }

        ConnectToServer(ip, port);
    }
}
