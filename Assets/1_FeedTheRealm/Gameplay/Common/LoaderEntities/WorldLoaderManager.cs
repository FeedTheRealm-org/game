using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using VContainer;

namespace FTR.Gameplay.Common.LoaderEntities
{
    public abstract class ZoneLoaderManager
    {
        [Inject]
        protected Config config;

        [Inject]
        private readonly ZoneService zoneService;

        [Inject]
        private readonly WorldService worldService;

        [Inject]
        private readonly Logging.Logger logger;

        public List<ILoader> loaders;

        public abstract string GetWorldId();
        public abstract string GetAccessToken();

        public async UniTask LoadWorld()
        {
            if (!config.DoNotLoadWorld)
                await Initialize();
        }

        // --- Private methods --- //
        private async UniTask Initialize()
        {
            try
            {
                (string worldId, string accessToken) = (GetWorldId(), GetAccessToken());
                ValidateArgs(worldId, "worldId");
                ValidateArgs(accessToken, "accessToken");
                logger.Log(
                    $"[ZONE-LOAD] Starting zone loading with Zone ID: {worldId} | Access Token: {accessToken}"
                );
                await Load(worldId, accessToken);
            }
            catch (System.Exception ex)
            {
                logger.Log(
                    $"World could not be loaded: {ex.Message}\n{ex.StackTrace}",
                    Logging.LogType.Error
                );
            }
        }

        private async UniTask Load(string worldId, string accessToken)
        {
            if (loaders == null || loaders.Count == 0)
                return;

            ZoneData zoneData =
                await LoadZoneData(worldId, accessToken)
                ?? throw new System.InvalidOperationException("Failed to load zone data");

            CreatablesData creatablesData =
                await LoadCreatablesData(worldId, accessToken)
                ?? throw new System.InvalidOperationException("Failed to load creatables data");

            for (int i = 0; i < loaders.Count; i++)
            {
                ILoader loader = loaders[i];
                logger.Log($"Loading {loader.GetType().Name} | {i + 1} / {loaders.Count}");
                await loader.Load(worldId, zoneData, creatablesData);
            }
            logger.Log("World loading complete!");
        }

        private async UniTask<ZoneData> LoadZoneData(string worldId, string accessToken)
        {
            (ZoneData data, string errorMessage, long responseCode) = await zoneService.GetZoneData(
                worldId,
                1,
                accessToken
            );
            if (data == null || !string.IsNullOrEmpty(errorMessage))
                throw new System.Exception(
                    $"Failed to load world data: {errorMessage} (Response code: {responseCode})"
                );
            return data;
        }

        private async UniTask<CreatablesData> LoadCreatablesData(string worldId, string accessToken)
        {
            // TODO: this also loads the world data, we should optimize this by creating a new endpoint that only returns the creatables data.
            var (_, creatablesData, errorMessage, responseCode) = await worldService.GetWorld(
                worldId,
                accessToken
            );

            if (creatablesData == null || !string.IsNullOrEmpty(errorMessage))
                throw new System.Exception(
                    $"Failed to load creatables creatablesData: {errorMessage} (Response code: {responseCode})"
                );
            return creatablesData;
        }

        private void ValidateArgs(string args, string argName)
        {
            if (args == null || string.IsNullOrEmpty(args))
            {
                throw new System.ArgumentException($"Empty or null value for: {argName}");
            }
        }
    }
}
