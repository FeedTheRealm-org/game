using System;
using System.Collections;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Interactions;
using UnityEngine;

public class PlayerInteractSystem : MonoBehaviour, IInteractor
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

    // public event Action OnInteractFinished; // TODO: adapt to new way -> this was used to unblock the player movement and actions

    /// <summary>
    /// Attempts to interact with the closest interactable object within range.
    /// Returns true if an interaction was initiated, false otherwise.
    /// </summary>
    public void OnInteract()
    {
        logger.Log("Player interaction triggered.", this);
        StartCoroutine(CheckClosestInteractable());
    }

    /// <summary>
    /// Checks for the closest interactable object within the interaction radius
    /// and initiates interaction if found.
    /// </summary>
    private IEnumerator CheckClosestInteractable()
    {
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
            yield break;
        }

        logger.Log("Interacting with: " + closestInteractable, this);
        var interactedId = closestInteractable.Interact(this);
        // npcInteractedEvent.Raise(new NpcInteractedData { PlayerId = "0", NpcId = interactedId });
    }

    public void FinishInteracting()
    {
        logger.Log("Finished interacting.", this);
        // OnInteractFinished?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
