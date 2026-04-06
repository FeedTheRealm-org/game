using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

/// <summary>
/// Sent by the client when the player moves out of interaction range.
/// The server closes the dialog and resets all interaction state.
/// </summary>
public class CancelInteractCommand : BaseServerCommand
{
    public CancelInteractCommand(uint netId)
        : base(netId) { }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnCancelInteract(eventCollector);
    }
}
