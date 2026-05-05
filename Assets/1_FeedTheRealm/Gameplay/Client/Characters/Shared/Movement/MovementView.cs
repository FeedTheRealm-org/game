using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Client.Utils;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

public class MovementView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    [Inject]
    private FixedTickEvent fixedTickEvent;

    [Inject]
    private ISoundPlayer soundPlayer;

    // Injected at Initialize
    private Rigidbody rb;
    private CharacterStateStorage stateStorage;
    private bool isInitialized = false;

    // TODO: moves these to a proper config or constants class later
    private const float errorMargin = 0.001f;
    private const float correctionSpeed = 10f;
    private bool correctingPosition = false;
    private Vector3 positionCorrectionTarget;

    private Vector3 currentDirection = Vector3.zero;
    private bool isDead = false;

    [Header("Footstep Settings")]
    [SerializeField]
    private float footstepInterval = 0.35f;

    [SerializeField]
    private bool repeatFootsteps = true;

    private float footstepTimer = 0f;
    private bool isMoving = false;
    private bool hasPlayedSingleFootstep = false;

    public void Initialize(Rigidbody rb, CharacterStateStorage stateStorage)
    {
        this.rb = rb;
        this.stateStorage = stateStorage;

        this.stateStorage.OnDirectionChanged += OnDirectionChanged;
        this.stateStorage.OnPositionCorrected += OnPositionCorrected;
        this.stateStorage.OnDeath += HandleDeath;
        this.stateStorage.OnRespawn += HandleRespawn;

        isInitialized = true;
        fixedTickEvent.OnRaised += FixedTick;
    }

    private void HandleDeath()
    {
        isDead = true;
        currentDirection = Vector3.zero;
        isMoving = false;
        animator.SetMoving(false);
        animator.SetDashing(false);
    }

    private void HandleRespawn()
    {
        isDead = false;
    }

    // TODO: review if we need to unsubscribe from events on disable/destroy,
    // or if the lifetime of this component is guaranteed to be the same as the character's lifetime
    private void OnDestroy()
    {
        fixedTickEvent.OnRaised -= FixedTick;
        stateStorage.OnDirectionChanged -= OnDirectionChanged;
        stateStorage.OnPositionCorrected -= OnPositionCorrected;
        stateStorage.OnDeath -= HandleDeath;
        stateStorage.OnRespawn -= HandleRespawn;
    }

    private void FixedTick()
    {
        if (!isInitialized || isDead)
            return;

        if (stateStorage.IsMovementBlocked)
        {
            correctingPosition = false;
            return;
        }

        if (correctingPosition)
        {
            rb.position = Vector3.Lerp(
                rb.position,
                positionCorrectionTarget,
                correctionSpeed * Time.fixedDeltaTime
            );

            if (Vector3.Distance(rb.position, positionCorrectionTarget) < errorMargin)
            {
                rb.position = positionCorrectionTarget;
                correctingPosition = false;
            }
        }
        else
        {
            Vector3 newPosition = rb.position + currentDirection * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }

        UpdateFootsteps();
    }

    /// <summary>
    /// OnVelocityChanged receives the authoritative velocity from the server.
    /// This is what actually moves the character.
    /// </summary>
    private void UpdateFootsteps()
    {
        if (!isMoving)
        {
            footstepTimer = 0f;
            hasPlayedSingleFootstep = false;
            return;
        }

        if (!repeatFootsteps)
        {
            if (!hasPlayedSingleFootstep)
            {
                PlayFootstepSound();
                hasPlayedSingleFootstep = true;
            }
            return;
        }

        footstepTimer += Time.fixedDeltaTime;

        if (footstepTimer >= footstepInterval)
        {
            PlayFootstepSound();
            footstepTimer = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Walking, transform.position);
    }

    private void OnDirectionChanged(Vector3 direction)
    {
        UpdateFacingDirection(direction);
        AnimateMovement(direction);
        currentDirection = direction;
    }

    /// <summary>
    /// OnPositionCorrected is used for reconciliation and error correction, periodically.
    /// </summary>
    private void OnPositionCorrected(Vector3 targetPosition)
    {
        correctingPosition = true;
        positionCorrectionTarget = targetPosition;
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

    private void AnimateMovement(Vector3 velocity)
    {
        bool wasMoving = isMoving;
        isMoving = velocity.sqrMagnitude > Vector3.zero.sqrMagnitude;

        if (isMoving)
        {
            if (!wasMoving)
            {
                animator.SetMoving(true);
                animator.SetDashing(false);
            }
        }
        else
        {
            if (wasMoving)
            {
                animator.SetMoving(false);
                animator.SetDashing(false);
            }
        }
    }
}
