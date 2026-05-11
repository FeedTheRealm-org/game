using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using UnityEngine;
using VContainer;

public class MovementController : MonoBehaviour
{
    [Inject]
    private FixedTickEvent fixedTickEvent;

    [Header("Debug")]
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private float rotationMagnitudeToSend = 25f;

    private Vector2 currentInputDirection;
    private Vector3 currentRealDirection;
    private float currentRotation;
    private float previousRotationSent;

    private NetworkAdapter networkAdapter;
    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;
        fixedTickEvent.OnRaised += FixedTick;
    }

    private void OnDestroy()
    {
        fixedTickEvent.OnRaised -= FixedTick;
    }

    private void FixedTick()
    {
        if (!isInitialized)
            return;

        currentRotation = Camera.main.transform.eulerAngles.y;
        var delta = Mathf.Abs(Mathf.DeltaAngle(currentRotation, previousRotationSent));

        if (delta >= rotationMagnitudeToSend)
            SendMoveCommand();
    }

    /// <summary>
    /// SetDirection called when an input event is raised (non continous).
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        if (!isInitialized)
            return;

        currentInputDirection = direction;
        SendMoveCommand();
    }

    /// <summary>
    /// SetDirection called when an input event is raised (non continous).
    /// </summary>
    public void OnDash()
    {
        if (!isInitialized)
            return;

        SendDashCommand();
    }

    private void SendMoveCommand()
    {
        if (!isInitialized)
            return;

        UpdateCurrentDirectionWithCamera();

        ActionCommandDTO command = new()
        {
            Type = ActionType.Move,
            Direction = currentRealDirection,
        };

        networkAdapter.DispatchAction(command);
        previousRotationSent = currentRotation;
    }

    /// <summary>
    /// Sends a dash command when a dash input event is raised (via OnDash).
    /// </summary>
    private void SendDashCommand()
    {
        if (!isInitialized)
            return;

        UpdateCurrentDirectionWithCamera();

        ActionCommandDTO command = new()
        {
            Type = ActionType.Dash,
            Direction = currentRealDirection,
        };

        networkAdapter.DispatchAction(command);
    }

    private void UpdateCurrentDirectionWithCamera()
    {
        var cameraTransform = Camera.main.transform;

        Vector3 camForward = new Vector3(
            cameraTransform.forward.x,
            0f,
            cameraTransform.forward.z
        ).normalized;

        Vector3 camRight = new Vector3(
            cameraTransform.right.x,
            0f,
            cameraTransform.right.z
        ).normalized;

        currentRealDirection = (
            camRight * currentInputDirection.x + camForward * currentInputDirection.y
        ).normalized;
    }
}
