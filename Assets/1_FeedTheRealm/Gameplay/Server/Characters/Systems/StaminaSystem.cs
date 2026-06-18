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
    public class StaminaSystem : MonoBehaviour
    {
        [Inject]
        private ServerConfig config;

        private bool isInitialized = false;
        private CharacterStateStorage stateStorage;
        private Coroutine staminaRecoveryCoroutine;

        private void OnDisable()
        {
            if (stateStorage != null)
            {
                stateStorage.OnStaminaConsume -= ConsumeStamina;
                stateStorage.OnStaminaRecoveryStop -= StopRecovery;
            }
            if (staminaRecoveryCoroutine != null)
            {
                StopCoroutine(staminaRecoveryCoroutine);
                staminaRecoveryCoroutine = null;
            }
        }

        public void Initialize(CharacterStateStorage stateStorage)
        {
            this.stateStorage = stateStorage;
            isInitialized = true;
            stateStorage.SetStamina(config.MaxStamina);
            stateStorage.OnStaminaConsume += ConsumeStamina;
            stateStorage.OnStaminaRecoveryStop += StopRecovery;
        }

        private void ConsumeStamina(float consumeAmount)
        {
            if (!isInitialized)
                return;
            stateStorage.SetStamina(Mathf.Max(stateStorage.Stamina - consumeAmount, 0));
            if (staminaRecoveryCoroutine == null)
                staminaRecoveryCoroutine = StartCoroutine(RecoverStaminaCoroutine());
        }

        private void StopRecovery()
        {
            if (!isInitialized)
                return;
            if (staminaRecoveryCoroutine != null)
                StopCoroutine(staminaRecoveryCoroutine);
            staminaRecoveryCoroutine = StartCoroutine(RecoverStaminaCoroutine());
        }

        private IEnumerator RecoverStaminaCoroutine()
        {
            yield return new WaitForSeconds(config.StaminaRecoveryDelay);
            while (stateStorage.Stamina < config.MaxStamina)
            {
                yield return new WaitForSeconds(config.StaminaRecoveryRate);

                float newStamina = Mathf.Min(
                    stateStorage.Stamina + config.StaminaRecoveryAmount,
                    config.MaxStamina
                );
                stateStorage.SetStamina(newStamina);
            }

            staminaRecoveryCoroutine = null;
        }
    }
}
