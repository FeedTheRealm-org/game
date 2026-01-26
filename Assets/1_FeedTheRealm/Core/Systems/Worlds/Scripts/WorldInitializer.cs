using System.ComponentModel;
using System.Threading.Tasks;
using Core.Systems.Worlds.Loader;
using UnityEngine;

namespace Core.Systems.Worlds
{
    /// <summary>
    /// This is the main entry point for initializing and loading a game world.
    /// </summary>
    public class WorldInitializer : MonoBehaviour
    {
        // [Header("UI Elements")]
        // [SerializeField]
        // private UIDocument loadingScreenUI;

        [Header("World Loader Controller")]
        [SerializeField]
        private WorldLoaderController worldLoaderController;

        [Header("Debug Settings")]
        [Description(
            "Here you can set the world ID and access token for debugging purposes. Also add the player GameObject to be spawned in the world."
        )]
        [SerializeField]
        private string worldId;

        [SerializeField]
        private string accessToken;

        public Task LoadServer()
        {
            worldId = Game.Core.Utils.GetParams.GetEnvVars("worldId", worldId);
            accessToken = Game.Core.Utils.GetParams.GetEnvVars("accessToken", accessToken);
            return worldLoaderController.LoadWorldServer(worldId, accessToken);
        }

        public Task LoadClient()
        {
            worldId = Game.Core.Utils.GetParams.GetEnvVars("worldId", worldId);
            accessToken = Game.Core.Utils.GetParams.GetEnvVars("accessToken", accessToken);
            return worldLoaderController.LoadWorldClient(worldId, accessToken);
        }
    }
}
