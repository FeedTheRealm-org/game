using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
using FTR.Gameplay.Bot.Linkers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Bot.EntryPoints.Scopes
{
    public class BotWorldInitiator : LifetimeScope
    {
        [SerializeField]
        private Config config;

        [SerializeField]
        private Logging.Logger logger;

        public RuntimeRole CurrentRuntimeRole =>
            config != null ? config.RuntimeRole : RuntimeRole.Client;

        protected override void Configure(IContainerBuilder builder)
        {
            if (CurrentRuntimeRole != RuntimeRole.Bot)
                return;

            ValidateSerializeFields();

            builder.RegisterInstance(config);
            builder.RegisterInstance(logger);

            builder.Register<BotRuntimeConfig>(Lifetime.Singleton);
            builder.Register<BotPlayerLinker>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BotWorldEntryPoint>();
        }

        private void ValidateSerializeFields()
        {
            if (config == null)
                throw new System.NullReferenceException(
                    "Config is not assigned in BotWorldInitiator."
                );

            if (logger == null)
                throw new System.NullReferenceException(
                    "Logger is not assigned in BotWorldInitiator."
                );
        }
    }
}
