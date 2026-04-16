using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Environment.Quest;
using FTR.Gameplay.Common.Linkers;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Loaders
{
    /// <summary>
    /// Client-side loader that populates ClientQuestRegistry with world quest data.
    /// </summary>
    public class ClientPortalLoader : ILoader
    {
        private readonly GameObject portalPrefab;
        private readonly IObjectResolver resolver;

        public ClientPortalLoader(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
        {
            portalPrefab = prefabProvider.PortalPrefab;
            this.resolver = resolver;
        }

        public async UniTask Load(string world_id, ZoneData zoneData, CreatablesData creatablesData)
        {
            foreach (var placement in zoneData.portalPlacements)
            {
                GameObject instance = resolver.Instantiate(portalPrefab);
                instance.GetComponent<GameObjectLinker>().Initialize();
                instance.name = $"Portal_{placement.id}";
                instance.transform.position = placement.position;
                instance.transform.localScale = new Vector3(
                    placement.radius,
                    instance.transform.localScale.y,
                    placement.radius
                );
            }
            await UniTask.CompletedTask;
        }
    }
}
