using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using API;
using Game.Core.Exceptions;
using Game.Core.Utils;
using Models;
using UnityEngine;
using Worlds;

namespace Core.Systems.Worlds.Loader
{
    public class WorldLoaderController : MonoBehaviour
    {
        [SerializeField]
        private WorldHandler worldHandler;

        [SerializeField]
        private Logging.Logger logger;

        [Header("Loaders")]
        [SerializeField]
        private List<GameObject> serverLoaders;

        [SerializeField]
        private List<GameObject> clientLoaders;

        [Header("Debug Settings")]
        [Description(
            "Here you can set the world ID and access token for debugging purposes. Also add the player GameObject to be spawned in the world."
        )]
        [SerializeField]
        private string worldId;

        [SerializeField]
        private string accessToken;

        [SerializeField]
        private WorldService worldService;

        public Task LoadServer()
        {
            worldId = GetParams.GetArgs("worldId", worldId);
            accessToken = GetParams.GetArgs("accessToken", accessToken); // TODO: Secure this token properly
            logger.Log(
                $"[SERVER] Server Loading World ID: {worldId} | Access Token: {accessToken}",
                this
            );
            return LoadWorldServer(worldId, accessToken);
        }

        public Task LoadClient()
        {
            worldId = worldHandler.selectedWorldID;
            accessToken = GetParams.GetArgs("accessToken", accessToken); // TODO: Consider using the clients session token instead
            logger.Log(
                $"[CLIENT] Client Loading World ID: {worldId} | Access Token: {accessToken}",
                this
            );
            return LoadWorldClient(worldId, accessToken);
        }

        // ----- Private Methods ----- //

        /// <summary>
        ///  Loads the world data on the server side.
        /// </summary>
        private async Task<WorldData> LoadWorldServer(string worldId, string accessToken)
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
                await loader.LoadServer(worldData, accessToken);
            }
            return worldData;
        }

        /// <summary>
        ///  Loads the world data on the client side.
        /// </summary>
        private async Task LoadWorldClient(string worldId, string accessToken)
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
                await loader.LoadClient(worldData, accessToken);
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
