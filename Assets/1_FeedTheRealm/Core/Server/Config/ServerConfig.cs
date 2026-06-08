using System;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Utils;
using UnityEngine;

namespace FTR.Core.Server.Config
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Config/ServerConfig")]
    public class ServerConfig : ScriptableObject
    {
        [Header("Environment Variables Settings")]
        public string EnvFilePath = ".env";

        // LoadFromEnvFile: Load variables from the .env file (e.g. Prod: false, Dev: false).
        public bool LoadFromEnvFile = false;

        // LoadEnvVars: Load system environment variables (e.g. Prod: true, Dev: false).
        public bool LoadEnvVars = true;

        [Header("Params/Args Settings")]
        // These come from <exec> --world-id=X --zone-id=Y
        [HideInInspector]
        public string WorldId;

        [HideInInspector]
        public int ZoneId;

        [HideInInspector]
        public bool IsTestWorld;

        public int SendPlayerCountIntervalSeconds = 120;

        [Header("Database Settings")]
        // PersistToDatabase: Save variables into the database (e.g. Prod: true, Dev: false).
        public bool PersistToDatabase = true;
        public int AutoSaveIntervalSeconds = 30;

        [Header("Layer Masks")]
        public LayerMask PlayerLayer;
        public LayerMask TargetLayer; // Enemies
        public LayerMask GroundLayer;
        public LayerMask ObstacleLayer;
        public LayerMask SlopeLayer;
        public LayerMask InteractableLayer;

        [Header("Movement")]
        [SerializeField]
        private float playerSpeed = 5f;
        public float PlayerSpeed => playerSpeed;
        public float MovementRaycastAngle = 30f;

        [Header("Items")]
        [SerializeField]
        private float itemDespawnTime = 120f; // 2 minutes

        [SerializeField]
        private ushort maxInitialForce = 5; // default max force applied to the item when spawned

        [SerializeField]
        private float groundCheckDelay = 0.5f;
        public float ItemDespawnTime => itemDespawnTime;
        public ushort MaxInitialForce => maxInitialForce;
        public float GroundCheckDelay => groundCheckDelay;

        [Header("Dash")]
        [SerializeField]
        private float dashSpeed = 25f;
        public float DashSpeed => dashSpeed;

        [SerializeField]
        private float dashDuration = 0.1f;
        public float DashDuration => dashDuration;

        [Header("Stamina")]
        [SerializeField]
        private float maxStamina = 100f;
        public float MaxStamina => maxStamina;

        [SerializeField]
        private float dashStaminaCost = 25f;
        public float DashStaminaCost => dashStaminaCost;

        [SerializeField]
        private float staminaRecoveryRate = 1f;
        public float StaminaRecoveryRate => staminaRecoveryRate;

        [SerializeField]
        private float staminaRecoveryAmount = 5f;
        public float StaminaRecoveryAmount => staminaRecoveryAmount;

        [Header("Combat")]
        [SerializeField]
        private float attackCooldown = 0.4f;
        public float AttackCooldown => attackCooldown;

        [SerializeField]
        private int unequippedDamage = 10;
        public int UnequippedDamage => unequippedDamage;

        [SerializeField]
        private float unequippedRange = 1.5f;
        public float UnequippedRange => unequippedRange;

        [Header("Ground Check")]
        [SerializeField]
        private float groundCheckDistance = 1f;

        [SerializeField]
        private float minGroundNormalAlignment = 0.5f;
        public float GroundCheckDistance => groundCheckDistance;
        public float MinGroundNormalAlignment => minGroundNormalAlignment;

        [SerializeField]
        private float groundCheckSphereRadius = 0.4f;
        public float GroundCheckSphereRadius => groundCheckSphereRadius;

        [Header("Entity Reaper")]
        [SerializeField]
        private float reapIntervalSeconds = 60f; // 1 minute
        public float ReapIntervalSeconds => reapIntervalSeconds;

        [Header("Inventory")]
        [SerializeField]
        private int fastSlotSize = 5;
        public int FastSlotSize => fastSlotSize;

        [SerializeField]
        private int inventorySize = 20;
        public int InventorySize => inventorySize;

        [Header("Use System")]
        [SerializeField]
        private float useRange = 2f;
        public float UseRange => useRange;

        [Header("Gold")]
        [SerializeField]
        private int startingGold = 100;
        public int StartingGold => startingGold;

        [Header("NPC AI")]
        public float WanderRadius = 10f;
        public float MinWaitTime = 2f;
        public float MaxWaitTime = 5f;
        public float StoppingDistance = 0.5f;
        public float AggressiveChaseRadius = 5f;
        public float AggressiveAttackRadius = 2f;
        public float AutoAttackDelay = 0.3f;
        public float RangedWeaponRaySpacing = 1f;
        public float MaxNavigationTime = 10f;

        [Header("Chest")]
        public float ChestItemSpawnRateSeconds = 0.5f;
        public float ChestItemSpawnHeight = 1f;

        public void LoadParams()
        {
            this.WorldId = ParamsSerializer.GetArgs("world-id", string.Empty);
            this.ZoneId = int.Parse(ParamsSerializer.GetArgs("zone-id", "0"));
            this.IsTestWorld = bool.Parse(ParamsSerializer.GetArgs("is-test-world", "false"));

#if UNITY_EDITOR
            // In case its running from the editor this is useful
            if (!LoadFromEnvFile)
                return;
            EnvironmentVariablesUtils.LoadFromEnvFile(this.EnvFilePath);
            if (string.IsNullOrEmpty(this.WorldId))
                this.WorldId = Environment.GetEnvironmentVariable("WORLD_ID");
            if (this.ZoneId == 0)
                this.ZoneId = int.Parse(Environment.GetEnvironmentVariable("ZONE_ID") ?? "0");
            if (!this.IsTestWorld)
                this.IsTestWorld = bool.Parse(
                    Environment.GetEnvironmentVariable("IS_TEST_WORLD") ?? "false"
                );
#endif
        }
    }
}
