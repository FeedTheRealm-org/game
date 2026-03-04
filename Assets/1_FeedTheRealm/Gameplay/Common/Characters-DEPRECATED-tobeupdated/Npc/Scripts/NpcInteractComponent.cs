using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Quests;
using UnityEngine;

public class NpcInteractComponent : MonoBehaviour, IInteractable
{
    [Header("Dialog settings")]
    [SerializeField]
    private DialogManagerComponent dialogManager;

    [Header("Quest settings")]
    [SerializeField]
    private QuestDecisionEvent questDecisionEvent;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private IInteractor _currentInteractor;

    private void OnEnable()
    {
        questDecisionEvent.OnRaised += OnQuestDecision;
    }

    private void OnDisable()
    {
        questDecisionEvent.OnRaised -= OnQuestDecision;
    }

    public string Interact(IInteractor interactor)
    {
        _currentInteractor = interactor;
        dialogManager.Next();
        logger.Log("NPC interacted with by " + interactor.GameObject.name, this);
        if (!dialogManager.IsQuestOffer())
            interactor.FinishInteracting();
        return dialogManager.NpcId;
    }

    public bool CanInteract(IInteractor interactor)
    {
        return true;
    }

    private void OnQuestDecision(QuestDecisionData _)
    {
        _currentInteractor?.FinishInteracting();
    }
}
