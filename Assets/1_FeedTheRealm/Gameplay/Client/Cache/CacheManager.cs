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

    public async Task<Texture2D> GetSprite(string uri, DateTime updatedAt)
    {
        string relativePath = ImageRelativePath(uri);

        byte[] data = disk.Read(relativePath);
        if (data == null)
        {
            var newTexture = await assetsService.DownloadTexture2D(uri);
            if (newTexture != null)
                disk.Write(ImageRelativePath(uri), newTexture.EncodeToPNG());
            return newTexture;
        }

        Texture2D texture = await Task.Run(() => DecodeTexture(data, uri));
        return texture;
    }

    public async Task<GameObject> GetModel(string uri, DateTime updatedAt)
    {
        string relativePath = ModelRelativePath(uri);

        byte[] data = disk.Read(relativePath);
        if (data == null)
        {
            var newModelPath = await modelService.DownloadModel(uri, isTemp: false);
            if (string.IsNullOrEmpty(newModelPath))
                return null;
            data = disk.Read(relativePath);
            if (data == null)
                return null;
        }

        GameObject model = await gltfLoaderService.LoadModel(data);
        return model;
    }

    private static string ImageRelativePath(string uri) => $"Images/{UriToFileName(uri)}.png";

    private static string ModelRelativePath(string uri) => $"Models/{UriToFileName(uri)}.glb";

    private static string UriToFileName(string uri)
    {
        byte[] hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(uri));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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
