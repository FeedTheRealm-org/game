using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters
{
    public class MovementSystem : MonoBehaviour, IGameTickable
    {
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

        private void OnDisable()
        {
            if (gameTickEvent != null)
                gameTickEvent.OnRaised -= GameTick;
        }

        public void Initialize(Rigidbody rb, CharacterStateStorage stateStorage)
        {
            this.rb = rb;
            this.stateStorage = stateStorage;
            isInitialized = true;
            moveSpeed = config.PlayerSpeed > 0 ? config.PlayerSpeed : moveSpeed;
            gameTickEvent.OnRaised += GameTick;
        }

        public void OnMove(Vector3 direction)
        {
            this.direction = direction;
            stateStorage.SetDirection(direction * moveSpeed);
        }

        public void GameTick(float dt)
        {
            if (!isInitialized)
                return;

            Vector3 nextPosition = rb.position + dt * moveSpeed * direction;
            rb.MovePosition(nextPosition);
            if (gameTickCounter % positionCorrectionCounter == 0)
                stateStorage.CorrectPosition(rb.position);

            gameTickCounter++;
        }
    }
}
