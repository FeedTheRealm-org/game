using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class SetUserIdCommand : BaseServerCommand
{
    private string tokenId;

    public SetUserIdCommand(uint netId, string tokenId)
        : base(netId)
    {
        this.tokenId = tokenId;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnSetUserId(eventCollector, tokenId);
    }
}
