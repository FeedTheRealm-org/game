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

    // Must match ServerConfig.MovementRaycastAngle so prediction casts the same rays as the server.
    [SerializeField]
    private float movementRaycastAngle = 30f;

    // Injected at Initialize
    private Rigidbody rb;
    private CharacterStateStorage stateStorage;
    private bool isInitialized = false;
    private float capsuleRadius;
    private CapsuleCollider capsuleCollider;

    private Vector3 positionError = Vector3.zero;

    private Vector3 currentDirection = Vector3.zero;
    private bool isDead = false;
    private bool wasGroundedLastTick = false;
    private float rayLength;

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

        capsuleCollider = rb.GetComponent<CapsuleCollider>();
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

        float dt = Time.fixedDeltaTime;

        // The synced direction has speed baked in (server SetDirection(dir.normalized * speed)).
        // The server's grounded-idle branch syncs raw velocity, so strip the vertical component to
        // recover the horizontal movement input exactly as the server's `direction` field holds it.
        Vector3 inputDirection = new Vector3(currentDirection.x, 0f, currentDirection.z);
        float currentSpeed = inputDirection.magnitude;

        bool isGrounded = stateStorage.IsGrounded;
        bool justLanded = isGrounded && !wasGroundedLastTick;

        rb.useGravity = !(isGrounded && stateStorage.IsOnSlope);

        Vector3 targetPosition = rb.position;

        if (isGrounded)
        {
            if (justLanded)
            {
                currentDirection = Vector3.zero;
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            else if (inputDirection != Vector3.zero)
            {
                if (stateStorage.IsOnSlope)
                {
                    Vector3 moveDirection = Vector3
                        .ProjectOnPlane(inputDirection.normalized, stateStorage.GroundNormal)
                        .normalized;
                    targetPosition = HandleMovementWithRaycasts(
                        moveDirection * (currentSpeed * dt)
                    );
                }
                else
                {
                    targetPosition = HandleMovementWithRaycasts(inputDirection * dt);
                }
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
        else
        {
            if (inputDirection != Vector3.zero)
                targetPosition = HandleMovementWithRaycasts(inputDirection * dt);
        }

        wasGroundedLastTick = isGrounded;

        Vector3 correctionStep = Vector3.zero;
        if (positionError.sqrMagnitude > clientConfig.ErrorMargin * clientConfig.ErrorMargin)
        {
            float t = Mathf.Clamp01(clientConfig.CorrectionSpeed * dt);
            correctionStep = positionError * t;
            positionError -= correctionStep;
        }
        else
        {
            positionError = Vector3.zero;
        }

        Vector3 finalPosition = targetPosition + correctionStep;
        if (finalPosition != rb.position)
            rb.MovePosition(finalPosition);

        UpdateFootsteps();
    }

    private Vector3 HandleMovementWithRaycasts(Vector3 delta)
    {
        Vector3 deltaNormalized = delta.normalized;
        rayLength = delta.magnitude;

        LayerMask blockingLayers = config.CubeColliderLayerMask | config.SlopeColliderLayerMask;

        Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;
        Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
        Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;
        Vector3 centerOrigin = rb.position;

        float angle = movementRaycastAngle;
        Vector3 leftDir = Quaternion.Euler(0, angle, 0) * deltaNormalized;
        Vector3 rightDir = Quaternion.Euler(0, -angle, 0) * deltaNormalized;

        float closest = float.MaxValue;
        bool anyHit = false;

        (Vector3 origin, Vector3 dir)[] rays =
        {
            (leftOrigin, leftDir),
            (centerOrigin, deltaNormalized),
            (rightOrigin, rightDir),
        };

        foreach (var (origin, dir) in rays)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit h, rayLength, blockingLayers))
            {
                if (h.distance < closest)
                {
                    closest = h.distance;
                    anyHit = true;
                }
            }
        }

        Vector3 targetPosition = anyHit
            ? rb.position + deltaNormalized * Mathf.Max(0f, closest - Physics.defaultContactOffset)
            : rb.position + delta;

        if (anyHit)
            targetPosition = ResolveOverlap(targetPosition);

        return targetPosition;
    }

    // ResolveOverlap performs a depenetration pass at the target position to
    // ensure we don't end up stuck inside geometry due to tunneling or missed raycasts
    private Vector3 ResolveOverlap(Vector3 targetPosition)
    {
        if (capsuleCollider == null)
            return targetPosition;

        Collider[] overlaps = Physics.OverlapSphere(
            targetPosition,
            capsuleRadius,
            config.CubeColliderLayerMask | config.SlopeColliderLayerMask
        );

        foreach (Collider other in overlaps)
        {
            if (other.attachedRigidbody == rb)
                continue;

            if (
                Physics.ComputePenetration(
                    capsuleCollider,
                    targetPosition,
                    rb.rotation,
                    other,
                    other.transform.position,
                    other.transform.rotation,
                    out Vector3 pushDir,
                    out float pushDist
                )
            )
            {
                targetPosition += pushDir * (pushDist + Physics.defaultContactOffset);
            }
        }

        return targetPosition;
    }

    private void OnDrawGizmos()
    {
        if (rb == null || !isInitialized)
            return;

        Vector3 deltaNormalized = new Vector3(
            currentDirection.x,
            0f,
            currentDirection.z
        ).normalized;
        if (deltaNormalized == Vector3.zero)
            return;

        Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;
        Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
        Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;
        Vector3 centerOrigin = rb.position;

        Vector3 leftDir = Quaternion.Euler(0, movementRaycastAngle, 0) * deltaNormalized;
        Vector3 rightDir = Quaternion.Euler(0, -movementRaycastAngle, 0) * deltaNormalized;

        // Left ray (angled)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(leftOrigin, leftOrigin + leftDir * rayLength);
        Gizmos.DrawWireSphere(leftOrigin, 0.05f);

        // Center ray
        Gizmos.color = Color.green;
        Gizmos.DrawLine(centerOrigin, centerOrigin + deltaNormalized * rayLength);
        Gizmos.DrawWireSphere(centerOrigin, 0.05f);

        // Right ray (angled)
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(rightOrigin, rightOrigin + rightDir * rayLength);
        Gizmos.DrawWireSphere(rightOrigin, 0.05f);

        // Capsule width indicator
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
        Vector3 error = targetPosition - rb.position;
        bool snap =
            stateStorage.IsTeleporting
            || error.sqrMagnitude
                > clientConfig.MovementCorrectionTolerance
                    * clientConfig.MovementCorrectionTolerance;

        if (snap)
        {
            rb.position = targetPosition;
            rb.linearVelocity = Vector3.zero;
            positionError = Vector3.zero;
            return;
        }
        positionError = error;
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
