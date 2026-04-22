using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(menuName = "Events/Client/Inventory/LastAdded")]
    public class LastAddedEvent : EventChannelSO<(StorageType, string, int, int)> { }
}
