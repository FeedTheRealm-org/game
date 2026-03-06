namespace FTR.Core.Server.States;

public abstract class EntityState
{
    // True if the state has been modified since the last snapshot.
    public bool Dirty { get; set; }
}
