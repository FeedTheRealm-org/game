namespace FTR.Core.Server.Commands;

public class AcceptQuestCommand : BaseServerCommand
{
    private string questId;

    public AcceptQuestCommand(string questId)
    {
        this.questId = questId;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnQuestAccepted();
    }
}
