using Game.Core.Domain.Movement;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField]
    private MovementView movementView;

    [SerializeField]
    private MovementNetworkAdapter movementNetworkAdapter;

    [SerializeField]
    private float moveSpeed = 5f;

    [Header("Debug")]
    [SerializeField]
    private Logging.Logger logger;

    private readonly MovementPrediction prediction = new();

    private Vector3 currentDirection;

    private uint sequenceNumber = 0;

    private void OnEnable()
    {
        movementNetworkAdapter.OnMovementReconcileSnapshot += OnReconcile;
    }

    private void OnDisable()
    {
        movementNetworkAdapter.OnMovementReconcileSnapshot -= OnReconcile;
    }

    // TODO: refactor camerai
    public void SetDirection(Vector2 direction)
    {
        var cameraTransform = Camera.main?.transform;

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

        currentDirection = (camRight * direction.x + camForward * direction.y).normalized;
        logger.Log($"SetDirection: {currentDirection}", this);
    }

    private void FixedUpdate()
    {
        sequenceNumber++;
        logger.Log($"FixedUpdate: Seq {sequenceNumber}, Dir {currentDirection}", this);

        float deltaTime = Time.fixedDeltaTime;

        MovementCommand command = new MovementCommand
        {
            sequenceNumber = sequenceNumber,
            x = currentDirection.x,
            y = currentDirection.y,
            z = currentDirection.z,
        };

        prediction.Store(command);
        movementNetworkAdapter.Tick(command);

        Vector3 nextPosition = MovementRules.CalculateNextPosition(
            movementView.transform.position,
            currentDirection,
            moveSpeed,
            deltaTime
        );

        movementView.MoveToPosition(nextPosition);
    }

    private void OnReconcile(MovementSnapshot snapshot)
    {
        prediction.Reconcile(transform, snapshot, moveSpeed, Time.fixedDeltaTime);
    }
}
