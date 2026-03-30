using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands
{
    /// <summary>
    /// Tells the server to advance (or close) the current dialog sequence for this character.
    /// </summary>
    public class DialogNextCommand : BaseServerCommand
    {
        public DialogNextCommand(uint netId)
            : base(netId) { }

        public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
        {
            commandable.OnDialogNext(eventCollector);
        }
    }
}
