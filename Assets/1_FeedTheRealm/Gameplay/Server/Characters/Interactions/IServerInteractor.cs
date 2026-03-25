using FTR.Core.Common.Interactions;
using FTR.Core.Server.Events;

namespace FTR.Gameplay.Server.Characters.Interactions
{
    public interface IServerInteractor : IInteractor
    {
        uint NetId { get; }

        /// <summary>
        /// Gets the current event collector for this interaction frame, if available.
        /// </summary>
        IEventCollectable CurrentEventCollector { get; }
    }
}
