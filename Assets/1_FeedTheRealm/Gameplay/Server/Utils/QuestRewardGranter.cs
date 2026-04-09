using Enums;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Server.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Utils
{
    /// <summary>
    /// Handles reward granting logic for completed quests. Stateless helper used by QuestSystem.
    /// </summary>
    public class QuestRewardGranter
    {
        private readonly uint netId;
        private readonly Logging.Logger logger;
        private readonly QuestRewardGoldEvent questRewardGoldEvent;
        private readonly QuestRewardItemEvent questRewardItemEvent;

        public QuestRewardGranter(
            uint netId,
            Logging.Logger logger,
            QuestRewardGoldEvent questRewardGoldEvent,
            QuestRewardItemEvent questRewardItemEvent
        )
        {
            this.netId = netId;
            this.logger = logger;
            this.questRewardGoldEvent = questRewardGoldEvent;
            this.questRewardItemEvent = questRewardItemEvent;
        }

        public void Grant(QuestData quest)
        {
            if (quest.rewards == null || quest.rewards.Count == 0)
                return;

            foreach (var reward in quest.rewards)
            {
                switch (reward.rewardType)
                {
                    case QuestRewardType.Gold:
                        GrantGold(reward.goldAmount);
                        break;

                    case QuestRewardType.Item:
                        GrantItem(reward.itemId);
                        break;

                    case QuestRewardType.LootTable:
                        if (!string.IsNullOrEmpty(reward.lootTableId))
                            GrantLootTable(reward.lootTableId);
                        break;
                }
            }
        }

        private void GrantGold(int amount)
        {
            if (amount <= 0)
                return;

            logger?.Log($"[QuestRewardGranter] Granting {amount} gold to Player:{netId}.");
            questRewardGoldEvent?.Raise((netId, amount));
        }

        private void GrantItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            logger?.Log($"[QuestRewardGranter] Granting item '{itemId}' to Player:{netId}.");
            questRewardItemEvent?.Raise((netId, itemId));
        }

        private void GrantLootTable(string lootTableId)
        {
            var lootTable = ServerItemsRegistry.GetLootTableById(lootTableId);
            if (lootTable?.lootItems == null)
            {
                logger?.Log(
                    $"[QuestRewardGranter] LootTable '{lootTableId}' not found or empty.",
                    Logging.LogType.Warning
                );
                return;
            }

            foreach (var entry in lootTable.lootItems)
            {
                if (!string.IsNullOrEmpty(entry.id) && Random.Range(0, 100) < entry.dropProbability)
                {
                    logger?.Log(
                        $"[QuestRewardGranter] LootTable '{lootTableId}' → item '{entry.id}' to Player:{netId}."
                    );
                    questRewardItemEvent?.Raise((netId, entry.id));
                }
            }
        }
    }
}
