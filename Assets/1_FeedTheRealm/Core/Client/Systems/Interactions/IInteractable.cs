namespace Game.Core.Client.Interactions
{
    /// <summary>
    /// Interface for interactable objects in the game.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Method to handle interaction with the object.
        /// Returns a string containing the ID of the interacted object.
        /// </summary>
        string Interact(IInteractor interactor);

        /// <summary>
        /// Method to check if the object can be interacted with.
        /// </summary>
        bool CanInteract(IInteractor interactor);
    }
}
