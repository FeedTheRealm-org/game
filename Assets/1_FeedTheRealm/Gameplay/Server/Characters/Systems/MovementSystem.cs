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
        [Inject]
        private readonly PlayersRepository playersRepository;

        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerConfig config;
        private bool isInitialized = false;
        private Rigidbody rb;
        private CharacterStateStorage stateStorage;
        private Vector3 direction = Vector3.zero;

        private float moveSpeed = 5f;
        private float positionCorrectionCounter = 3;
        private float gameTickCounter = 0;

        private bool isDead = false;

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

        public void Initialize(Rigidbody rb, CharacterStateStorage stateStorage)
        {
            this.rb = rb;
            this.stateStorage = stateStorage;

            this.stateStorage.OnDeath += HandleDeath;
            this.stateStorage.OnRespawn += HandleRespawn;

            moveSpeed = config.PlayerSpeed > 0 ? config.PlayerSpeed : moveSpeed;
            gameTickEvent.OnRaised += GameTick;
            isInitialized = true;
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

        public void OnMove(Vector3 direction)
        {
            if (!stateStorage.IsGrounded || isDead)
                return;

            this.direction = direction.normalized;
            stateStorage.SetDirection(this.direction * moveSpeed);
        }

        public void GameTick(float dt)
        {
            if (!isInitialized || stateStorage.IsMovementBlocked || isDead)
                return;

            Vector3 nextPosition = rb.position + dt * moveSpeed * direction;
            rb.MovePosition(nextPosition);
            if (gameTickCounter % positionCorrectionCounter == 0)
                stateStorage.CorrectPosition(rb.position);

            gameTickCounter++;
        }

        public void LoadPosition(Vector3 position)
        {
            rb.position = position;
            stateStorage.CorrectPosition(rb.position);
            Debug.Log($"Loaded position for player {stateStorage.CharacterId}: {position}");
        }
    }
}
