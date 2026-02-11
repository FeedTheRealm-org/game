using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class AcceptQuestCommand : BaseServerCommand
{
    private string questId;

    public AcceptQuestCommand(uint netId, string questId)
        : base(netId)
    {
        this.questId = questId;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnQuestAccepted();
    }
}
