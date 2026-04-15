namespace FTR.Core.Common.Enums;

/// <summary>
/// ServerResponseType represents different types of responses that the server can send back to the client.
/// </summary>
public enum ServerEventType
{
    AttackEvent,
    HitEvent,
    DashEvent,
    InitialForceEvent,
    InteractFailedEvent,
    DialogEvent,
    OpenShopEvent,
    NotEnoughGoldEvent,
    InteractCompletedEvent,
    QuestProgressEvent,
    QuestCompletedEvent,
    ChatMessageBroadcastEvent,
}
