using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player dashing movement based on the stamina.
/// </summary>
public class DashComponent : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private GroundCheckComponent groundCheck;

    [SerializeField]
    private MovementComponent movement;

    [SerializeField]
    private StaminaComponent stamina;

    [SerializeField]
    private float dashSpeed = 50f;

    [SerializeField]
    private float dashDuration = 0.1f;

    [SerializeField]
    private float staminaConsumption = 20f;

    private bool isDashing;
    private NetworkMovementSynchronizer networkMovementSync;

    public bool IsDashing => isDashing;

    public System.Action OnDashFinished;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (groundCheck == null)
            groundCheck = GetComponent<GroundCheckComponent>();
        if (movement == null)
            movement = GetComponent<MovementComponent>();

        // Try to find NetworkMovementSynchronizer for multiplayer support
        networkMovementSync = GetComponent<NetworkMovementSynchronizer>();
    }

    /// <summary>
    /// Called by the input system to initiate a dash.
    /// </summary>
    public void OnDash()
    {
        if (isDashing || !groundCheck.IsGrounded || !consumeStamina())
        {
            OnDashFinished?.Invoke();
            return;
        }

        Vector3 dashDirection = movement.CurrentDirection.normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        StartCoroutine(dashRoutine(dashDirection));
    }

    /* --- UTIL METHODS --- */

    /// <summary>
    /// Coroutine to handle the dash force aplication for the defined duration.
    /// </summary>
    private IEnumerator dashRoutine(Vector3 direction)
    {
        isDashing = true;

        // Notify NetworkMovementSynchronizer to prevent velocity sync during dash
        if (networkMovementSync != null)
        {
            networkMovementSync.NotifyDashStart(dashDuration);
        }

        // Disable MovementComponent to prevent it from overriding the dash velocity
        bool wasMovementEnabled = movement.enabled;
        movement.enabled = false;

        // apply instant burst
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * dashSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);

        // stop dash instantly for "snappy" feel
        rb.linearVelocity = Vector3.zero;
        isDashing = false;

        // Re-enable MovementComponent
        movement.enabled = wasMovementEnabled;

        OnDashFinished?.Invoke();
    }

    private bool consumeStamina()
    {
        return stamina.TryConsumeStamina(staminaConsumption);
    }
}
