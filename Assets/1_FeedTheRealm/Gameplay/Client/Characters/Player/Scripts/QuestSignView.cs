using FTR.Core.Client;
using FTR.Core.Common.EventChannels;
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

    [Inject]
    private ClientPrefabProvider prefabProvider;

    [Inject]
    private NpcDialogMessageEvent npcDialogMessageEvent;

    [Inject]
    private NpcDialogClosedEvent npcDialogClosedEvent;

    private GameObject signInstance;
    private GameObject haloInstance;
    private NpcDialogRegistry dialogRegistry;
    private string npcId;

    public void Initialize(string npcId, NpcDialogRegistry dialogRegistry, Transform characterBody)
    {
        this.npcId = npcId;
        this.dialogRegistry = dialogRegistry;

        bool hasAvailableQuest = CheckHasAvailableQuest(dialogRegistry);

        prefabProvider.QuestSignPrefab.SetActive(false);
        prefabProvider.QuestHaloPrefab.SetActive(false);

        this.signInstance = Instantiate(prefabProvider.QuestSignPrefab, characterBody);
        signInstance.transform.localPosition = new Vector3(0, 1.4f, 0);
        signInstance.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        signInstance.SetActive(false);

        this.haloInstance = Instantiate(prefabProvider.QuestHaloPrefab, characterBody);
        haloInstance.transform.localPosition = new Vector3(0, 0, 0);
        haloInstance.transform.localScale = new Vector3(4, 4, 4);
        haloInstance.transform.rotation = Quaternion.Euler(-93, 0, 0);
        haloInstance.SetActive(false);

        signInstance.SetActive(hasAvailableQuest);
        haloInstance.SetActive(hasAvailableQuest);

        questDecisionEvent.OnRaised += HandleQuestDecision;
        questCompletedEvent.OnRaised += HandleQuestCompleted;
        npcDialogMessageEvent.OnRaised += HandleNpcDialogMessage;
        npcDialogClosedEvent.OnRaised += HandleNpcDialogClosed;
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

    private void HandleNpcDialogMessage((string npcId, MessageData message) data)
    {
        if (data.npcId != npcId)
            return;

        SetSignVisible(false);
    }

    private void HandleNpcDialogClosed()
    {
        bool hasAvailableQuest = CheckHasAvailableQuest(dialogRegistry);
        SetSignVisible(hasAvailableQuest);
        if (!hasAvailableQuest)
            SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (signInstance != null)
            signInstance.SetActive(visible);
        if (haloInstance != null)
            haloInstance.SetActive(visible);
    }

    private void SetSignVisible(bool visible)
    {
        if (signInstance != null)
            signInstance.SetActive(visible && haloInstance.activeSelf);
    }
}
