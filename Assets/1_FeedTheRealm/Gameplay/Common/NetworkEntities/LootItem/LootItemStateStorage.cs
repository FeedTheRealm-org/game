using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public class LootItemStateStorage : NetworkBehaviour
    {
        // [SyncVar(hook = nameof(OnFinalPositionSync))]
        // private Vector3 finalPosition;

        // [SyncVar(hook = nameof(OnInitialForceSync))]
        // private Vector3 initialPosition;

        // [SyncVar(hook = nameof(OnInitialForceSync))]
        // private Vector3 initialForce;

        // /* --- Getters --- */

        // public Vector3 Position => finalPosition;
        // public Vector3 InitialForce => initialForce;
        // public bool IsGrounded { get; set; }

        // public event Action<Vector3> OnPositionCorrected;
        // public event Action<Vector3> OnDirectionChanged;

        // /* --- Setters --- */

        // [Server]
        // public void SetDirection(Vector3 newDirection)
        // {
        //     direction = newDirection;
        // }

        // [Server]
        // public void CorrectInitialPosition(Vector3 newPosition)
        // {
        //     position = newPosition;
        // }

        // [Server]
        // public void CorrectPosition(Vector3 newPosition)
        // {
        //     position = newPosition;
        // }

        // [Server]
        // public void SetStamina(float newStamina)
        // {
        //     stamina = newStamina;
        // }

        // [Server]
        // public void SetHealth(float newHealth)
        // {
        //     health = newHealth;
        // }

        // /* --- Syncvar hooks --- */

        // private void OnFinalPositionSync(Vector3 oldPosition, Vector3 newPosition)
        // {
        //     OnPositionCorrected?.Invoke(newPosition);
        // }

        // private void OnInitialPositionSync(Vector3 oldPosition, Vector3 newPosition)
        // {
        //     OnPositionCorrected?.Invoke(newPosition);
        // }

        // private void OnInitialForceSync(Vector3 oldDirection, Vector3 newDirection)
        // {
        //     OnDirectionChanged?.Invoke(newDirection);
        // }

        // private void OnStaminaSync(float oldStamina, float newStamina)
        // {
        //     OnStaminaChanged?.Invoke(newStamina);
        // }

        // private void OnHealthSync(float oldHealth, float newHealth)
        // {
        //     OnHealthChanged?.Invoke(newHealth);
        // }

        // public override void OnStartClient()
        // {
        //     Debug.Log(
        //         $"[CharacterStateStorage] Initial sync: position={position}, direction={direction}, stamina={stamina}, health={health}",
        //         this
        //     );
        //     OnFinalPositionSync(Vector3.zero, position);
        //     OnInitialPositionSync(Vector3.zero, initialPosition);
        //     OnInitialForceSync(Vector3.zero, initialForce);

        // }
    }
}
