using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Core.Interfaces;
using Unity.Cinemachine;
using UnityEngine;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public abstract class SetupService : ISetup
    {
        private readonly WorldSetupEvent setupEvent;

        public SetupService(WorldSetupEvent setupEvent)
        {
            this.setupEvent = setupEvent;
            setupEvent.OnRaised += Setup;
        }

        public void Dispose()
        {
            setupEvent.OnRaised -= Setup;
        }

        public abstract void Setup();
    }
}
