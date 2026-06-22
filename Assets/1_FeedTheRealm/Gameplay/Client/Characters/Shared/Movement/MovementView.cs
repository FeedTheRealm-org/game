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

    [Header("Ground Check (must mirror the server's ground check)")]
    // Surfaces tilted more than this count as a slope -> movement projected onto the plane.
    [SerializeField]
    private float slopeAngleThreshold = 1f;

    // Walkable ceiling. Match the value the server uses to classify a slope.
    [SerializeField]
    private float maxSlopeAngle = 50f;

    // Extra downward reach while already grounded. Stops the center down-ray from
    // momentarily overshooting the surface on a slope and flipping grounded/onSlope off.
    [SerializeField]
    private float groundStickDistance = 0.3f;

    // Injected at Initialize
    private Rigidbody rb;
    private CharacterStateStorage stateStorage;
    private bool isInitialized = false;
    private float capsuleRadius;
    private CapsuleCollider capsuleCollider;
    private LayerMask groundAndBlockingLayers;

    private Vector3 positionError = Vector3.zero;

    private Vector3 currentDirection = Vector3.zero;
    private bool isDead = false;
    private bool wasGroundedLastTick = false;
    private float rayLength;

    // Ground state computed locally each tick (these are NOT synced from the server,
    // they are plain properties on CharacterStateStorage, so the client must derive them itself).
    private bool isGrounded = false;
    private bool isOnSlope = false;
    private Vector3 groundNormal = Vector3.up;

    [Header("Footstep Settings")]
    [SerializeField]
    private float footstepInterval = 0.35f;

    [SerializeField]
    private bool repeatFootsteps = true;

    private float footstepTimer = 0f;

    private bool wasMoving = false;
    private bool isMoving = false;

    private bool hasPlayedSingleFootstep = false;

    public void Initialize(Rigidbody rb, CharacterStateStorage stateStorage)
    {
        this.rb = rb;
        this.stateStorage = stateStorage;

        // Interpolation smooths the render position between physics steps.
        this.rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Gravity is owned by the server. The client never applies it: all vertical motion
        // comes from slope projection (climbing) or from the position SyncVar (falling).
        // This removes the single-tick gravity spikes that bumped the body on slopes.
        this.rb.useGravity = false;

        capsuleCollider = rb.GetComponent<CapsuleCollider>();
        capsuleRadius =
            capsuleCollider != null ? capsuleCollider.radius * rb.transform.lossyScale.x : 0.5f;

        groundAndBlockingLayers = config.CubeColliderLayerMask | config.SlopeColliderLayerMask;

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

        // Derive ground state locally; the server does the same with its own ground check
        // and never syncs the result (IsGrounded / IsOnSlope / GroundNormal are not SyncVars).
        UpdateGroundState();

        // The synced direction has speed baked in (server SetDirection(dir.normalized * speed)).
        // The server's grounded-idle branch syncs raw velocity, so strip the vertical component to
        // recover the horizontal movement input exactly as the server's `direction` field holds it.
        Vector3 inputDirection = new Vector3(currentDirection.x, 0f, currentDirection.z);
        float currentSpeed = inputDirection.magnitude;

        bool justLanded = isGrounded && !wasGroundedLastTick;

        Vector3 targetPosition = rb.position;

        if (isGrounded)
        {
            if (justLanded)
            {
                currentDirection = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
            }
            else if (inputDirection != Vector3.zero)
            {
                if (isOnSlope)
                {
                    // Movement is fully positional with gravity off; clear any residual velocity
                    // so nothing tugs the body off the slope plane mid-climb.
                    rb.linearVelocity = Vector3.zero;

                    Vector3 moveDirection = Vector3
                        .ProjectOnPlane(inputDirection.normalized, groundNormal)
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
                rb.linearVelocity = Vector3.zero;
            }
        }
        else
        {
            // Airborne: no gravity on the client, so Y is held here and the server's fall
            // arrives through the position SyncVar / correction path.
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

    // Mirrors the server's ground check so prediction classifies flat/slope/airborne the same way.
    // Cast shape and thresholds must match whatever the server uses; the cast distance is shared
    // via CharacterStateStorage.GetGroundCheckDistance().
    private void UpdateGroundState()
    {
        if (!stateStorage.IsGroundCheckEnabled)
        {
            isGrounded = false;
            isOnSlope = false;
            groundNormal = Vector3.up;
            return;
        }

        // wasGroundedLastTick still holds last tick's value here (it's reassigned at the end of FixedTick).
        // While grounded we extend the cast so a momentary overshoot on a slope can't drop grounding.
        float distance =
            stateStorage.GetGroundCheckDistance()
            + (wasGroundedLastTick ? groundStickDistance : 0f);

        if (
            Physics.Raycast(
                rb.position,
                Vector3.down,
                out RaycastHit hit,
                distance,
                groundAndBlockingLayers
            )
        )
        {
            isGrounded = true;
            groundNormal = hit.normal;
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            isOnSlope = angle > slopeAngleThreshold && angle <= maxSlopeAngle;
        }
        else
        {
            isGrounded = false;
            isOnSlope = false;
            groundNormal = Vector3.up;
        }
    }

    private Vector3 HandleMovementWithRaycasts(Vector3 delta)
    {
        Vector3 deltaNormalized = delta.normalized;
        rayLength = delta.magnitude;

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
            if (Physics.Raycast(origin, dir, out RaycastHit h, rayLength, groundAndBlockingLayers))
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
            groundAndBlockingLayers
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
        currentDirection = direction;
        wasMoving = isMoving;
        isMoving = VectorTransformations.IsMovementMagnitude(direction);
        UpdateFacingDirection(direction);
        AnimateMovement(direction);
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

    private void AnimateMovement(Vector3 direction)
    {
        if (isMoving && !wasMoving)
        {
            animator.SetMoving(true);
            animator.SetDashing(false);
        }
        else if (wasMoving && !isMoving)
        {
            animator.SetMoving(false);
            animator.SetDashing(false);
        }
    }

    private void OnDrawGizmos()
    {
        if (rb == null || !isInitialized)
            return;

        // Ground check ray
        Gizmos.color = isOnSlope ? Color.red : (isGrounded ? Color.green : Color.gray);
        Gizmos.DrawLine(
            rb.position,
            rb.position + Vector3.down * stateStorage.GetGroundCheckDistance()
        );
        if (isGrounded)
            Gizmos.DrawLine(rb.position, rb.position + groundNormal);

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
}
