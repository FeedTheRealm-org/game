using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.EventChannels;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Client.Environment.Items
{
    /// <summary>
    /// ItemObject represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class ItemObject : MonoBehaviour, IGameTickable
    {
        private string id;

        [SerializeField]
        private string itemName;

        [SerializeField]
        private string description;

        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private uint despawnTime = 3600; // game rate is 30 ticks per second, so this is 2 minutes

        private NetworkAdapter networkAdapter;

        public string ItemName => itemName;
        public string Description => description;
        public string Id => id;

        private void OnEnable()
        {
            networkAdapter = GetComponent<NetworkAdapter>();
            id = GetComponent<NetworkIdentity>().netId.ToString();
            gameTickEvent.OnRaised += GameTick;
        }

        private void OnDisable()
        {
            gameTickEvent.OnRaised -= GameTick;
        }

        public void GameTick(float dt)
        {
            if (despawnTime <= 0)
                Destroy(gameObject);
            else
                despawnTime--;
        }

        private void OnTriggerEnter(Collider other)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int otherLayer = other.gameObject.layer;

            if (otherLayer == playerLayer)
            {
                string playerId = other.gameObject.GetComponent<NetworkIdentity>().netId.ToString();
                SendPickupCommand(playerId);
            }
        }

        private void AfterPickup()
        {
            Destroy(gameObject);
        }

        private void SendPickupCommand(string playerId)
        {
            TransactionCommandDTO command = new() { Type = TransactionType.PickUp, Id = playerId };
            networkAdapter.DispatchTransaction(command);
            Debug.Log($"HELLO I GOT PICKED UP!!! {id} by player {playerId}");
        }
    }
}
