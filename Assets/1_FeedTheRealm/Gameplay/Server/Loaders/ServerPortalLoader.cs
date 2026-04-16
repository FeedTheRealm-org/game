using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Environment.Portal;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Loaders
{
    /// <summary>
    /// Loader that populates PortalRegistry with portal data.
    /// </summary>
    public class ServerPortalLoader : ILoader
    {
        private PortalRegistry portalRegistry;
        private GameObject portalPrefab;
        private IObjectResolver resolver;

        public ServerPortalLoader(
            PortalRegistry portalRegistry,
            ServerPrefabProvider prefabProvider,
            IObjectResolver resolver
        )
        {
            this.portalRegistry = portalRegistry;
            portalPrefab = prefabProvider.PortalPrefab;
            this.resolver = resolver;
        }

        public async UniTask Load(string world_id, ZoneData zoneData, CreatablesData creatablesData)
        {
            if (creatablesData?.portals == null)
            {
                Debug.LogWarning("[PortalLoader] CreatablesData has no portals.");
                return;
            }

            portalRegistry.Populate(creatablesData.portals, zoneData.portalPlacements);
            Debug.Log(
                $"[ServerPortalLoader] Registry populated with these ids: {string.Join(", ", portalRegistry.GetAllPortalIds())}"
            );

            foreach (var portal in zoneData.portalPlacements)
            {
                GameObject instance = resolver.Instantiate(portalPrefab);
                instance.name = $"Portal_{portal.id}";
                instance.transform.position = portal.position;
                instance.transform.localScale = new Vector3(
                    portal.radius,
                    instance.transform.localScale.y,
                    portal.radius
                );
                var portalStorage = instance.GetComponent<PortalStateStorage>();
                portalStorage.portalId = portal.id;

                instance.GetComponent<GameObjectLinker>().Initialize();
            }

            await UniTask.CompletedTask;
        }
    }
}
