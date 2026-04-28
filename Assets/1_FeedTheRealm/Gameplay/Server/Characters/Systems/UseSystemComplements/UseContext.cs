using System;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
using FTR.Gameplay.Server.Utils.UseEquipment;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements
{
    /// <summary>
    /// Built once in UseSystem.Initialize and reused for every Execute call.
    /// </summary>
    public sealed class UseContext
    {
        public readonly uint NetId;
        private readonly Func<Vector3> _hitPointProvider;
        private readonly Func<LayerMask> _targetLayerProvider;
        public Vector3 HitPoint => _hitPointProvider();
        public LayerMask TargetLayer => _targetLayerProvider();
        public readonly ServerConfig Config;
        public readonly MovementSystem Movement;
        public readonly HealthSystem Health;
        public readonly InventorySystem Inventory;
        public readonly StatModifierBag StatMods;
        public readonly EnemySlayedEvent EnemySlayedEvent;
        public readonly WorldMonitor World;
        public readonly Logging.Logger Logger;
        public readonly object LogSource;

        public UseContext(
            uint netId,
            Func<Vector3> hitPointProvider,
            Func<LayerMask> targetLayerProvider,
            ServerConfig config,
            MovementSystem movement,
            HealthSystem health,
            InventorySystem inventory,
            StatModifierBag statMods,
            EnemySlayedEvent enemySlayedEvent,
            WorldMonitor world,
            Logging.Logger logger,
            object logSource
        )
        {
            NetId = netId;
            _hitPointProvider = hitPointProvider;
            _targetLayerProvider = targetLayerProvider;
            Config = config;
            Movement = movement;
            Health = health;
            Inventory = inventory;
            StatMods = statMods;
            EnemySlayedEvent = enemySlayedEvent;
            World = world;
            Logger = logger;
            LogSource = logSource;
        }
    }
}
