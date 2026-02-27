using FTR.Core.Client.Enums;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Client.Utils;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

public class MovementView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    [Inject]
    private TickEvent tickEvent;

    // Injected at Initialize
    private Rigidbody rb;
    private CharacterStateStorage stateStorage;

    private bool isInitialized = false;

    public void Initialize(Rigidbody rb, CharacterStateStorage stateStorage)
    {
        this.rb = rb;
        this.stateStorage = stateStorage;

        this.stateStorage.OnVelocityChanged += OnVelocityChanged;
        this.stateStorage.OnPositionCorrected += OnPositionCorrected;

        isInitialized = true;
    }

    private void OnEnable()
    {
        tickEvent.OnRaised += Tick;
    }

    private void OnDisable()
    {
        tickEvent.OnRaised -= Tick;
    }

    private void Tick() { }

    /// <summary>
    /// OnVelocityChanged is used to update the animation parameters based on the current velocity when it changes.
    /// </summary>
    private void OnVelocityChanged(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        UpdateFacingDirection(velocity);
    }

    /// <summary>
    /// OnPositionCorrected is used for reconciliation and error correction, periodically.
    /// </summary>
    private void OnPositionCorrected(Vector3 position)
    {
        rb.position = position;
    }

    /// <summary>
    /// Moves the Rigidbody to the specified position and starts the animation.
    /// </summary>
    public void MoveToPosition(Vector3 nextPosition)
    {
        if (!isInitialized)
            throw new MissingComponentException(
                "MovementView must be initialized before calling MoveToPosition."
            );

        rb.MovePosition(nextPosition);

        if (!animator.IsMoving())
        {
            animator.SetMoving(true);
            animator.SetDashing(false);
        }
    }

    /// <summary>
    /// Updates the facing direction based on the movement direction and camera orientation.
    /// </summary>
    public void UpdateFacingDirection(Vector3 direction)
    {
        if (!isInitialized)
            throw new MissingComponentException(
                "MovementView must be initialized before calling UpdateFacingDirection."
            );

        if (direction == Vector3.zero)
        {
            animator.SetMoving(false);
            animator.SetDashing(false);
        }

        FacingDirection facing = GetFacingDirection(direction);
        if (facing == FacingDirection.None)
            return;

        animator.SetFacing(facing);
    }

    /// <summary>
    /// Determines the facing direction based on the movement direction and camera orientation.
    /// </summary>
    private FacingDirection GetFacingDirection(Vector3 direction)
    {
        if (!VectorTransformations.IsMovementMagnitude(direction))
            return FacingDirection.None;

        var cameraTransform = Camera.main.transform;

        Vector3 camForward = new Vector3(
            cameraTransform.forward.x,
            0f,
            cameraTransform.forward.z
        ).normalized;
        Vector3 camRight = new Vector3(
            cameraTransform.right.x,
            0f,
            cameraTransform.right.z
        ).normalized;

        float forwardAmount = Vector3.Dot(direction, camForward);
        float rightAmount = Vector3.Dot(direction, camRight);

        if (Mathf.Abs(forwardAmount) > Mathf.Abs(rightAmount))
            return forwardAmount > 0 ? FacingDirection.Back : FacingDirection.Front;
        else
            return rightAmount > 0 ? FacingDirection.Right : FacingDirection.Left;
    }
}
