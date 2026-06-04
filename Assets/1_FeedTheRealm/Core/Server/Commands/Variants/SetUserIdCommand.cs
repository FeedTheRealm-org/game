using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class SetUserIdCommand : BaseServerCommand
{
    private string tokenId;
    private bool isTeleporting;

    public SetUserIdCommand(uint netId, string tokenId, bool isTeleporting)
        : base(netId)
    {
        this.tokenId = tokenId;
        this.isTeleporting = isTeleporting;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnSetUserId(eventCollector, tokenId, isTeleporting);
    }
}
