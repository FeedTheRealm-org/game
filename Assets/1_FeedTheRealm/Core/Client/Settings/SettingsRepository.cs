using System.IO;
using FTRShared.Runtime.Models.Settings;
using UnityEngine;

namespace FTR.Core.Client.Settings
{
    public class SettingsRepository
    {
        private readonly string _path = Path.Combine(
            Application.persistentDataPath,
            "settings.json"
        );

        public void Save(SettingsData data)
        {
            File.WriteAllText(_path, JsonUtility.ToJson(data, prettyPrint: true));
        }

        public SettingsData Load()
        {
            if (!File.Exists(_path))
                return null;

            try
            {
                string json = File.ReadAllText(_path);
                return JsonUtility.FromJson<SettingsData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load settings from '{_path}': {ex.Message}");

                try
                {
                    if (File.Exists(_path))
                        File.Delete(_path);
                }
                catch (System.Exception deleteEx)
                {
                    Debug.LogWarning(
                        $"Failed to delete corrupted settings file '{_path}': {deleteEx.Message}"
                    );
                }

                return null;
            }
        }

        public void Delete()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}
