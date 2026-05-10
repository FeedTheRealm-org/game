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
            // TODO(optimization): why spherecast + raycast?
            bool result = Physics.SphereCast(
                groundCheckSphereOrigin,
                config.GroundCheckSphereRadius,
                Vector3.down,
                out RaycastHit _,
                groundCheckDistance,
                config.GroundLayer | config.SlopeLayer
            );

            if (
                Physics.Raycast(
                    groundCheckSphereOrigin,
                    Vector3.down,
                    out RaycastHit slopeHit,
                    groundCheckDistance + config.GroundCheckSphereRadius,
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

            return result;
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
        }
    }
}
