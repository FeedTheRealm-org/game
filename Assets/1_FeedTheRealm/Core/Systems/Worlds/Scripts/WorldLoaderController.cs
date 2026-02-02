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
        [Header("Dependencies")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private WorldHandler worldHandler;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private Session.Session session;

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

        /// <summary>
        ///  Loads the world on the server side using command line arguments.
        /// </summary>
        public Task LoadServer()
        {
#if !UNITY_EDITOR
            worldId = GetParams.GetArgs("worldId");
            accessToken = GetParams.GetArgs("accessToken");
#endif
            logger.Log(
                $"[SERVER] Server Loading World ID: {worldId} | Access Token: {accessToken}",
                this
            );

            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(accessToken))
            {
                logger.Log(
                    "World ID or Access Token is not set. Cannot load world.",
                    this,
                    Logging.LogType.Error
                );
                return Task.CompletedTask;
            }

            return LoadWorldServer(worldId, accessToken);
        }

        /// <summary>
        ///  Loads the world on the client side using the selected world id from WorldHandler
        ///  and the clients current session token.
        /// </summary>
        public Task LoadClient()
        {
#if !UNITY_EDITOR
            worldId = worldHandler.selectedWorldID;
            accessToken = session.APIToken ?? accessToken;
#endif
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
            WorldData worldData = await LoadWorldData(worldId, accessToken);
            LoaderStartupMessage(worldData);
            foreach (GameObject loaderObject in serverLoaders)
            {
                logger.Log($"--------------------------", this);
                IServerLoader loader =
                    loaderObject.GetComponent<IServerLoader>()
                    ?? throw new MissingControllerException(
                        loaderObject.name,
                        nameof(IServerLoader)
                    );
                await loader.LoadServer(worldData, accessToken);
            }
            logger.Log($"World ready for players to join!", this);
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

        private void LoaderStartupMessage(WorldData worldData)
        {
            logger.Log(
                "------------------------------------------------------------------------------------------------\n"
                    + "________________________________   ._.    __________________________________   _________________________\n"
                    + "\\_   _____/\\__    ___/\\______   \\  | |   /   _____/\\_   _____/\\______   \\   \\ /   /\\_   _____/\\______   \\\n"
                    + " |    __)    |    |    |       _/  |_|   \\_____  \\  |    __)_  |       _/\\   Y   /  |    __)_  |       _/\n"
                    + " |     \\     |    |    |    |   \\  |-|   /        \\ |        \\ |    |   \\ \\     /   |        \\ |    |   \\\n"
                    + " \\___  /     |____|    |____|_  /  | |  /_______  //_______  / |____|_  /  \\___/   /_______  / |____|_  /\n"
                    + "     \\/                       \\/   |_|          \\/         \\/         \\/                   \\/         \\/ \n"
                    + "------------------------------------------------------------------------------------------------\n"
                    + $"{worldData}",
                this
            );
        }
    }
}
