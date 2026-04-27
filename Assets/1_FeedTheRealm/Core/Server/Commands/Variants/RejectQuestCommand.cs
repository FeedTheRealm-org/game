using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

/// <summary>
/// Sent by the client when the player rejects a quest.
/// The server only needs to resume the inactivity timer — no quest state changes.
/// </summary>
public class RejectQuestCommand : BaseServerCommand
{
    public RejectQuestCommand(uint netId)
        : base(netId) { }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnQuestDecided(eventCollector);
    }
}
