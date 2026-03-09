using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class GroundCheckSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerConfig config;
        private Collider col;
        private CharacterStateStorage stateStorage;
        private Vector3 groundCheckSphereOrigin;

        private void OnDisable()
        {
            if (gameTickEvent != null)
                gameTickEvent.OnRaised -= GameTick;
        }

        public void Initialize(Collider col, CharacterStateStorage stateStorage)
        {
            this.col = col;
            this.stateStorage = stateStorage;
            gameTickEvent.OnRaised += GameTick;
        }

        public void GameTick(float dt)
        {
            Bounds bounds = col.bounds;
            groundCheckSphereOrigin = new Vector3(
                bounds.center.x,
                bounds.center.y,
                bounds.center.z
            );
            stateStorage.SetIsGrounded(IsColliedWithGround());
        }

        private bool IsColliedWithGround()
        {
            bool result = Physics.SphereCast(
                groundCheckSphereOrigin,
                config.GroundCheckSphereRadius,
                Vector3.down,
                out RaycastHit _,
                config.GroundCheckDistance,
                config.GroundLayer
            );

            return result;
        }
    }
}
