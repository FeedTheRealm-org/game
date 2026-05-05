using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class UseController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    private NetworkAdapter networkAdapter;

    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;
    }

    public void Use()
    {
        if (!isInitialized)
            return;

        ActionCommandDTO command = new() { Type = ActionType.Use, Direction = GetUseDirection() };

        networkAdapter.DispatchAction(command);
    }

    private Vector3 GetUseDirection()
    {
        var cameraTransform = Camera.main != null ? Camera.main.transform : null;
        var sourceForward = cameraTransform != null ? cameraTransform.forward : transform.forward;

        Vector3 direction = new Vector3(sourceForward.x, 0f, sourceForward.z);

        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
    }
}
