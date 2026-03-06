using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Events;

public class HitEvent : BaseServerEvent
{
    public HitEvent(uint netId)
        : base(netId, false) { }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO { Type = Common.Enums.ServerEventType.HitEvent, content = null };
    }
}
