using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Stamina", menuName = "Scriptable Objects/Stamina")]
public class Stamina : ScriptableObject
{
    [Header("Stamina Settings")]
    public float MaxStamina = 100f;
    public float RecoverAmount = 5f;
    public float RecoverRate = 1f;

    public float CurrentStamina { get; private set; }

    public event Action<float> OnStaminaChanged;

    private void OnEnable()
    {
        CurrentStamina = MaxStamina;
    }

    /// <summary>
    /// Sets the current stamina and invokes the OnStaminaChanged event.
    /// </summary>
    public void SetStamina(float value)
    {
        CurrentStamina = Mathf.Clamp(value, 0, MaxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina); // Notify subscribers
    }
}
