using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    private Logging.Logger logger;

    private Vector2 currentInputDirection;
    private Vector3 currentRealDirection;

    private NetworkAdapter networkAdapter;

    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;
    }

    public void SetDirection(Vector2 direction)
    {
        if (!isInitialized || direction == currentInputDirection)
            return;

        currentInputDirection = direction;
        logger.Log($"Direction Changed to: {direction}", this);

        UpdateCurrentDirectionWithCamera();

        ActionCommandDTO command = new()
        {
            Type = ActionType.Move,
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
