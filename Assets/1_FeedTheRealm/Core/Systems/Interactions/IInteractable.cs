namespace Game.Core.Interactions
{
    /// <summary>
    /// Interface for interactable objects in the game.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Method to handle interaction with the object.
        /// </summary>
        void Interact(IInteractor interactor);

        /// <summary>
        /// Method to check if the object can be interacted with.
        /// </summary>
        bool CanInteract(IInteractor interactor);
    }
}
