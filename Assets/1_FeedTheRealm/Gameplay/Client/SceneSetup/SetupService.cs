using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Core.Interfaces;
using Unity.Cinemachine;
using UnityEngine;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public abstract class SetupService : ISetup
    {
        private readonly WorldSetupEvent setupEvent;

        // TODO: insted of raising an event, we should itereate a list of ISetup in the WorldSetupService
        // and call setup on each one. This way we can control the order of the setups and avoid potential issues with event listeners.
        // and also setup the loading screen before starting the setup process, and hide it after all setups are done.
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
