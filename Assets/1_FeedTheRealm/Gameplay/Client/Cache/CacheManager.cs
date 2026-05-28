using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using API;
using UnityEngine;

namespace FTR.Gameplay.Client.Cache;

[Serializable]
public class CacheEntry
{
    public string uri;
    public DateTime updatedAt;
}

public class CacheManager
{
    private readonly AssetsService assetsService;
    private readonly ModelService modelService;
    private readonly GltLoaderService gltfLoaderService;
    private readonly DiskService disk;

    private readonly Dictionary<string, CacheEntry> cacheEntries =
        new Dictionary<string, CacheEntry>();

    private const string cacheFolder = "cache/";
    private const string cacheStateFile = "cache_state.json";

    public CacheManager(
        DiskService disk,
        AssetsService assetsService,
        GltLoaderService gltfLoaderService,
        ModelService modelService
    )
    {
        this.assetsService = assetsService;
        this.modelService = modelService;
        this.gltfLoaderService = gltfLoaderService;
        this.disk = disk;

        LoadCacheState();
    }

    // Examples:
    // FULL URL: https://d3ry8oaxnx8r71.cloudfront.net/ArmorBody/f51a1c0e-07ad-4f3d-a647-82e61547aa4d.png
    // URI (unique and same for defaults): /ArmorBody/f51a1c0e-07ad-4f3d-a647-82e61547aa4d.png
    // URI(item - unique): /worlds/8ff4168b-137f-47f4-8887-f42fd3adc520/items/946a5ef4-c259-4dfa-a7b5-4493d07fa96f.png
    // BASE URL (remote): https://example.cloudfront.net
    // BASE URL (local): file://~/.config/unity3d/AtusGames/Feed the realm
    public async Task<Texture2D> GetSprite(string uri, DateTime updatedAt)
    {
        var cachePath = Path.Combine(cacheFolder, uri.TrimStart('/'));
        Debug.Log($"Getting sprite for URI: {uri}, cache path: {cachePath}");
        byte[] data = disk.Read(cachePath);
        if (data == null || ShouldInvalidateCache(uri, updatedAt))
        {
            var newTexture = await assetsService.DownloadTexture2D(uri);
            if (newTexture != null)
            {
                disk.Write(cachePath, newTexture.EncodeToPNG());
                cacheEntries[uri] = new CacheEntry { uri = uri, updatedAt = updatedAt };
                SaveCacheState();
            }
            return newTexture;
        }

        Texture2D texture = DecodeTexture(data, uri);
        return texture;
    }

    // Examples:
    // URI (unique): /worlds/8ff4168b-137f-47f4-8887-f42fd3adc520/models/362d4df0-2ae8-4c15-9521-f2cadd69f8c3/user_uploaded_name.glb
    // URI (default models - unique): /worlds/00000000-0000-0000-0000-000000000000/models/7f141c6e-09f7-4c3d-ae16-1ea31f253888/DEFAULT_CHEST_CLOSED_chest_closed.glb
    // BASE URL (remote): https://example.cloudfront.net
    // BASE URL (local): file://~/.config/unity3d/AtusGames/Feed the realm
    public async Task<GameObject> GetModel(string uri, DateTime updatedAt)
    {
        var cachePath = Path.Combine(cacheFolder, uri.TrimStart('/'));
        Debug.Log($"Getting model for URI: {uri}, cache path: {cachePath}");
        byte[] data = disk.Read(cachePath);
        if (data == null || ShouldInvalidateCache(uri, updatedAt))
        {
            var modelInfo = new ModelInfo { url = uri };
            var newModelPath = await modelService.DownloadModel(
                modelInfo,
                savePath: cachePath,
                isTemp: false
            );
            if (string.IsNullOrEmpty(newModelPath))
                return null;
            data = disk.Read(cachePath);
            if (data == null)
                return null;
            else
            {
                cacheEntries[uri] = new CacheEntry { uri = uri, updatedAt = updatedAt };
                SaveCacheState();
            }
        }

        GameObject model = await gltfLoaderService.LoadModel(data);
        return model;
    }

    private Texture2D DecodeTexture(byte[] data, string uri)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);

        if (!texture.LoadImage(data))
        {
            UnityEngine.Object.Destroy(texture);
            throw new System.Exception($"Failed to decode image data for URI: {uri}");
        }

        return texture;
    }

    private bool ShouldInvalidateCache(string uri, DateTime updatedAt)
    {
        if (cacheEntries.TryGetValue(uri, out var entry))
            return updatedAt > entry.updatedAt; // TODO: consider deleting file instead of just overwriting it
        return true;
    }

    private void SaveCacheState()
    {
        var cacheState = JsonUtility.ToJson(cacheEntries);
        disk.Write(cacheStateFile, System.Text.Encoding.UTF8.GetBytes(cacheState));
    }

    private void LoadCacheState()
    {
        byte[] data = disk.Read(cacheStateFile);
        if (data == null)
            return;

        var cacheStateJson = System.Text.Encoding.UTF8.GetString(data);
        var loadedEntries = JsonUtility.FromJson<Dictionary<string, CacheEntry>>(cacheStateJson);
        foreach (var entry in loadedEntries)
        {
            cacheEntries[entry.Key] = entry.Value;
        }
    }

    private void ClearCache()
    {
        foreach (var entry in cacheEntries.Values)
        {
            disk.Delete(Path.Combine(cacheFolder, entry.uri));
        }
        cacheEntries.Clear();
        disk.Delete(cacheStateFile);
    }
}
