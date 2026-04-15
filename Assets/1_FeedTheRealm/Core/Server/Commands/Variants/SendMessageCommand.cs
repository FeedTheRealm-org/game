using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class SendMessageCommand : BaseServerCommand
{
    private string id;
    private SendMessageCommandContent content;

    public SendMessageCommand(uint netId, string id, SendMessageCommandContent content)
        : base(netId)
    {
        this.id = id;
        this.content = content;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnSendMessage(eventCollector, content.Message);
    }
}
