using System;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class GoldSystem : MonoBehaviour
    {
        public event Action<int> OnSaveGold;

        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private ServerConfig config;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            if (resolver.TryResolve<QuestRewardGoldEvent>(out var ev) && ev != null)
                questRewardGoldEvent = ev;
        }

        private QuestRewardGoldEvent questRewardGoldEvent;
        private bool subscribedToQuestReward = false;

        private uint netId;
        private GoldStateStorage goldState;
        private WorldMonitor worldMonitor;

        public int GetCurrentGold() => goldState.Gold;

        public void Initialize(uint netId, GoldStateStorage goldState, WorldMonitor worldMonitor)
        {
            this.netId = netId;
            this.goldState = goldState;
            this.worldMonitor = worldMonitor;

            SubscribeToQuestReward();
        }

        private void OnDestroy()
        {
            UnsubscribeFromQuestReward();
        }

        private void OnQuestRewardGold((uint playerNetId, int goldAmount) data)
        {
            if (data.playerNetId != netId)
                return;

            logger?.Log(
                $"[GoldSystem] Quest reward: adding {data.goldAmount} gold to Player:{netId}.",
                this
            );
            goldState.AddGold(data.goldAmount);
        }

        public void AddGold(IEventCollectable ec, int amount)
        {
            if (amount <= 0)
            {
                logger.Log($"[GoldSystem] AddGold: invalid amount {amount}", this);
                return;
            }

            logger.Log($"[GoldSystem] Player {netId} gained {amount} gold", this);
            goldState.AddGold(amount);
        }

        public bool ReduceGold(IEventCollectable ec, int amount)
        {
            if (amount <= 0)
            {
                logger.Log($"[GoldSystem] ReduceGold: invalid amount {amount}", this);
                return false;
            }

            if (goldState.Gold < amount)
            {
                logger.Log(
                    $"[GoldSystem] ReduceGold: insufficient gold for Player:{netId}. "
                        + $"Requested {amount}, current {goldState.Gold}",
                    this
                );
                return false;
            }

            logger.Log($"[GoldSystem] Player {netId} reduced {amount} gold", this);
            goldState.ReduceGold(amount);
            return true;
        }

        public bool HasEnoughGold(uint netId, string productId, int price, int amount)
        {
            if (goldState.Gold >= price * amount)
                return true;

            var connId = GetPlayerConnectionId(netId);
            if (!connId.HasValue)
            {
                logger?.Log($"[GoldSystem] conn not found, Player:{netId}.", this);
                return false;
            }

            worldMonitor.Events.Enqueue(
                new NotEnoughGoldEvent(
                    netId,
                    new NotEnoughGoldEventContent { ProductId = productId, Amount = amount },
                    connId.Value
                )
            );

            worldMonitor.Events.Enqueue(new InteractCompletedEvent(netId, connId.Value));

            logger?.Log(
                $"[GoldSystem] Player {netId} has insufficient gold for '{productId}'.",
                this
            );
            return false;
        }

        public void OnPickUp(IEventCollectable ec, int goldAmount, System.Action<bool> onComplete)
        {
            if (goldAmount <= 0)
            {
                logger.Log(
                    $"[GoldSystem] OnPickUp: invalid goldAmount {goldAmount} for player {netId}",
                    this
                );
                onComplete?.Invoke(false);
                return;
            }

            logger.Log($"[GoldSystem] Player {netId} picked up {goldAmount} gold", this);
            AddGold(ec, goldAmount);
            onComplete?.Invoke(true);
        }

        public void LoadGold(int savedGold)
        {
            goldState.SetGold(savedGold);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (
                worldMonitor.Entities.TryGet(playerNetId, out var entity)
                && entity.ConnectionId.HasValue
            )
                return entity.ConnectionId.Value;

            return null;
        }

        private void SubscribeToQuestReward()
        {
            if (subscribedToQuestReward || questRewardGoldEvent == null)
                return;
            questRewardGoldEvent.OnRaised += OnQuestRewardGold;
            subscribedToQuestReward = true;
        }

        private void UnsubscribeFromQuestReward()
        {
            if (!subscribedToQuestReward || questRewardGoldEvent == null)
                return;
            questRewardGoldEvent.OnRaised -= OnQuestRewardGold;
            subscribedToQuestReward = false;
        }
    }
}
