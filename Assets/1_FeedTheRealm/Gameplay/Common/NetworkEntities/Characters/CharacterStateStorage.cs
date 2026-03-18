using System;
using FTR.Core.Common.Characters;
using FTR.Core.Common.Systems.Status;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterStateStorage : NetworkBehaviour, ICharacterHealthSource, IGroundable
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        [SyncVar(hook = nameof(OnDirectionSync))]
        private Vector3 direction;

        [SyncVar(hook = nameof(OnStaminaSync))]
        private float stamina;

        [SyncVar(hook = nameof(OnHealthSync))]
        private float health;

        [SyncVar]
        private int _currentDialogIndex;

        [SyncVar(hook = nameof(OnCurrentNpcIdSync))]
        private string _currentNpcId;

        [SyncVar(hook = nameof(OnIsInteractingSync))]
        private bool _isInteracting;

        /* --- Getters --- */

        public Vector3 Position => position;
        public Vector3 Direction => direction;
        public float Stamina => stamina;
        public float Health => health;
        public bool IsLocalPlayer => isLocalPlayer;
        public bool IsGrounded { get; set; }
        public bool IsMovementBlocked { get; set; }
        public bool IsInteracting => _isInteracting;
        public string CurrentNpcId => _currentNpcId;
        public int CurrentDialogIndex => _currentDialogIndex;

        /* --- Events --- */

        public event Action<Vector3> OnPositionCorrected;
        public event Action<Vector3> OnDirectionChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<float> OnHealthChanged;
        public event Action<bool> OnIsInteractingChanged;
        public event Action<string> OnCurrentNpcIdChanged;

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

        [Server]
        public void SetInteracting(bool value, string npcId = "")
        {
            _currentNpcId = npcId;
            if (!value)
                _currentDialogIndex = 0;
            _isInteracting = value;
        }

        [Server]
        public void SetDialogIndex(int index) => _currentDialogIndex = index;

        [Server]
        public void SwitchInteractingNpc(string newNpcId)
        {
            _currentNpcId = newNpcId;
            _currentDialogIndex = 0;
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
        }

        private void OnIsInteractingSync(bool _, bool v)
        {
            OnIsInteractingChanged?.Invoke(v);
        }

        private void OnCurrentNpcIdSync(string oldId, string newId)
        {
            if (_isInteracting && !string.IsNullOrEmpty(newId))
                OnCurrentNpcIdChanged?.Invoke(newId);
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
            OnIsInteractingSync(false, _isInteracting);
        }
    }
}
