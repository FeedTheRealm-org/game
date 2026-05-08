using System;
using System.Collections.Generic;
using System.IO;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientWorldAreaLoader : ILoader
    {
        private readonly MaterialService materialService;
        private readonly Logging.Logger logger;
        private const string Seperator = "@";

        public ClientWorldAreaLoader(MaterialService materialService, Logging.Logger logger)
        {
            this.materialService = materialService;
            this.logger = logger;
        }

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            ZoneController zoneController =
                UnityEngine.Object.FindFirstObjectByType<ZoneController>();
            if (zoneController == null)
            {
                logger.Log(
                    "[ClientWorldAreaLoader] ZoneController not found in scene.",
                    Logging.LogType.Error
                );
                return;
            }

            var zoneAreaData = zoneData?.zoneAreaData;
            if (zoneAreaData == null)
            {
                logger.Log(
                    "[ClientWorldAreaLoader] No zone area data, skipping material load.",
                    Logging.LogType.Warning
                );
                return;
            }

            var materials = await materialService.GetMaterialsListAsync(worldId);
            if (materials == null || materials.Length == 0)
            {
                logger.Log(
                    "[ClientWorldAreaLoader] No materials found for world.",
                    Logging.LogType.Warning
                );
                return;
            }

            await ApplyMaterial(
                materials,
                zoneAreaData.zoneMaterialId,
                ZoneTextureType.Ground,
                material =>
                {
                    zoneController.ChangeMaterial(material, zoneAreaData.zoneMaterialId);
                    zoneController.ApplyTextureGranularity(zoneAreaData.textureGranularity);
                }
            );

            await ApplyMaterial(
                materials,
                zoneAreaData.skyboxMaterialId,
                ZoneTextureType.Skybox,
                material =>
                    zoneController.SetSkyboxMaterial(material, zoneAreaData.skyboxMaterialId)
            );
        }

        private async UniTask ApplyMaterial(
            MaterialResponse[] materials,
            string materialId,
            ZoneTextureType type,
            Action<Material> onLoaded
        )
        {
            if (string.IsNullOrEmpty(materialId))
                return;

            var response = FindMaterial(materials, materialId);
            if (response == null)
            {
                logger.Log(
                    $"[ClientWorldAreaLoader] Material '{materialId}' not found on server.",
                    Logging.LogType.Warning
                );
                return;
            }

            string tempPath = await materialService.DownloadMaterialAsync(response);
            if (tempPath == null)
                return;

            try
            {
                var material = LoadMaterial(tempPath, type);
                if (material != null)
                    onLoaded(material);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        // Matches either by extracted UUID (custom) or by name (default)
        private MaterialResponse FindMaterial(MaterialResponse[] materials, string materialId)
        {
            if (string.IsNullOrEmpty(materialId))
                return null;

            string[] parts = materialId.Split(Seperator);
            bool isDefault = parts[0] == "Default";

            if (isDefault)
            {
                // For default materials match by name segment after '@'
                string materialName = parts.Length > 1 ? parts[1] : null;
                if (materialName == null)
                    return null;
                foreach (var m in materials)
                    if (string.Equals(m.name, materialName, StringComparison.OrdinalIgnoreCase))
                        return m;
            }
            else
            {
                // For custom materials match by id (the UUID before '@')
                string id = parts[0];
                foreach (var m in materials)
                    if (string.Equals(m.id, id, StringComparison.OrdinalIgnoreCase))
                        return m;
            }

            return null;
        }

        private Material LoadMaterial(string texturePath, ZoneTextureType type)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(texturePath);
                var texture = new Texture2D(2, 2);
                if (!texture.LoadImage(bytes))
                {
                    logger.Log(
                        $"[ClientWorldAreaLoader] Failed to load texture from '{texturePath}'.",
                        Logging.LogType.Error
                    );
                    return null;
                }

                Material material;
                if (type == ZoneTextureType.Skybox)
                {
                    material = new Material(Shader.Find("Skybox/Panoramic"));
                    material.SetTexture("_MainTex", texture);
                }
                else
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.mainTexture = texture;
                }
                return material;
            }
            catch (Exception ex)
            {
                logger.Log(
                    $"[ClientWorldAreaLoader] Error loading material: {ex.Message}",
                    Logging.LogType.Error
                );
                return null;
            }
        }
    }
}
