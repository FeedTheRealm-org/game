using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using Worlds;

public class ClientWorldLoader : ILoader
{
    private string worldId;

    private WorldHandler worldHandler;
    private WorldService worldService;

    private string accessToken;

    private Logging.Logger logger;

    public async UniTask<WorldData> Load()
    {
        // worldId = worldHandler.selectedWorldID;
        // accessToken = session.APIToken;

        logger.Log($"[CLIENT] Client Loading World ID: {worldId}");

        WorldData worldData = await LoadWorldData(worldId, accessToken);
        if (worldData == null)
        {
            logger.Log(
                $"Failed to load world data for world ID: {worldId}. Aborting client loading.",
                Logging.LogType.Error
            );
            return null;
        }

        // foreach (GameObject loaderObject in clientLoaders)
        // {
        //     await loader.Load();
        // }

        return worldData;
    }

    private async UniTask<WorldData> LoadWorldData(string worldId, string accessToken)
    {
        (WorldData data, string errorMessage, long responseCode) = await worldService.GetWorldData(
            worldId,
            accessToken
        );
        if (data == null || !string.IsNullOrEmpty(errorMessage))
        {
            logger.Log(
                $"Failed to load world '{worldId}': {errorMessage}, Code: {responseCode}",
                Logging.LogType.Error
            );
            return null;
        }
        return data;
    }
}
