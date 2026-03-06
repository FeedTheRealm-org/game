using System.Collections.Generic;
using FTR.Core.Server.Entities;

public sealed class EntityRegistry
{
    private readonly Dictionary<uint, ServerEntity> entities = new();

    public bool TryGet(uint netId, out ServerEntity entity)
    {
        return entities.TryGetValue(netId, out entity);
    }

    public void Register(uint netId, ServerEntity entity) => entities[netId] = entity;
}
