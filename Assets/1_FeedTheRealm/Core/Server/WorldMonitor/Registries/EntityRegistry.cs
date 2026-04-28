using System.Collections.Generic;
using FTR.Core.Server.Entities;

public sealed class EntityRegistry
{
    private readonly Dictionary<uint, ServerEntity> entities = new();

    public int Count => entities.Count;
    public int PlayerCount { get; private set; }

    public bool TryGet(uint netId, out ServerEntity entity)
    {
        return entities.TryGetValue(netId, out entity);
    }

    public void Register(uint netId, ServerEntity entity)
    {
        entities[netId] = entity;
        if (entity.IsPlayer)
            PlayerCount++;
    }

    public void Unregister(uint netId)
    {
        if (entities.Remove(netId, out var entity) && entity.IsPlayer)
            PlayerCount--;
    }
}
