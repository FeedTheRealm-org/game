using System;
using System.Collections;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Interactions;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Interactions;
using UnityEngine;

public class PlayerInteractSystem : MonoBehaviour, IServerInteractor
{
    [Header("Interaction Settings")]
    [SerializeField]
    private float interactionRadius = 2.0f;

    [SerializeField]
    private LayerMask interactableLayerMask;

    [Header("Logging Settings")]
    [SerializeField]
    private Logging.Logger logger;

    public GameObject GameObject => this.gameObject;
    public Transform Transform => this.transform;

    public uint NetId { get; private set; }
    public IEventCollectable CurrentEventCollector { get; private set; }
    public IInteractable CurrentInteractable { get; private set; }

    public void Initialize(uint netId)
    {
        this.NetId = netId;
    }

    /// <summary>
    /// Attempts to interact with the closest interactable object within range.
    /// Returns true if an interaction was initiated, false otherwise.
    /// </summary>
    public void OnInteract(IEventCollectable ec)
    {
        if (logger != null)
            logger.Log("Player interaction triggered.", this);
        this.CurrentEventCollector = ec;

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            interactableLayerMask
        );
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            IInteractable interactable = hitCollider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(this))
            {
                float distance = Vector3.Distance(
                    transform.position,
                    hitCollider.transform.position
                );
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable == null)
        {
            FinishInteracting();
            this.CurrentEventCollector = null;
            return;
        }

        if (CurrentInteractable == closestInteractable)
        {
            if (logger != null)
                logger.Log("Continuing interaction with: " + closestInteractable, this);
            CurrentInteractable.ContinueInteraction(this);
            this.CurrentEventCollector = null;
            return;
        }

        if (CurrentInteractable != null && CurrentInteractable != closestInteractable)
        {
            CurrentInteractable.StopInteraction(this);
        }

        CurrentInteractable = closestInteractable;
        if (logger != null)
            logger.Log("Interacting with: " + closestInteractable, this);
        CurrentInteractable.Interact(this);

        this.CurrentEventCollector = null;
    }

    public void OnDialogNext(IEventCollectable ec)
    {
        if (CurrentInteractable != null)
        {
            this.CurrentEventCollector = ec;
            CurrentInteractable.ContinueInteraction(this);
            this.CurrentEventCollector = null;
        }
        else
        {
            if (logger != null)
                logger.Log(
                    "[PlayerInteractSystem] OnDialogNext received but no active interaction.",
                    this
                );
        }
    }

    public void FinishInteracting()
    {
        if (logger != null)
            logger.Log("Finished interacting.", this);
        if (CurrentInteractable != null)
        {
            CurrentInteractable.StopInteraction(this);
            CurrentInteractable = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
