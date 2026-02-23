using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField]
    private MovementView movementView;

    [SerializeField]
    private float moveSpeed = 5f; // TODO: move to config SO

    [Header("Debug")]
    [SerializeField]
    private Logging.Logger logger;

    private Vector2 inputDirection;
    private Vector3 currentDirection;

    private uint sequenceNumber = 0;

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
        this.inputDirection = direction;
        logger.Log($"SetDirection: {direction}", this);
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
            return;

        float deltaTime = Time.fixedDeltaTime;

        UpdateCurrentDirectionWithCamera();

        ActionCommandDTO command = new ActionCommandDTO
        {
            Type = ActionType.Move,
            Direction = currentDirection,
        };

        movementView.UpdateFacingDirection(currentDirection);
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
