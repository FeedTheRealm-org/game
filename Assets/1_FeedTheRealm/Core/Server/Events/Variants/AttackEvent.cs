using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Events;

public class AttackEvent : BaseServerEvent
{
    public AttackEvent(uint netId)
        : base(netId, false) { }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO
        {
            Type = Common.Enums.ServerEventType.AttackEvent,
            content = null,
        };
    }
}
