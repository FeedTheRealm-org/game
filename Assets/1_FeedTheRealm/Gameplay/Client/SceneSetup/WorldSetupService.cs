using System.Collections.Generic;
using FeedTheRealm.Core.Interfaces;
using UnityEngine;
using VContainer;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public class WorldSetupService
    {
        private readonly IEnumerable<ISetup> setupServices;

        public WorldSetupService(IEnumerable<ISetup> setupServices)
        {
            this.setupServices = setupServices;
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
