using System.Collections.Generic;
using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Core.Interfaces;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.UI;
using UnityEngine;
using VContainer;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public class WorldSetupService
    {
        private List<ISetup> setupServices;

        public WorldSetupService(
            ClientPrefabProvider clientPrefabProvider,
            OnWorldLeaveEvent onExitEvent,
            IObjectResolver resolver
        )
        {
            WorldUISetupService worldUISetupService = new(
                clientPrefabProvider,
                onExitEvent,
                resolver
            );

            setupServices = new List<ISetup> { worldUISetupService };

            foreach (var service in setupServices)
            {
                resolver.Inject(service);
            }
        }

        public void ExecuteSetup()
        {
            Debug.Log("Executing World Setup...");
            foreach (var setup in setupServices)
            {
                setup.Setup();
            }
        }
    }
}
