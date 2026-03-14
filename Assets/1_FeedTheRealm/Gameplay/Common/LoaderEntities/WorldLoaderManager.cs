using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.WorldLoader;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Common.LoaderEntities
{
    [RequireComponent(typeof(LoaderProvider))]
    public abstract class WorldLoaderManager : MonoBehaviour
    {
        [Header("General Dependencies")]
        [SerializeField]
        protected Config config;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private Logging.Logger logger;
        private LoaderProvider loaderProvider;
        public abstract string GetWorldId();
        public abstract string GetAccessToken();

        public async void LoadWorld()
        {
            if (!config.DoNotLoadWorld)
                Initialize();
        }

        private async void Initialize()
        {
            loaderProvider = GetComponent<LoaderProvider>();
            try
            {
                (string worldId, string accessToken) = (GetWorldId(), GetAccessToken());
                ValidateArgs(worldId, "worldId");
                ValidateArgs(accessToken, "accessToken");
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

        // --- Private methods --- //

        private async UniTask Load(string worldId, string accessToken)
        {
            WorldData worldData =
                await LoadWorldData(worldId, accessToken)
                ?? throw new System.InvalidOperationException("Failed to load world data");
            IReadOnlyList<ILoader> loaders =
                loaderProvider.GetLoaders()
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

        private void ValidateArgs(string args, string argName)
        {
            if (args == null || string.IsNullOrEmpty(args))
            {
                throw new System.ArgumentException($"Empty or null value for: {argName}");
            }
        }
    }
}
