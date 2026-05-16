using FTR.Core.Common.Config;
using FTR.Core.Common.Systems.Status;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class GroundCheckSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerConfig config;
        private Collider col;
        private IGroundable stateStorage;
        private Vector3 groundCheckSphereOrigin;
        private float groundCheckDistance => stateStorage.GetGroundCheckDistance();

        private void OnDisable()
        {
            if (gameTickEvent != null)
                gameTickEvent.OnRaised -= GameTick;
        }

        public void Initialize(Collider col, IGroundable stateStorage)
        {
            this.col = col;
            this.stateStorage = stateStorage;
            gameTickEvent.OnRaised += GameTick;
        }

        public void GameTick(float dt)
        {
            // TODO(optimization): consider skipping a few frames after being grounded to avoid doing expensive checks every frame.
            if (!stateStorage.IsGroundCheckEnabled)
                return;
            Bounds bounds = col.bounds;
            groundCheckSphereOrigin = new Vector3(
                bounds.center.x,
                bounds.center.y,
                bounds.center.z
            );
            stateStorage.IsGrounded = IsCollidedWithGround();
        }

        private bool IsCollidedWithGround()
        {
            bool isGrounded = Physics.SphereCast(
                groundCheckSphereOrigin,
                config.GroundCheckSphereRadius,
                Vector3.down,
                out RaycastHit groundHit,
                groundCheckDistance,
                config.GroundLayer | config.SlopeLayer
            );

            if (!isGrounded)
            {
                stateStorage.IsOnSlope = false;
                stateStorage.GroundNormal = Vector3.up;
                return false;
            }

            // Reject surfaces that are too vertical to stand on.
            // A dot product of the hit normal against Vector3.up gives the cosine of the angle:
            // 1.0 = flat ground, 0.0 = vertical wall, negative = ceiling.
            float normalAlignment = Vector3.Dot(groundHit.normal, Vector3.up);
            if (normalAlignment < config.MinGroundNormalAlignment)
            {
                stateStorage.IsOnSlope = false;
                stateStorage.GroundNormal = Vector3.up;
                return false;
            }

            if (
                Physics.SphereCast(
                    groundCheckSphereOrigin,
                    config.GroundCheckSphereRadius,
                    Vector3.down,
                    out RaycastHit slopeHit,
                    groundCheckDistance,
                    config.SlopeLayer
                )
            )
            {
                stateStorage.IsOnSlope = true;
                stateStorage.GroundNormal = slopeHit.normal;
            }
            else
            {
                stateStorage.IsOnSlope = false;
                stateStorage.GroundNormal = Vector3.up;
            }

            return true;
        }

        private void OnDrawGizmos()
        {
            if (col == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                groundCheckSphereOrigin,
                groundCheckSphereOrigin + Vector3.down * groundCheckDistance
            );
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                groundCheckSphereOrigin,
                groundCheckSphereOrigin + stateStorage.GroundNormal * 2f
            );

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                groundCheckSphereOrigin + Vector3.down * groundCheckDistance,
                config.GroundCheckSphereRadius
            );
        }
    }
}
