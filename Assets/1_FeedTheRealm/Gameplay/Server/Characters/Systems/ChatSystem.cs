using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
using FTR.Core.Server.Events;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class ChatSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        private uint netId;
        private WorldMonitor worldMonitor;

        public void Initialize(uint netId, WorldMonitor worldMonitor)
        {
            this.netId = netId;
            this.worldMonitor = worldMonitor;
        }

        /// <summary>
        /// Called by the command pipeline via ICommandable.OnSendMessage.
        /// </summary>
        public void OnSendMessage(IEventCollectable eventCollector, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                logger?.Log(
                    $"[ChatSystem] Player:{netId} sent an empty message — ignored.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            logger?.Log($"[ChatSystem] Player:{netId} says: \"{message}\"", this);

            worldMonitor.Events.Enqueue(
                new ChatMessageBroadcastEvent(
                    netId,
                    new ChatMessageBroadcastEventContent { SenderId = netId, Message = message }
                )
            );
        }

        public void GameTick(float dt) { }
    }
}
