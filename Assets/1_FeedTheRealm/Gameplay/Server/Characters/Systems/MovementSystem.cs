using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
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
        private uint netId;

        [SerializeField]
        private ServerConfig config;
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
                {
                    speedBuffAmount = 0f;
                }
            }

            float currentSpeed = moveSpeed + speedBuffAmount;
            Vector3 nextPosition = rb.position + dt * currentSpeed * direction;
            rb.MovePosition(nextPosition);
            if (gameTickCounter % positionCorrectionCounter == 0)
                stateStorage.CorrectPosition(rb.position);

            gameTickCounter++;
        }

        public void LoadPosition(Vector3 position)
        {
            rb.position = position;
            stateStorage.CorrectPosition(rb.position);
        }
    }
}
