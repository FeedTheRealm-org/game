using System.Collections;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class DashSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerConfig config;

        private bool isInitialized = false;

        private Rigidbody rb;

        private uint netId;

        private bool isDashing;
        private CharacterStateStorage stateStorage;

        private float staminaRecoveryTimer = 0f;

        private void OnDisable()
        {
            if (gameTickEvent != null)
                gameTickEvent.OnRaised -= GameTick;
        }

        public void Initialize(uint netId, Rigidbody rb, CharacterStateStorage stateStorage)
        {
            this.rb = rb;
            this.netId = netId;
            this.stateStorage = stateStorage;
            stateStorage.SetStamina(config.MaxStamina);
            isInitialized = true;
            gameTickEvent.OnRaised += GameTick;
        }

        public void OnDash(IEventCollectable ec, Vector3 direction)
        {
            if (isDashing || !isInitialized)
                return;

            if (stateStorage.Stamina < config.DashStaminaCost)
                return;

            Vector3 force = direction.normalized * config.DashSpeed;
            stateStorage.SetStamina(stateStorage.Stamina - config.DashStaminaCost);
            stateStorage.IsMovementBlocked = true;
            StartCoroutine(DashRoutine(force));

            ec.Collect(
                new DashEvent(
                    netId,
                    new DashEventContent
                    {
                        Force = new Force
                        {
                            X = force.x,
                            Y = force.y,
                            Z = force.z,
                        },
                        Duration = config.DashDuration,
                    }
                )
            );
        }

        private void ApplyDashing(Vector3 force)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(force, ForceMode.VelocityChange);
        }

        private void StopDash()
        {
            rb.linearVelocity = Vector3.zero;
            isDashing = false;
            stateStorage.IsMovementBlocked = false;
        }

        private IEnumerator DashRoutine(Vector3 force)
        {
            isDashing = true;
            ApplyDashing(force); // apply instant burst
            yield return new WaitForSeconds(config.DashDuration);
            StopDash(); // stop dash instantly for "snappy" feel
        }

        public void GameTick(float dt)
        {
            if (!isInitialized || isDashing)
                return;

            if (stateStorage.Stamina >= config.MaxStamina)
                return;

            staminaRecoveryTimer += dt;
            if (staminaRecoveryTimer >= config.StaminaRecoveryRate)
            {
                staminaRecoveryTimer = 0f;
                float newStamina = Mathf.Min(stateStorage.Stamina + config.StaminaRecoveryAmount, config.MaxStamina);
                stateStorage.SetStamina(newStamina);
            }
        }
    }
}
