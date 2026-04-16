using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Quests;
using FTR.Gameplay.Client.Environment.Quest;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

/// <summary>
/// Client-side bridge between server quest events and local UI event channels.
/// </summary>
public class QuestProgressView : MonoBehaviour
{
    private QuestProgressEvent questProgressEvent;
    private QuestCompletedEvent questCompletedEvent;
    private ClientQuestRegistry clientQuestRegistry;

    private NetworkEventRouter eventRouter;

    [Inject]
    public void Construct(
        QuestProgressEvent questProgressEvent,
        QuestCompletedEvent questCompletedEvent,
        ClientQuestRegistry clientQuestRegistry
    )
    {
        this.questProgressEvent = questProgressEvent;
        this.questCompletedEvent = questCompletedEvent;
        this.clientQuestRegistry = clientQuestRegistry;
    }

    public void Initialize(NetworkEventRouter eventRouter)
    {
        this.eventRouter = eventRouter;
        eventRouter.OnQuestProgressEvent += HandleQuestProgress;
        eventRouter.OnQuestCompletedEvent += HandleQuestCompleted;
    }

    private void OnDestroy()
    {
        if (eventRouter == null)
            return;
        eventRouter.OnQuestProgressEvent -= HandleQuestProgress;
        eventRouter.OnQuestCompletedEvent -= HandleQuestCompleted;
    }

    private void HandleQuestProgress(
        FTR.Core.Common.Protocol.RpcMessages.QuestProgressEventContent content
    )
    {
        if (!clientQuestRegistry.TryGetQuest(content.QuestId, out var questData))
        {
            Debug.LogWarning(
                $"[QuestProgressView] Quest '{content.QuestId}' not found in ClientQuestRegistry."
            );
            return;
        }

        questProgressEvent.Raise(
            new QuestProgressData
            {
                Id = content.EffectiveQuestId,
                Quest = questData,
                CurrentProgressAmount = content.Current,
                TargetProgressAmount = content.Target,
            }
        );
    }

    private void HandleQuestCompleted(
        FTR.Core.Common.Protocol.RpcMessages.QuestCompletedEventContent content
    )
    {
        if (!clientQuestRegistry.TryGetQuest(content.QuestId, out var questData))
        {
            Debug.LogWarning(
                $"[QuestProgressView] Quest '{content.QuestId}' not found for completion event."
            );
            return;
        }

        questCompletedEvent.Raise((questData, content.EffectiveQuestId));
    }
}
