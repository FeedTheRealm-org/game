using FTR.Core.Client.EventChannels.Quest;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Environment.Quest;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

public class QuestSignView : MonoBehaviour
{
    [Inject]
    private QuestDecisionEvent questDecisionEvent;

    [Inject]
    private QuestCompletedEvent questCompletedEvent;

    private GameObject signInstance;
    private GameObject haloInstance;
    private NpcDialogRegistry dialogRegistry;
    private string npcId;

    public void Initialize(
        string npcId,
        NpcDialogRegistry dialogRegistry,
        GameObject signInstance,
        GameObject haloInstance
    )
    {
        this.npcId = npcId;
        this.signInstance = signInstance;
        this.haloInstance = haloInstance;
        this.dialogRegistry = dialogRegistry;

        bool hasAvailableQuest = CheckHasAvailableQuest(dialogRegistry);
        signInstance.SetActive(hasAvailableQuest);
        haloInstance.SetActive(hasAvailableQuest);

        questDecisionEvent.OnRaised += HandleQuestDecision;
        questCompletedEvent.OnRaised += HandleQuestCompleted;
    }

    private void OnDestroy()
    {
        if (questDecisionEvent != null)
            questDecisionEvent.OnRaised -= HandleQuestDecision;
        if (questCompletedEvent != null)
            questCompletedEvent.OnRaised -= HandleQuestCompleted;
    }

    private bool CheckHasAvailableQuest(NpcDialogRegistry dialogRegistry)
    {
        int progressionCount = dialogRegistry.GetProgressionCount(npcId);

        for (int i = 0; i < progressionCount; i++)
        {
            string questId = dialogRegistry.GetQuestIdForSlot(npcId, i);
            if (!string.IsNullOrEmpty(questId))
                return true;
        }

        return false;
    }

    private void HandleQuestDecision(QuestDecisionData decision)
    {
        if (decision.NpcId != npcId)
            return;
        if (decision.IsAccepted)
            SetVisible(false);
    }

    private void HandleQuestCompleted((QuestData quest, string effectiveQuestId) data)
    {
        if (!data.effectiveQuestId.EndsWith("_" + npcId))
            return;

        int progressionCount = dialogRegistry.GetProgressionCount(npcId);
        for (int i = 0; i < progressionCount; i++)
        {
            string questId = dialogRegistry.GetQuestIdForSlot(npcId, i);
            if (questId == data.quest.id && dialogRegistry.IsRepeatableAt(npcId, i))
            {
                SetVisible(true);
                return;
            }
        }
    }

    private void SetVisible(bool visible)
    {
        if (signInstance != null)
            signInstance.SetActive(visible);
        if (haloInstance != null)
            haloInstance.SetActive(visible);
    }
}
