namespace FTR.Core.Common.Interactions
{
    /// <summary>
    /// Optional interface for interactables that can be blocked mid-interaction
    /// waiting for an external decision (e.g. a quest offer the player must accept or reject).
    /// </summary>
    public interface IQuestBlockable
    {
        void OnQuestDecided(uint playerNetId);
    }
}
