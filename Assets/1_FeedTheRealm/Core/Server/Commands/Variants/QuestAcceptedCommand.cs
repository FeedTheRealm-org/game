using UnityEngine;

namespace FTR.Core.Server.Commands;

public class QuestAcceptedCommand : BaseServerCommand
{
    private Vector3 direction;

    public QuestAcceptedCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnQuestAccepted();
    }
}
