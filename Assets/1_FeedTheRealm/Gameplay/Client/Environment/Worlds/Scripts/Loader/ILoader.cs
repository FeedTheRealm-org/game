using System.Threading.Tasks;
using Models;

namespace Gameplay.Client.Environment.Worlds.Loader
{
    public interface IServerLoader
    {
        Task LoadServer(WorldData worldData, string accessToken);
    }

    public interface IClientLoader
    {
        Task LoadClient(WorldData worldData, string accessToken);
    }
}
