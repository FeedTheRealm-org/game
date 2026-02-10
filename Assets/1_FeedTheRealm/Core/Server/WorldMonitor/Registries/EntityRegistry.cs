using System.Collections.Generic;
using FTR.Core.Server.Entities;

public sealed class EntityRegistry
{
    private readonly Dictionary<uint, ServerEntity> entities = new();

    public ServerEntity Get(uint netId) => entities[netId];

    public void Register(uint netId, ServerEntity entity) => entities[netId] = entity;
}
