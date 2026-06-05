using FTR.Core.Client.Config;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Client.Utils;
using FTR.Core.Common.Config;
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

    [Inject]
    private Config config;

    [Inject]
    private ClientConfig clientConfig;

    // Injected at Initialize
    private Rigidbody rb;
    private CharacterStateStorage stateStorage;
    private bool isInitialized = false;
    private float capsuleRadius;

    private Vector3 positionError = Vector3.zero;

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

        // Interpolation smooths the render position between physics steps.
        this.rb.interpolation = RigidbodyInterpolation.Interpolate;

        var capsuleCollider = rb.GetComponent<CapsuleCollider>();
        capsuleRadius =
            capsuleCollider != null ? capsuleCollider.radius * rb.transform.lossyScale.x : 0.5f;

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
        positionError = Vector3.zero;
        isMoving = false;
        animator.SetMoving(false);
        animator.SetDashing(false);
    }

    private void HandleRespawn()
    {
        isDead = false;
        soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Spawn, transform.position);
    }

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
            positionError = Vector3.zero;
            return;
        }

        Vector3 predictedDelta = GetPredictedDelta();

        Vector3 correctionStep = Vector3.zero;
        if (positionError.sqrMagnitude > clientConfig.ErrorMargin * clientConfig.ErrorMargin)
        {
            float t = Mathf.Clamp01(clientConfig.CorrectionSpeed * Time.fixedDeltaTime);
            correctionStep = positionError * t;
            positionError -= correctionStep;
        }
        else
        {
            positionError = Vector3.zero;
        }

        Vector3 safeDelta = ClampDeltaToWalls(predictedDelta + correctionStep);
        rb.MovePosition(rb.position + safeDelta);

        UpdateFootsteps();
    }

    private Vector3 GetPredictedDelta()
    {
        if (currentDirection == Vector3.zero)
            return Vector3.zero;

        Vector3 horizontalDirection = new Vector3(currentDirection.x, 0f, currentDirection.z);
        if (horizontalDirection == Vector3.zero)
            return Vector3.zero;

        return horizontalDirection * Time.fixedDeltaTime;
    }

    private Vector3 ClampDeltaToWalls(Vector3 delta)
    {
        if (delta == Vector3.zero)
            return Vector3.zero;

        Vector3 deltaNormalized = delta.normalized;
        Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;

        Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
        Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;

        LayerMask blockingLayers = config.CubeColliderLayerMask | config.SlopeColliderLayerMask;

        bool hitLeft = Physics.Raycast(
            leftOrigin,
            deltaNormalized,
            out RaycastHit leftHit,
            delta.magnitude,
            blockingLayers
        );
        bool hitRight = Physics.Raycast(
            rightOrigin,
            deltaNormalized,
            out RaycastHit rightHit,
            delta.magnitude,
            blockingLayers
        );

        if (hitLeft || hitRight)
        {
            float stopDistance =
                hitLeft && hitRight ? Mathf.Min(leftHit.distance, rightHit.distance)
                : hitLeft ? leftHit.distance
                : rightHit.distance;

            return deltaNormalized * Mathf.Max(0f, stopDistance - Physics.defaultContactOffset);
        }

        return delta;
    }

    private void OnDrawGizmos()
    {
        if (rb == null || !isInitialized)
            return;

        Vector3 deltaNormalized = currentDirection.normalized;
        if (deltaNormalized == Vector3.zero)
            return;

        Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;
        Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
        Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;
        float rayLength = 2f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(leftOrigin, leftOrigin + deltaNormalized * rayLength);
        Gizmos.DrawWireSphere(leftOrigin, 0.05f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(rightOrigin, rightOrigin + deltaNormalized * rayLength);
        Gizmos.DrawWireSphere(rightOrigin, 0.05f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(leftOrigin, rightOrigin);
    }

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

    private void OnPositionCorrected(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(rb.position, targetPosition);

        if (distance > clientConfig.MovementCorrectionTolerance)
        {
            rb.position = targetPosition;
            rb.linearVelocity = Vector3.zero;
            positionError = Vector3.zero;
            return;
        }

        positionError = targetPosition - rb.position;
    }

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
