using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Core.Server.Utils;
using FTRShared.Runtime.Models;
using VContainer.Unity;

namespace FTR.Gameplay.Server.WorldLoader
{
    public sealed class ServerWorldLoader : IStartable
    {
        private WorldService worldService;
        private Logging.Logger logger;
        private ServerLoaderComponents loaderComponents;
        private WorldReadyEvent worldReadyEvent;
        private Config config;

        public ServerWorldLoader(
            WorldService worldService,
            Logging.Logger logger,
            Config config,
            ServerPrefabProvider prefabProvider,
            WorldReadyEvent worldReadyEvent
        )
        {
            this.worldService = worldService;
            this.logger = logger;
            this.config = config;
            this.worldReadyEvent = worldReadyEvent;

            loaderComponents =
                prefabProvider.ServerLoaderComponents.GetComponent<ServerLoaderComponents>();
            if (loaderComponents == null)
                throw new System.InvalidOperationException(
                    "ServerLoaderComponents not found in prefab provider"
                );
        }

        public async void Start()
        {
            try
            {
                (string worldId, string accessToken) = GetWorldArgs();
                ValidateArgs(worldId);
                ValidateArgs(accessToken);
                await Load(worldId, accessToken);

                // TODO: make all connection related code wait for this event before allowing
                // players to connect or interact with the world in any way
                worldReadyEvent.Raise();
            }
            catch (System.Exception ex)
            {
                logger.Log(
                    $"World could not be loaded: {ex.Message}\n{ex.StackTrace}",
                    Logging.LogType.Error
                );
            }
        }

        // --- Private methods --- //

        private async UniTask Load(string worldId, string accessToken)
        {
            WorldData worldData =
                await LoadWorldData(worldId, accessToken)
                ?? throw new System.InvalidOperationException("Failed to load world data");
            IReadOnlyList<ILoader> loaders =
                loaderComponents.GetLoaders()
                ?? throw new System.InvalidOperationException(
                    "No loaders found in ServerLoaderComponents"
                );

            for (int i = 0; i < loaders.Count; i++)
            {
                ILoader loader = loaders[i];
                logger.Log($"Loading {loader.GetType().Name} | {i + 1} / {loaders.Count}");
                await loader.Load(worldData);
            }
            logger.Log("World loading complete!");
        }

        private async UniTask<WorldData> LoadWorldData(string worldId, string accessToken)
        {
            (WorldData data, string errorMessage, long responseCode) =
                await worldService.GetWorldData(worldId, accessToken);
            if (data == null || !string.IsNullOrEmpty(errorMessage))
                throw new System.Exception(
                    $"Failed to load world data: {errorMessage} (Response code: {responseCode})"
                );
            return data;
        }

        private (string, string) GetWorldArgs()
        {
            if (config.IsDebugWorld)
                return (config.WorldID, config.AccessToken);
            string worldId = ParamsSerializer.GetArgs("worldId", null);
            string accessToken = ParamsSerializer.GetArgs("accessToken", null);
            return (worldId, accessToken);
        }

        private void ValidateArgs(string args)
        {
            if (args == null || string.IsNullOrEmpty(args))
            {
                throw new System.ArgumentException("Invalid command line arguments");
            }
        }
    }
}
