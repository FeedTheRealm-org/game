using UnityEngine;
using System.Collections;

public class StaminaComponent : MonoBehaviour {
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float recoverAmount = 5f;
    [SerializeField] private float recoverRate = 1f;

    private Coroutine recoveryCoroutine;

    public float CurrentStamina { get; private set; }

    private void Start() {
        CurrentStamina = maxStamina;
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
        Debug.Log("Current stamina: " + CurrentStamina + ", Consuming: " + amount);
        if (amount > CurrentStamina) {
            return false;
        }
        CurrentStamina = Mathf.Max(CurrentStamina - amount, 0);
        return true;
    }

    /* --- UTIL METHODS --- */

    private void recoverStamina() {
        if (CurrentStamina < maxStamina) {
            CurrentStamina = Mathf.Min(CurrentStamina + recoverAmount, maxStamina);
        }
    }

    private IEnumerator staminaRecoveryRoutine() {
        while (true) {
            recoverStamina();
            yield return new WaitForSeconds(recoverRate);
        }
    }

}
