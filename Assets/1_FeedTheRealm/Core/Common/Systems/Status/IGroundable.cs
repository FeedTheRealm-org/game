namespace FTR.Core.Common.Systems.Status
{
    /// <summary>
    /// Payload for HealthChangedEvent.
    /// Only raised for the local player's character; no NetId needed.
    /// </summary>
    public interface IGroundable
    {
        bool IsGrounded { get; set; }
    }
}
