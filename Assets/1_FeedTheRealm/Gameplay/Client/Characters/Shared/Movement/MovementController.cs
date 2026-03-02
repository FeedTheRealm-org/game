using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField]
    private MovementView movementView;

    [Header("Debug")]
    [SerializeField]
    private Logging.Logger logger;

    private Vector2 inputDirection;
    private Vector3 currentDirection;

    private NetworkAdapter networkAdapter;

    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;
    }

    // TODO: refactor camera make a camera manager!
    public void SetDirection(Vector2 direction)
    {
        if (!isInitialized)
            return;

        inputDirection = direction;
        logger.Log($"SetDirection: {direction}", this);

        UpdateCurrentDirectionWithCamera();

        ActionCommandDTO command = new() { Type = ActionType.Move, Direction = currentDirection };

        // TODO: dont do this here, let the server tell us which direction to face based on the authoritative state
        // movementView.UpdateFacingDirection(currentDirection);
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

        currentDirection = (camRight * inputDirection.x + camForward * inputDirection.y).normalized;
    }
}
