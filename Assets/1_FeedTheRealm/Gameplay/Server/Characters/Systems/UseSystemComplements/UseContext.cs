using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Registry;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements
{
    /// <summary>
    /// Snapshot of everything a use-strategy needs to execute.
    /// Built once per OnUse call and passed down — strategies never hold
    /// a reference to UseSystem.
    /// </summary>
    public sealed class UseContext
    {
        public readonly uint NetId;
        public readonly Vector3 HitPoint;
        public readonly LayerMask TargetLayer;
        public readonly ServerConfig Config;
        public readonly EnemySlayedEvent EnemySlayedEvent;
        public readonly ConsumeItemEvent ConsumeItemEvent;
        public readonly PlayerHealEvent PlayerHealEvent;
        public readonly PlayerBuffSpeedEvent PlayerBuffSpeedEvent;
        public readonly WorldMonitor World;
        public readonly Logging.Logger Logger;
        public readonly object LogSource;

        public UseContext(
            uint netId,
            Vector3 hitPoint,
            LayerMask targetLayer,
            ServerConfig config,
            EnemySlayedEvent enemySlayedEvent,
            ConsumeItemEvent consumeItemEvent,
            PlayerHealEvent playerHealEvent,
            PlayerBuffSpeedEvent playerBuffSpeedEvent,
            WorldMonitor world,
            Logging.Logger logger,
            object logSource
        )
        {
            NetId = netId;
            HitPoint = hitPoint;
            TargetLayer = targetLayer;
            Config = config;
            EnemySlayedEvent = enemySlayedEvent;
            ConsumeItemEvent = consumeItemEvent;
            PlayerHealEvent = playerHealEvent;
            PlayerBuffSpeedEvent = playerBuffSpeedEvent;
            World = world;
            Logger = logger;
            LogSource = logSource;
        }
    }
}
