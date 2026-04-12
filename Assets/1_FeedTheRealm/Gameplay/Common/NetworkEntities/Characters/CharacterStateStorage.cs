using System;
using FTR.Core.Common.Characters;
using FTR.Core.Common.Systems.Status;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterStateStorage
        : NetworkBehaviour,
            ICharacterHealthSource,
            IGroundable,
            ICharacterIdentity,
            ICharacterIdSource
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        [SyncVar(hook = nameof(OnDirectionSync))]
        private Vector3 direction;

        [SyncVar(hook = nameof(OnStaminaSync))]
        private float stamina;

        [SyncVar(hook = nameof(OnHealthSync))]
        private float health;

        [SyncVar(hook = nameof(OnCharacterIdSync))]
        private string characterId = "";

        [SyncVar(hook = nameof(OnCharacterNameSync))]
        private string characterName;

        /* --- Getters --- */

        public Vector3 Position => position;
        public Vector3 Direction => direction;
        public float Stamina => stamina;
        public float Health => health;
        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public bool IsLocalPlayer => isLocalPlayer;
        public bool IsGrounded { get; set; }
        public bool IsMovementBlocked { get; set; }

        /* --- Events --- */

        public event Action<Vector3> OnPositionCorrected;
        public event Action<Vector3> OnDirectionChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<float> OnHealthChanged;
        public event Action<string> OnCharacterIdChanged;
        public event Action<string> OnCharacterNameChanged;
        public event Action OnDeath;
        public event Action OnRespawn;

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
            float oldHealth = health;
            health = newHealth;
            RaiseHealthStatusChanged(oldHealth, newHealth);
        }

        [Server]
        public void SetCharacterId(string newCharacterId)
        {
            characterId = newCharacterId;
        }

        [Server]
        public void SetCharacterName(string newCharacterName)
        {
            characterName = newCharacterName;
        }

        /* --- SyncVar hooks --- */

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
            RaiseHealthStatusChanged(oldHealth, newHealth);
        }

        private void OnCharacterIdSync(string oldId, string newId)
        {
            characterId = newId;
            OnCharacterIdChanged?.Invoke(newId);
        }

        private void OnCharacterNameSync(string oldName, string newName)
        {
            characterName = newName;
            OnCharacterNameChanged?.Invoke(newName);
        }

        /* --- Event Raisers --- */

        private void RaiseHealthStatusChanged(float oldHealth, float newHealth)
        {
            if (oldHealth > 0 && newHealth <= 0)
                OnDeath?.Invoke();
            else if (oldHealth <= 0 && newHealth > 0)
                OnRespawn?.Invoke();
        }

        public override void OnStartClient()
        {
            Debug.Log(
                $"[CharacterStateStorage] Initial sync: position={position}, direction={direction}, stamina={stamina}, health={health}, characterId={characterId}, characterName={characterName}",
                this
            );
            OnPositionSync(Vector3.zero, position);
            OnDirectionSync(Vector3.zero, direction);
            OnStaminaSync(0, stamina);
            OnHealthSync(0, health);
            OnCharacterIdSync(null, characterId);
            OnCharacterNameSync(null, characterName);
        }
    }
}
