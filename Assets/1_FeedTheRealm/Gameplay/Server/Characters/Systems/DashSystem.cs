using System.Numerics;
using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
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

        private bool isDashing;

        // TODO: Stamina
        // private float currentStamina;
        // private float staminaRecoveryTimer;

        public void Initialize(Rigidbody rb)
        {
            this.rb = rb;
            isInitialized = true;
        }

        public void OnDash(Vector3 direction)
        {
            // TODO: Can dash again if stamina allows (Stamina system), when on ground (Ground check system)
            if (isDashing || !isInitialized)
            {
                return;
            }

            Vector3 force = direction * config.DashSpeed;
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
        }

        private IEnumerator DashRoutine(Vector3 force)
        {
            isDashing = true;
            ApplyDashing(force); // apply instant burst
            yield return new WaitForSeconds(config.DashDuration);
            StopDash(); // stop dash instantly for "snappy" feel
        }

        public void GameTick(float dt) { }
    }
}
