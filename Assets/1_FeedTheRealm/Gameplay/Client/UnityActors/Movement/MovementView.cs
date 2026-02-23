using FTR.Core.Client.Enums;
using FTR.Core.Client.Utils;
using UnityEngine;

public class MovementView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    // Injected at Initialize
    private Rigidbody rb;

    private bool isInitialized = false;

    public void Initialize(Rigidbody rb)
    {
        this.rb = rb;
        isInitialized = true;
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
