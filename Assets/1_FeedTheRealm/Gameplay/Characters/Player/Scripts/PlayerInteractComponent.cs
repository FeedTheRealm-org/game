using System;
using Game.Core.Interactions;
using UnityEngine;

public class PlayerInteractComponent : MonoBehaviour, IInteractor
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

    public event Action OnInteractFinished;

    /// <summary>
    /// Attempts to interact with the closest interactable object within range.
    /// Returns true if an interaction was initiated, false otherwise.
    /// </summary>
    public bool OnInteract()
    {
        logger.Log("Player interaction triggered.", this);
        return CheckClosestInteractable();
    }

    private bool CheckClosestInteractable()
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
            return false;

        logger.Log("Interacting with: " + closestInteractable, this);
        closestInteractable.Interact(this);
        return true;
    }

    public void FinishInteracting()
    {
        logger.Log("Finished interacting.", this);
        OnInteractFinished?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
}
