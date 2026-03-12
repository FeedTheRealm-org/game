namespace FTR.Core.Common.Systems.Status
{
    /// <summary>
    /// Represents an entity that can be grounded or in an airborne state.
    /// </summary>
    public interface IGroundable
    {
        /// <summary>
        /// Gets or sets a value indicating whether the entity is currently grounded.
        /// </summary>
        bool IsGrounded { get; set; }
    }
}
