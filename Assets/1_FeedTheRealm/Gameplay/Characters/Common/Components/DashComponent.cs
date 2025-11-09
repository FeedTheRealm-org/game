using UnityEngine;
using System.Collections;

/// <summary>
/// Handles player dashing movement based on the stamina.
/// </summary>
public class DashComponent : MonoBehaviour {
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GroundCheckComponent groundCheck;
    [SerializeField] private MovementComponent movement;
    [SerializeField] private StaminaComponent stamina;
    [SerializeField] private SpriteLoader spriteLoader;

    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float staminaConsumption = 20f;

    private string hairTextureName = "Hair_1";
    private bool isDashing;

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (groundCheck == null) groundCheck = GetComponent<GroundCheckComponent>();
        if (movement == null) movement = GetComponent<MovementComponent>();
    }

    /// <summary>
    /// Called by the input system to initiate a dash.
    /// </summary>
    public void OnDash() {
        if (hairTextureName == "Hair_1") {
            hairTextureName = "Hair_2";
            spriteLoader.ChangeHair(hairTextureName);
        } else {
            hairTextureName = "Hair_1";
            spriteLoader.ChangeHair(hairTextureName);
        }
        if (isDashing || !groundCheck.IsGrounded || !consumeStamina()) return;

        Vector3 dashDirection = movement.CurrentDirection.normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        StartCoroutine(dashRoutine(dashDirection));
    }

    /* --- UTIL METHODS --- */

    /// <summary>
    /// Coroutine to handle the dash force aplication for the defined duration.
    /// </summary>
    private IEnumerator dashRoutine(Vector3 direction) {
        isDashing = true;

        // apply instant burst
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * dashSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);

        // stop dash instantly for "snappy" feel
        rb.linearVelocity = Vector3.zero;
        isDashing = false;
    }

    private bool consumeStamina() {
        return stamina.TryConsumeStamina(staminaConsumption);
    }
}

