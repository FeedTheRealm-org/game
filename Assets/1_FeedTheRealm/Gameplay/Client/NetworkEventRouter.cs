using System;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class NetworkEventRouter : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    public event Action<AttackEventContent> OnAttackEvent;
    public event Action<DashEventContent> OnDashEvent;
    public event Action<InitialForceEventContent> OnLootItemSpawnEvent;
    public event Action<DialogEventContent> OnDialogEvent;
    public event Action OnInteractFailedEvent;

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
                AttackEventContent attackEvent = AttackEventContent.Parser.ParseFrom(
                    serverEvent.content
                );
                OnAttackEvent?.Invoke(attackEvent);
                logger.Log($"Routed AttackEvent", this);
                break;
            case ServerEventType.DashEvent:
                DashEventContent dashEvent = DashEventContent.Parser.ParseFrom(serverEvent.content);
                OnDashEvent?.Invoke(dashEvent);
                logger.Log($"Routed DashEvent", this);
                break;
            case ServerEventType.InitialForceEvent:
                InitialForceEventContent lootItemSpawnEvent =
                    InitialForceEventContent.Parser.ParseFrom(serverEvent.content);
                OnLootItemSpawnEvent?.Invoke(lootItemSpawnEvent);
                logger.Log($"Routed LootItemSpawnEvent", this);
                break;
            case ServerEventType.DialogEvent:
                DialogEventContent dialogEvent = DialogEventContent.Parser.ParseFrom(
                    serverEvent.content
                );
                OnDialogEvent?.Invoke(dialogEvent);
                logger.Log($"Routed DialogEvent", this);
                break;
            case ServerEventType.InteractFailedEvent:
                OnInteractFailedEvent?.Invoke();
                logger.Log($"Routed InteractFailedEvent", this);
                break;
            default:
                logger.Log($"Received unhandled server event type: {serverEvent.Type}", this);
                break;
        }
    }
}
