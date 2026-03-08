using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Client.Environment.Items
{
    /// <summary>
    /// ItemObject represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class ItemObject : MonoBehaviour
    {
        private string id;

        [SerializeField]
        private string itemName;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private string description;

        private NetworkAdapter networkAdapter;

        public string ItemName => itemName;
        public string Description => description;
        public string Id => id;

        private void Start()
        {
            gameObject.SetActive(true);
            networkAdapter = GetComponent<NetworkAdapter>();
            id = GetComponent<NetworkIdentity>().netId.ToString();
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
