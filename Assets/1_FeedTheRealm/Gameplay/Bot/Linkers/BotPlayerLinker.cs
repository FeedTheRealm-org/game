using FTR.Core.Bot.Config;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Bot.Characters.Player;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;

namespace FTR.Gameplay.Bot.Linkers
{
    public class BotPlayerLinker : PlayerLinker
    {
        private readonly BotRuntimeConfig runtimeConfig;
        private readonly Logging.Logger logger;

        public BotPlayerLinker(BotRuntimeConfig runtimeConfig, Logging.Logger logger)
        {
            this.runtimeConfig = runtimeConfig;
            this.logger = logger;
        }

        public override void Link(GameObject gameObject)
        {
            var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
            if (networkAdapter == null)
            {
                Debug.LogWarning(
                    "[BotPlayerLinker] Missing NetworkAdapter on player object.",
                    gameObject
                );
                return;
            }

            if (!networkAdapter.IsLocalPlayer)
                return;

            gameObject.name = $"BotPlayer-{networkAdapter.netId}";

            if (!string.IsNullOrWhiteSpace(runtimeConfig.JoinToken))
            {
                networkAdapter.DispatchTransaction(
                    new TransactionCommandDTO
                    {
                        Type = TransactionType.SetUserId,
                        NetId = networkAdapter.netId,
                        Id = runtimeConfig.JoinToken,
                        content = null,
                    }
                );
            }
            else
            {
                logger.Log(
                    "[BotPlayerLinker] --join-token was not provided. Character identity resolution on server will be skipped."
                );
            }

            var botController = gameObject.GetComponent<BotPlayerController>();
            if (botController == null)
                botController = gameObject.AddComponent<BotPlayerController>();

            botController.Initialize(networkAdapter, runtimeConfig);

            logger.Log(
                $"[BotPlayerLinker] Local bot initialized: botId={runtimeConfig.BotId}, worldId={runtimeConfig.WorldId}, zoneId={runtimeConfig.ZoneId}, netId={networkAdapter.netId}"
            );
        }
    }
}
