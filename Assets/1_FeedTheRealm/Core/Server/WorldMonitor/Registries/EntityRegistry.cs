using System.Collections.Generic;
using Game.Core.Server.Entities;
using UnityEngine;

public sealed class EntityRegistry
{
    private readonly Dictionary<uint, ServerEntity> entities = new();

    public ServerEntity Get(uint netId) => entities[netId];

    public void Register(uint netId, ServerEntity entity) => entities[netId] = entity;
}
