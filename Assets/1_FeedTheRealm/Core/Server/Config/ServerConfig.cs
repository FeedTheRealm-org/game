using FTR.Core.Common.Utils;
using UnityEngine;

namespace FTR.Core.Server.Config
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Config/ServerConfig")]
    public class ServerConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField]
        private float playerSpeed = 5f;
        public float PlayerSpeed => playerSpeed;

        [Header("Items")]
        [SerializeField]
        private uint itemDespawnTime = 120; // this is in seconds

        [SerializeField]
        private ushort maxInitialForce = 5; // default max force applied to the item when spawned
        public uint ItemDespawnTime => itemDespawnTime;
        public ushort MaxInitialForce => maxInitialForce;

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

        [Header("Ground Check")]
        [SerializeField]
        private float groundCheckDistance = 1f;
        public float GroundCheckDistance => groundCheckDistance;

        [SerializeField]
        private float groundCheckSphereRadius = 0.4f;
        public float GroundCheckSphereRadius => groundCheckSphereRadius;

        [SerializeField]
        private LayerMask groundLayer;
        public LayerMask GroundLayer => groundLayer;

        [Header("Inventory")]
        [SerializeField]
        private int fastSlotSize = 5;
        public int FastSlotSize => fastSlotSize;

        [SerializeField]
        private int inventorySize = 20;
        public int InventorySize => inventorySize;

        [Header("NPC AI")]
        public float WanderRadius = 10f;
        public float MinWaitTime = 2f;
        public float MaxWaitTime = 5f;
        public float StoppingDistance = 0.5f;
        public float AggressiveChaseRadius = 5f;
        public float AggressiveAttackRadius = 1f;
    }
}
