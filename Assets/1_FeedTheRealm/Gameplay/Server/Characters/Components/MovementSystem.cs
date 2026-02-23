using System.Numerics;
using FTR.Core.Common.Loaders;
using FTR.Core.Common.Utils;
using FTR.Core.Server.EventChannels;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters
{
    public class MovementSystem : MonoBehaviour, IGameTickable
    {
        private Rigidbody rb;
        private Collider col;
        private Vector3 direction = Vector3.zero;

        [Inject]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private float moveSpeed = 5f;

        private float movingMagnitudeThreshold = 0.001f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            gameTickEvent.OnRaised += GameTick;
        }

        public void OnMove(Vector3 direction)
        {
            this.direction = direction;
        }

        public void GameTick(float dt)
        {
            Vector3 nextPosition = rb.position + dt * moveSpeed * direction;
            rb.MovePosition(nextPosition);
            // SEND TO STATE STORAGE AND THEN TO CLIENTS
        }
    }
}
