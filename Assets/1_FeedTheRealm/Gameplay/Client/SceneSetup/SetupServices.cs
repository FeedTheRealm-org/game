using FeedTheRealm.Core.Interfaces;
using UnityEngine;
using VContainer;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public class SetupServices
    {
        public void RegisterAll(IContainerBuilder builder)
        {
            var setupServices = new[] { typeof(WorldUISetupService) };

            foreach (var serviceType in setupServices)
            {
                builder.Register(serviceType, Lifetime.Scoped).As(serviceType).As(typeof(ISetup));
            }

            builder.Register<ConfirmPopupHandle>(Lifetime.Singleton);

            builder.Register<WorldSetupService>(Lifetime.Scoped);
        }
    }
}
