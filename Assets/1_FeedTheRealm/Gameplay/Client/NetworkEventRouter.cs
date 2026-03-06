using System;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class NetworkEventRouter : MonoBehaviour
{
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
                break;
            case ServerEventType.HitEvent:
                OnHitEvent?.Invoke(serverEvent);
                break;
            default:
                Debug.LogWarning($"Received unhandled server event type: {serverEvent.Type}");
                break;
        }
    }
}
