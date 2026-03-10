using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class HitEvent : BaseServerEvent
{
    private readonly float currentHealth;
    private readonly float maxHealth;

    public HitEvent(uint netId, float currentHealth, float maxHealth)
        : base(netId, false)
    {
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;
    }

    public override ServerEventDTO ToDTO()
    {
        var content = new HitEventContent
        {
            TargetNetId = NetId,
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth,
        };
        return new ServerEventDTO
        {
            Type = Common.Enums.ServerEventType.HitEvent,
            content = content.ToByteArray(),
        };
    }
}
