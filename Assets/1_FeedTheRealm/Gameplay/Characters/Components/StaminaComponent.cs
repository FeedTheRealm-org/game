using UnityEngine;
using System.Collections;

public class StaminaComponent : MonoBehaviour {
    [SerializeField]
    private Stamina staminaData;

    private Coroutine recoveryCoroutine;

    private void OnEnable() {
        recoveryCoroutine = StartCoroutine(staminaRecoveryRoutine());
    }

    private void OnDestroy() {
        if (recoveryCoroutine != null) {
            StopCoroutine(recoveryCoroutine);
        }
    }

    private void OnDisable() {
        if (recoveryCoroutine != null) {
            StopCoroutine(recoveryCoroutine);
        }
    }

    public bool TryConsumeStamina(float amount) {
        Debug.Log("Current stamina: " + staminaData.CurrentStamina + ", Consuming: " + amount);
        if (amount > staminaData.CurrentStamina) {
            return false;
        }
        staminaData.SetStamina(Mathf.Max(staminaData.CurrentStamina - amount, 0));
        return true;
    }

    /* --- UTIL METHODS --- */

    private void recoverStamina() {
        if (staminaData.CurrentStamina < staminaData.MaxStamina) {
            staminaData.SetStamina(Mathf.Min(staminaData.CurrentStamina + staminaData.RecoverAmount, staminaData.MaxStamina));
        }
    }

    private IEnumerator staminaRecoveryRoutine() {
        while (true) {
            recoverStamina();
            yield return new WaitForSeconds(staminaData.RecoverRate);
        }
    }

}
