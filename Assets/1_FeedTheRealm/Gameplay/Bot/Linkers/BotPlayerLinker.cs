using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Bot.Characters.Player;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Bot.Linkers
{
    public class BotPlayerLinker : PlayerLinker
    {
        private readonly Config config;
        private readonly BotConfig botConfig;
        private readonly Session.Session session;
        private readonly Logging.Logger logger;
        private readonly IObjectResolver resolver;

        public BotPlayerLinker(
            Config config,
            BotConfig botConfig,
            Session.Session session,
            Logging.Logger logger,
            IObjectResolver resolver
        )
        {
            this.config = config;
            this.botConfig = botConfig;
            this.session = session;
            this.logger = logger;
            this.resolver = resolver;
        }

        public override void Link(GameObject gameObject)
        {
            var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
            if (networkAdapter == null)
                throw new System.NullReferenceException(
                    $"NetworkAdapter component not found on {gameObject.name}."
                );

            if (!networkAdapter.IsLocalPlayer)
                return;

            gameObject.name = $"BotPlayer_{networkAdapter.netId}";

            networkAdapter.DispatchTransaction(
                new TransactionCommandDTO
                {
                    Type = TransactionType.SetUserId,
                    NetId = networkAdapter.netId,
                    Id = $"{config.BotJoinToken}_{session.UserId}",
                    content = null,
                }
            );

            var botController = gameObject.AddComponent<BotPlayerController>();
            resolver.Inject(botController);
            resolver.Inject(networkAdapter);
            botController.Initialize(networkAdapter);

            logger.Log(
                $"[BotPlayerLinker] Local bot initialized: botId={botConfig.BotId}, worldId={botConfig.WorldId}, zoneId={botConfig.ZoneId}, netId={networkAdapter.netId}"
            );
            var rb = gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
}
