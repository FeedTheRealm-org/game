using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Persistence;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class MovementSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private GameTickEvent gameTickEvent;
        private uint netId;

        [SerializeField]
        private ServerConfig serverConfig;

        [SerializeField]
        private Config config;
        private bool isInitialized = false;
        private Rigidbody rb;
        private CharacterStateStorage stateStorage;
        private Vector3 direction = Vector3.zero;

        private float moveSpeed = 5f;
        private float positionCorrectionCounter = 3;
        private float gameTickCounter = 0;

        private float speedBuffAmount = 0f;
        private float speedBuffTimer = 0f;

        private bool isDead = false;
        private bool wasGroundedLastTick = false;
        private float capsuleRadius;
        private float rayLength;

        public Vector3 GetCurrentPosition() => rb.position;

        private void OnDestroy()
        {
            if (gameTickEvent != null)
                gameTickEvent.OnRaised -= GameTick;

            if (stateStorage != null)
            {
                stateStorage.OnDeath -= HandleDeath;
                stateStorage.OnRespawn -= HandleRespawn;
            }
        }

        public void Initialize(uint netId, Rigidbody rb, CharacterStateStorage stateStorage)
        {
            this.netId = netId;
            this.rb = rb;
            this.stateStorage = stateStorage;

            this.stateStorage.OnDeath += HandleDeath;
            this.stateStorage.OnRespawn += HandleRespawn;

            var capsuleCollider = rb.GetComponent<CapsuleCollider>();
            capsuleRadius =
                capsuleCollider != null ? capsuleCollider.radius * rb.transform.lossyScale.x : 0.5f;

            moveSpeed = serverConfig.PlayerSpeed > 0 ? serverConfig.PlayerSpeed : moveSpeed;
            gameTickEvent.OnRaised += GameTick;

            isInitialized = true;
        }

        public void LoadPosition(Vector3 position)
        {
            rb.position = position;
            stateStorage.CorrectPosition(rb.position);
            Debug.Log($"Loaded position for player {stateStorage.CharacterId}: {position}");
        }

        public void OnMove(Vector3 direction)
        {
            if (!stateStorage.IsGrounded || isDead)
                return;

            this.direction = direction.normalized;
            float totalSpeed = moveSpeed + speedBuffAmount;
            stateStorage.SetDirection(this.direction * totalSpeed);
        }

        public void ApplySpeedBuff(float boost, float duration)
        {
            speedBuffAmount = boost;
            speedBuffTimer = duration;
        }

        public void GameTick(float dt)
        {
            if (!isInitialized || stateStorage.IsMovementBlocked || isDead)
                return;

            if (speedBuffTimer > 0)
            {
                speedBuffTimer -= dt;
                if (speedBuffTimer <= 0)
                    speedBuffAmount = 0f;
            }

            float currentSpeed = moveSpeed + speedBuffAmount;
            bool isGrounded = stateStorage.IsGrounded;
            bool justLanded = isGrounded && !wasGroundedLastTick;

            rb.useGravity = !(isGrounded && stateStorage.IsOnSlope);

            if (isGrounded)
            {
                if (justLanded)
                {
                    direction = Vector3.zero;
                    stateStorage.SetDirection(Vector3.zero);
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                }
                else if (direction != Vector3.zero)
                {
                    if (stateStorage.IsOnSlope)
                    {
                        Vector3 moveDirection = Vector3
                            .ProjectOnPlane(direction, stateStorage.GroundNormal)
                            .normalized;
                        HandleMovementWithRaycasts(moveDirection * (currentSpeed * dt));
                    }
                    else
                    {
                        HandleMovementWithRaycasts(direction * (currentSpeed * dt)); // same
                    }
                }
                else
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                    stateStorage.SetDirection(rb.linearVelocity);
                }
            }
            else
            {
                if (direction != Vector3.zero)
                    HandleMovementWithRaycasts(direction * (currentSpeed * dt));
            }

            wasGroundedLastTick = isGrounded;

            if (gameTickCounter % positionCorrectionCounter == 0)
                stateStorage.CorrectPosition(rb.position);

            gameTickCounter++;
        }

        private void HandleMovementWithRaycasts(Vector3 delta)
        {
            Vector3 deltaNormalized = delta.normalized;
            rayLength = delta.magnitude;

            LayerMask blockingLayers = config.CubeColliderLayerMask | config.SlopeColliderLayerMask;

            Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;
            Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
            Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;
            Vector3 centerOrigin = rb.position;

            float angle = serverConfig.MovementRaycastAngle;
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
                ? rb.position
                    + deltaNormalized * Mathf.Max(0f, closest - Physics.defaultContactOffset)
                : rb.position + delta;

            if (anyHit)
                targetPosition = ResolveOverlap(targetPosition);

            rb.MovePosition(targetPosition);
        }

        // ResolveOverlap performs a depenetration pass at the target position to
        // ensure we don't end up stuck inside geometry due to tunneling or missed raycasts
        private Vector3 ResolveOverlap(Vector3 targetPosition)
        {
            var capsuleCollider = rb.GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
                return targetPosition;

            // Find everything overlapping at the destination
            Collider[] overlaps = Physics.OverlapSphere(
                targetPosition,
                capsuleRadius,
                config.CubeColliderLayerMask | config.SlopeColliderLayerMask
            );

            // For each overlapping collider, calculates exactly how far (pushDist)
            // and in what direction (pushDir) to push the player out of it.
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

        private void HandleDeath()
        {
            isDead = true;
            direction = Vector3.zero;
        }

        private void HandleRespawn()
        {
            isDead = false;
        }

        private void OnDrawGizmos()
        {
            if (rb == null || !isInitialized)
                return;

            Vector3 deltaNormalized = direction.normalized;
            if (deltaNormalized == Vector3.zero)
                return;

            Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;
            Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
            Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;
            Vector3 centerOrigin = rb.position;

            float angle = serverConfig != null ? serverConfig.MovementRaycastAngle : 30f;
            Vector3 leftDir = Quaternion.Euler(0, angle, 0) * deltaNormalized;
            Vector3 rightDir = Quaternion.Euler(0, -angle, 0) * deltaNormalized;

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
            Gizmos.DrawLine(
                rb.position - perpendicular * capsuleRadius,
                rb.position + perpendicular * capsuleRadius
            );
        }
    }
}
