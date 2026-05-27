using System;
using System.IO;
using UnityEngine;

namespace FTR.Gameplay.Client.Cache;

public class DiskService
{
    public static string GetFullPath(string relativePath) =>
        Path.Combine(Application.persistentDataPath, relativePath);

    public bool Exists(string relativePath) => File.Exists(GetFullPath(relativePath));

    public void Write(string relativePath, byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException(
                $"[DiskService] Data is null or empty for '{relativePath}'."
            );

        string fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, data);
        Debug.Log($"[DiskService] Written {data.Length} bytes → {fullPath}");
    }

    public byte[] Read(string relativePath)
    {
        string fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[DiskService] File not found: {fullPath}");
            return null;
        }

        byte[] data = File.ReadAllBytes(fullPath);
        Debug.Log($"[DiskService] Read {data.Length} bytes ← {fullPath}");
        return data;
    }

    public void Delete(string relativePath)
    {
        string fullPath = GetFullPath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log($"[DiskService] Deleted {fullPath}");
        }
    }
}
