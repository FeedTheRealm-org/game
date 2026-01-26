using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Game.Core.Exceptions;
using Models;
using UnityEngine;

namespace Core.Systems.Worlds.Loader
{
    public class WorldLoaderController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private WorldService worldService;

        [Header("Loaders")]
        [SerializeField]
        private List<GameObject> serverLoaders;

        [SerializeField]
        private List<GameObject> clientLoaders;

        /// <summary>
        ///  Loads the world data on the server side.
        /// </summary>
        public async Task<WorldData> LoadWorldServer(string worldId, string accessToken)
        {
            logger.Log("Loading World (Server)", this);
            WorldData worldData = await LoadWorldData(worldId, accessToken);

            foreach (GameObject loaderObject in serverLoaders)
            {
                IServerLoader loader =
                    loaderObject.GetComponent<IServerLoader>()
                    ?? throw new MissingControllerException(
                        loaderObject.name,
                        nameof(IServerLoader)
                    );

                logger.Log($"[Server] Starting loader: {loader.GetType().Name}", this);
                await loader.LoadServer(worldData, accessToken);
                logger.Log($"[Server] Finished loader: {loader.GetType().Name}", this);
            }
            return worldData;
        }

        /// <summary>
        ///  Loads the world data on the client side.
        /// </summary>
        public async Task LoadWorldClient(string worldId, string accessToken)
        {
            logger.Log("Loading World (Client)", this);
            WorldData worldData = await LoadWorldData(worldId, accessToken);
            foreach (GameObject loaderObject in clientLoaders)
            {
                var loader =
                    loaderObject.GetComponent<IClientLoader>()
                    ?? throw new MissingControllerException(
                        loaderObject.name,
                        nameof(IClientLoader)
                    );
                logger.Log($"[Client] Starting loader: {loader.GetType().Name}", this);
                await loader.LoadClient(worldData, accessToken);
                logger.Log($"[Client] Finished loader: {loader.GetType().Name}", this);
            }
        }

        /// <summary>
        ///  Loads the world data from the API.
        /// </summary>
        private async Task<WorldData> LoadWorldData(string worldId, string accessToken)
        {
            (WorldData data, string errorMessage, long responseCode) =
                await worldService.GetWorldData(worldId, accessToken);
            if (data == null || !string.IsNullOrEmpty(errorMessage))
            {
                logger.Log(
                    $"Failed to load world '{worldId}': {errorMessage}, Code: {responseCode}",
                    this,
                    Logging.LogType.Error
                );
                return null;
            }
            return data;
        }
    }
}
