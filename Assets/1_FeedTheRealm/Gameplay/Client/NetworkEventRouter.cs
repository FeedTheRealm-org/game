using System;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class NetworkEventRouter : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    // List of subscribable ServerEvents
    public event Action<AttackEventContent> OnAttackEvent;
    public event Action OnHitEvent;

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
                AttackEventContent attackEvent = AttackEventContent.Parser.ParseFrom(data);
                OnAttackEvent?.Invoke(attackEvent);
                logger.Log($"Routed AttackEvent", this);
                break;
            case ServerEventType.HitEvent:
                OnHitEvent?.Invoke();
                logger.Log($"Routed HitEvent", this);
                break;
            default:
                logger.Log($"Received unhandled server event type: {serverEvent.Type}", this);
                break;
        }
    }
}
