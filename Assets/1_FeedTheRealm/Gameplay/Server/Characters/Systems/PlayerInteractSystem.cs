using FTR.Core.Common.Interactions;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
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

    public uint NetId { get; private set; }
    public IInteractable CurrentInteractable { get; private set; }

    private WorldMonitor worldMonitor;
    private uint ownNetId;

    public void Initialize(uint netId, WorldMonitor worldMonitor, uint ownNetId)
    {
        this.NetId = netId;
        this.worldMonitor = worldMonitor;
        this.ownNetId = ownNetId;
    }

    /// <summary>
    /// Finds the closest IInteractable in range and either starts or continues the interaction.
    /// </summary>
    public void TryInteract(IEventCollectable ec)
    {
        IInteractable closest = FindClosestInteractable();

        if (closest == null)
        {
            logger?.Log("[PlayerInteractSystem] No interactable found — notifying client.", this);
            SendInteractFailed();
            FinishInteracting();
            return;
        }

        if (CurrentInteractable != null && CurrentInteractable != closest)
            CurrentInteractable.StopInteraction(this);

        CurrentInteractable = closest;
        CurrentInteractable.Interact(this);
    }

    public void FinishInteracting()
    {
        if (CurrentInteractable != null)
        {
            CurrentInteractable.StopInteraction(this);
            CurrentInteractable = null;
        }
    }

    private IInteractable FindClosestInteractable()
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            interactableLayerMask
        );

        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            IInteractable interactable =
                col.GetComponent<IInteractable>() ?? col.GetComponentInChildren<IInteractable>();

            if (interactable == null || !interactable.CanInteract(this))
                continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }

    private void SendInteractFailed()
    {
        if (worldMonitor == null)
            return;

        int? connId = GetPlayerConnectionId(NetId);
        if (!connId.HasValue)
        {
            logger?.Log(
                $"[PlayerInteractSystem] SendInteractFailed — connection not found for Player:{NetId}.",
                this
            );
            return;
        }

        worldMonitor.Events.Enqueue(new InteractFailedEvent(ownNetId, connId.Value));
    }

    private int? GetPlayerConnectionId(uint playerNetId)
    {
        if (
            worldMonitor.Entities.TryGet(playerNetId, out var entity)
            && entity.ConnectionId.HasValue
        )
            return entity.ConnectionId.Value;

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
