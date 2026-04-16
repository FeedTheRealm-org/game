using FTR.Core.Common.Utils;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Environment.Portal;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class TeleportSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private PortalRegistry portalRegistry;

        private MovementSystem movementSystem;
        private CharacterStateStorage stateStorage;

        public void Initialize(MovementSystem movementSystem, CharacterStateStorage stateStorage)
        {
            this.movementSystem = movementSystem;
            this.stateStorage = stateStorage;
        }

        public void GameTick(float dt) { }

        public void OnTeleport(string portalId)
        {
            if (portalRegistry.TryGetPortal(portalId, out var portalInfo))
            {
                if (
                    Vector3.Distance(stateStorage.Position, portalInfo.PlacementData.position)
                    <= portalInfo.PlacementData.radius
                )
                    movementSystem.LoadPosition(portalInfo.Destination);
                else
                    Debug.LogError(
                        $"[TeleportSystem] Player is not within portal radius for {portalId}. | Cannot teleport player."
                    );
            }
            else
                Debug.LogError(
                    $"[TeleportSystem] No portal found with id {portalId} | Cannot teleport player."
                );
        }
    }
}
