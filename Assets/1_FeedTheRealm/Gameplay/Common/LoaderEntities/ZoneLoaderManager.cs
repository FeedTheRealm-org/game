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
        private UniTask<bool> loadWorldTask;
        private bool hasLoadWorldTask;

        public bool LastLoadSucceeded { get; private set; }

        public abstract string GetWorldId();
        public abstract string GetAccessToken();

        public abstract int GetZoneId();

        public async UniTask<bool> LoadWorld()
        {
            if (config.DEBUG_DoNotLoadWorld)
            {
                LastLoadSucceeded = true;
                return true;
            }

            if (!hasLoadWorldTask)
            {
                hasLoadWorldTask = true;
                loadWorldTask = Initialize();
            }

            LastLoadSucceeded = await loadWorldTask;
            return LastLoadSucceeded;
        }

        // --- Private methods --- //
        private async UniTask<bool> Initialize()
        {
            try
            {
                (string worldId, string accessToken, int zoneId) = (
                    GetWorldId(),
                    GetAccessToken(),
                    GetZoneId()
                );
                ValidateArgs(worldId, "worldId");
                ValidateArgs(accessToken, "accessToken");
                ValidateArgs(zoneId, "zoneId");
                logger.Log(
                    $"[ZONE-LOAD] Starting zone loading with Zone ID: {zoneId} | World ID: {worldId} | Access Token: {accessToken}"
                );
                await Load(zoneId, worldId, accessToken);
                return true;
            }
            catch (System.Exception ex)
            {
                logger.Log(
                    $"World could not be loaded: {ex.Message}\n{ex.StackTrace}",
                    Logging.LogType.Error
                );
                return false;
            }
        }

        private async UniTask Load(int zoneId, string worldId, string accessToken)
        {
            if (loaders == null || loaders.Count == 0)
                return;

            ZoneData zoneData =
                await LoadZoneData(zoneId, worldId, accessToken)
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

        private async UniTask<ZoneData> LoadZoneData(int zoneId, string worldId, string accessToken)
        {
            (ZoneData data, string errorMessage, long responseCode) = await zoneService.GetZoneData(
                worldId,
                zoneId,
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

        private void ValidateArgs(int args, string argName)
        {
            if (args <= 0)
            {
                throw new System.ArgumentException(
                    $"Invalid value for: {argName}. Value must be greater than 0."
                );
            }
        }
    }
}
