using Game.Core.Events;
using Game.Core.Interactions;
using Game.Core.Quests;
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

    public void Interact(IInteractor interactor)
    {
        dialogManager.Next();
        logger.Log("NPC interacted with by " + interactor.GameObject.name, this);
        _currentInteractor = interactor;
        if (!dialogManager.IsQuestOffer())
            interactor.FinishInteracting();
    }

    public bool CanInteract(IInteractor interactor)
    {
        return true;
    }

    private void OnQuestDecision(QuestDecision _)
    {
        _currentInteractor.FinishInteracting();
    }
}
