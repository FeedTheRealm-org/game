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

            return JsonUtility.FromJson<SettingsData>(File.ReadAllText(_path));
        }

        public void Delete()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}
