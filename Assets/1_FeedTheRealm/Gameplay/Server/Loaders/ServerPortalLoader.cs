using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
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
        [Inject]
        private PortalRegistry portalRegistry;

        [Inject]
        private GameObject portalPrefab;

        [Inject]
        private IObjectResolver resolver;

        public ServerPortalLoader(
            PortalRegistry portalRegistry,
            ServerPrefabProvider prefabProvider,
            IObjectResolver resolver
        )
        {
            this.portalRegistry = portalRegistry;
            this.portalPrefab = prefabProvider.PortalComponent;
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
                $"[ServerPortalLoader] Registry populated with {zoneData.portalPlacements.Count} portal(s)."
            );

            foreach (var placement in zoneData.portalPlacements)
            {
                GameObject instance = resolver.Instantiate(portalPrefab);
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
