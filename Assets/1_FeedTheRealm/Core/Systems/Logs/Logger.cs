using UnityEngine;

public enum LogType {
    Info,
    Warning,
    Error
}

/// <summary>
/// Handles logging modularly and usable for different logger game objects.
/// </summary>
[CreateAssetMenu(fileName = "Logger", menuName = "Logging/Logger")]
public class Logger : ScriptableObject {
    [Header("Settings")]
    [SerializeField]
    private bool _showLogs;

    public void Log(object msg, Object sender, LogType type = LogType.Info) {
        if (_showLogs) {
            switch (type) {
                case LogType.Info:
                    Debug.Log(msg, sender);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(msg, sender);
                    break;
                case LogType.Error:
                    Debug.LogError(msg, sender);
                    break;
            }
        }
    }
}
