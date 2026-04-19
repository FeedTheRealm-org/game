using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
using FTR.Gameplay.Bot.Linkers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Bot.EntryPoints.Scopes
{
    public class BotInitiator : LifetimeScope
    {
        [SerializeField]
        private SceneReference mainScene;

        [SerializeField]
        private Config config;

        [SerializeField]
        private BotConfig botConfig;

        [SerializeField]
        private Logging.Logger logger;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config.RuntimeRole != RuntimeRole.Bot)
                throw new System.InvalidOperationException(
                    "BotInitiator should only be used in Bot runtime role."
                );

            ValidateSerializeFields();

            builder.RegisterInstance(mainScene);
            builder.RegisterInstance(config);
            builder.RegisterInstance(botConfig);
            builder.RegisterInstance(logger);

            builder.RegisterEntryPoint<BotEntryPoint>();
        }

        private void ValidateSerializeFields()
        {
            if (mainScene == null)
                throw new System.NullReferenceException(
                    "MainScene is not assigned in BotWorldInitiator."
                );

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
