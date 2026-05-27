using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API;
using GLTFast;
using UnityEngine;

namespace FTR.Gameplay.Client.Cache;

public class CacheManager
{
    private readonly AssetsService assetsService;
    private readonly ModelService modelService;
    private readonly GltLoaderService gltfLoaderService;
    private readonly DiskService disk;

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
    }

    // Example:
    // FULL URL: https://d3ry8oaxnx8r71.cloudfront.net/ArmorBody/f51a1c0e-07ad-4f3d-a647-82e61547aa4d.png
    // URI: /ArmorBody/f51a1c0e-07ad-4f3d-a647-82e61547aa4d.png
    // BASE URL: https://d3ry8oaxnx8r71.cloudfront.net
    // BASE URI: file://~/.config/unity3d/AtusGames/Feed the realm
    public async Task<Texture2D> GetSprite(string uri, DateTime updatedAt)
    {
        byte[] data = disk.Read(uri);
        if (data == null)
        {
            var newTexture = await assetsService.DownloadTexture2D(uri);
            if (newTexture != null)
                disk.Write(uri, newTexture.EncodeToPNG());
            return newTexture;
        }

        Texture2D texture = await Task.Run(() => DecodeTexture(data, uri));
        return texture;
    }

    public async Task<GameObject> GetModel(string uri, DateTime updatedAt)
    {
        byte[] data = disk.Read(uri);
        if (data == null)
        {
            var newModelPath = await modelService.DownloadModel(uri, isTemp: false);
            if (string.IsNullOrEmpty(newModelPath))
                return null;
            data = disk.Read(uri);
            if (data == null)
                return null;
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
}
