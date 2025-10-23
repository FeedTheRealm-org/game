using UnityEngine;

namespace Logging {
    public enum LogType {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Handles logging modularly and usable for different logger game objects.
    /// </summary>
    [CreateAssetMenu(fileName = "Logger", menuName = "Scriptable Objects/Logger")]
    public class Logger : ScriptableObject {
        [Header("Settings")]
        [SerializeField]
        private bool _showLogs;

        [Header("Log Prefix")]
        [SerializeField]
        private string _loggerPrefix;

        [Header("Log Colors")]
        [SerializeField]
        private Color _loggerColor;

        private string _stringColor;
        private readonly string _resetColor = "</color>";

        private void OnEnable() {
            // Format necessary strings once
            _stringColor = $"<color=#{ColorUtility.ToHtmlStringRGB(_loggerColor)}>";
        }

        public void Log(object msg, Object sender, LogType type = LogType.Info) {
            if (_showLogs) {
                string formatedMsg = $"{_stringColor}{_loggerPrefix} {msg}{_resetColor}";
                switch (type) {
                    case LogType.Info:
                        Debug.Log(formatedMsg, sender);
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(formatedMsg, sender);
                        break;
                    case LogType.Error:
                        Debug.LogError(formatedMsg, sender);
                        break;
                }
            }
        }
    }
}
