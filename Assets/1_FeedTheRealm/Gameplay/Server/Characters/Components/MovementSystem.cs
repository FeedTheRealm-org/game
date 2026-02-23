using FTR.Core.Common.Utils;
using FTR.Core.Server.EventChannels;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters
{
    public class MovementSystem : MonoBehaviour, IGameTickable
    {
        private Rigidbody rb;
        private Vector3 direction = Vector3.zero;

        private bool isInitialized = false;

        [Inject]
        private GameTickEvent gameTickEvent;

        private float moveSpeed = 5f;

        private float positionCorrectionCounter = 15;
        private float gameTickCounter = 0;

        private void OnEnable()
        {
            gameTickEvent.OnRaised += GameTick;
        }

        private void OnDisable()
        {
            gameTickEvent.OnRaised -= GameTick;
        }

        public void Initialize(Rigidbody rb)
        {
            this.rb = rb;
            isInitialized = true;
        }

        public void OnMove(Vector3 direction)
        {
            this.direction = direction;
        }

        public void GameTick(float dt)
        {
            if (!isInitialized)
                return;
            Vector3 nextPosition = rb.position + dt * moveSpeed * direction;
            rb.MovePosition(nextPosition);

            if (gameTickCounter % positionCorrectionCounter == 0)
            {
                // SEND POSITION TO STATE STORAGE
            }

            // SEND DIRECTION STATE STORAGE AND THEN TO CLIENTS
        }
    }
}
