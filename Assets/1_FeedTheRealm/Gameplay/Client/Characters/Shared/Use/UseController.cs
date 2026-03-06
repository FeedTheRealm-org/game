using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class UseController : MonoBehaviour
{
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

        ActionCommandDTO command = new() { Type = ActionType.Use };

        networkAdapter.DispatchAction(command);
    }
}
