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
                        HandleMovementWithRaycasts(direction * (currentSpeed * dt), currentSpeed);
                    }
                    else
                    {
                        HandleMovementWithRaycasts(direction * (currentSpeed * dt), currentSpeed);
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
                    HandleMovementWithRaycasts(direction * (currentSpeed * dt), currentSpeed);
            }

            wasGroundedLastTick = isGrounded;

            if (gameTickCounter % positionCorrectionCounter == 0)
                stateStorage.CorrectPosition(rb.position);

            gameTickCounter++;
        }

        private void HandleMovementWithRaycasts(Vector3 delta, float currentSpeed)
        {
            if (currentSpeed >= config.TunnelingRiskSpeed)
            {
                Vector3 deltaNormalized = delta.normalized;
                Vector3 perpendicular = Vector3.Cross(deltaNormalized, Vector3.up).normalized;

                Vector3 leftOrigin = rb.position - perpendicular * capsuleRadius;
                Vector3 rightOrigin = rb.position + perpendicular * capsuleRadius;

                LayerMask blockingLayers =
                    config.CubeColliderLayerMask | config.SlopeColliderLayerMask;

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

                    rb.MovePosition(
                        rb.position
                            + deltaNormalized * (stopDistance - Physics.defaultContactOffset)
                    );
                    return;
                }
            }

            rb.MovePosition(rb.position + delta);
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
            float rayLength = 2f; // visual length, not tied to delta

            // Left raycast
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftOrigin, leftOrigin + deltaNormalized * rayLength);
            Gizmos.DrawWireSphere(leftOrigin, 0.05f);

            // Right raycast
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(rightOrigin, rightOrigin + deltaNormalized * rayLength);
            Gizmos.DrawWireSphere(rightOrigin, 0.05f);

            // Capsule radius indicator
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                rb.position - perpendicular * capsuleRadius,
                rb.position + perpendicular * capsuleRadius
            );
        }
    }
}
