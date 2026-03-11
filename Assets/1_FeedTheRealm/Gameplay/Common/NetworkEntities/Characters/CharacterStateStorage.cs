using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        [SyncVar(hook = nameof(OnDirectionSync))]
        private Vector3 direction;

        [SyncVar(hook = nameof(OnStaminaSync))]
        private float stamina;

        [SyncVar(hook = nameof(OnHealthSync))]
        private float health;

        /* --- Getters --- */

        public Vector3 Position => position;
        public Vector3 Direction => direction;
        public float Stamina => stamina;
        public float Health => health;
        public bool IsGrounded { get; set; }
        public bool IsMovementBlocked { get; set; }

        public event Action<Vector3> OnPositionCorrected;
        public event Action<Vector3> OnDirectionChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<float> OnHealthChanged;

        /* --- Setters --- */

        [Server]
        public void SetDirection(Vector3 newDirection)
        {
            direction = newDirection;
        }

        [Server]
        public void CorrectPosition(Vector3 newPosition)
        {
            position = newPosition;
        }

        [Server]
        public void SetStamina(float newStamina)
        {
            stamina = newStamina;
        }

        [Server]
        public void SetHealth(float newHealth)
        {
            health = newHealth;
        }

        /* --- Syncvar hooks --- */

        private void OnPositionSync(Vector3 oldPosition, Vector3 newPosition)
        {
            OnPositionCorrected?.Invoke(newPosition);
        }

        private void OnDirectionSync(Vector3 oldDirection, Vector3 newDirection)
        {
            OnDirectionChanged?.Invoke(newDirection);
        }

        private void OnStaminaSync(float oldStamina, float newStamina)
        {
            OnStaminaChanged?.Invoke(newStamina);
        }

        private void OnHealthSync(float oldHealth, float newHealth)
        {
            OnHealthChanged?.Invoke(newHealth);
        }

        public override void OnStartClient()
        {
            Debug.Log(
                $"[CharacterStateStorage] Initial sync: position={position}, direction={direction}, stamina={stamina}, health={health}",
                this
            );
            OnPositionSync(Vector3.zero, position);
            OnDirectionSync(Vector3.zero, direction);
            OnStaminaSync(0, stamina);
            OnHealthSync(0, health);
        }
    }
}
