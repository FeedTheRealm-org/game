using System;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class NetworkEventRouter : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    // List of subscribable ServerEvents
    public event Action<ServerEventDTO> OnAttackEvent;
    public event Action<ServerEventDTO> OnHitEvent;

    private NetworkAdapter networkAdapter;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        this.networkAdapter = networkAdapter;
        networkAdapter.OnServerEvent += RouteEvent;
    }

    private void OnDestroy()
    {
        if (networkAdapter != null)
            networkAdapter.OnServerEvent -= RouteEvent;
    }

    private void RouteEvent(ServerEventDTO serverEvent)
    {
        switch (serverEvent.Type)
        {
            case ServerEventType.AttackEvent:
                OnAttackEvent?.Invoke(serverEvent);
                logger.Log($"Routed AttackEvent", this);
                break;
            case ServerEventType.HitEvent:
                OnHitEvent?.Invoke(serverEvent);
                logger.Log($"Routed HitEvent", this);
                break;
            default:
                logger.Log($"Received unhandled server event type: {serverEvent.Type}", this);
                break;
        }
    }
}
