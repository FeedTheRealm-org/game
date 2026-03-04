using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;

public class ServerWorldLoader : ILoader
{
    private string worldId;

    private WorldService worldService;

    private string accessToken;

    private Logging.Logger logger;

    // private ILoader serverStructureLoader = new();

    public async UniTask<WorldData> Load()
    {
        // worldId = ParamsSerializer.GetArgs("worldId");
        // accessToken = ParamsSerializer.GetArgs("accessToken");

        logger.Log($"[SERVER] Server Loading World ID: {worldId}");

        WorldData worldData = await LoadWorldData(worldId, accessToken);
        if (worldData == null)
        {
            logger.Log(
                $"Failed to load world data for world ID: {worldId}. Aborting server loading.",
                Logging.LogType.Error
            );
            return null;
        }
        // foreach (GameObject loaderObject in serverLoaders)
        // {
        //     await loader.LoadServer(worldData, accessToken);
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
