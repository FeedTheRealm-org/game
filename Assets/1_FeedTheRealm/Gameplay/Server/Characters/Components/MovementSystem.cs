using FTR.Core.Common.Utils;
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
        private bool isInitialized = false;
        private Rigidbody rb;
        private CharacterStateStorage stateStorage;
        private Vector3 direction = Vector3.zero;
        private float moveSpeed = 5f;
        private float positionCorrectionCounter = 15;
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

            // Subscribe after injection has occurred
            gameTickEvent.OnRaised += GameTick;

            Debug.Log(
                "MovementSystem initialized | rb: "
                    + (rb != null)
                    + " | stateStorage: "
                    + (stateStorage != null)
            );
        }

        public void OnMove(Vector3 direction)
        {
            Debug.Log($"Received move command with direction: {direction}");
            this.direction = direction;
        }

        public void GameTick(float dt)
        {
            if (!isInitialized)
                return;

            Vector3 nextPosition = rb.position + dt * moveSpeed * direction;
            Vector3 newVelocity = (nextPosition - rb.position) / dt;

            rb.MovePosition(nextPosition);
            if (gameTickCounter % positionCorrectionCounter == 0)
                stateStorage.CorrectPosition(rb.position);
            stateStorage.SetVelocity(newVelocity);

            gameTickCounter++;
        }
    }
}
