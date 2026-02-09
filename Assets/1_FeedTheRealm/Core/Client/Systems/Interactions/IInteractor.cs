using UnityEngine;

namespace Game.Core.Client.Interactions
{
    /// <summary>
    /// Interface for interactor entities in the game.
    /// </summary>
    public interface IInteractor
    {
        /// <summary>
        /// The GameObject associated with the interactor.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// The Transform component of the interactor.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Event triggered when the interactor has finished interacting.
        /// </summary>
        void FinishInteracting();
    }
}
