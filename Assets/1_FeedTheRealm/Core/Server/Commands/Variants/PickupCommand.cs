using System;
using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

/// <summary>
///  PickUpCommand is a ServerOnly Command that represents a
///  command sent by the server to enqueue a pickup action for a player.
/// </summary>
public class PickUpCommand : BaseServerCommand
{
    private string itemId;

    private Action<bool> onComplete;

    public PickUpCommand(uint netId, string itemId, Action<bool> onComplete)
        : base(netId)
    {
        this.itemId = itemId;
        this.onComplete = onComplete;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnPickUp(eventCollector, itemId, onComplete);
    }
}
