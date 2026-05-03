using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(menuName = "Events/Client/Inventory/LastSwapped")]
    public class LastSwappedEvent
        : EventChannelSO<(StorageType, int, string, int, StorageType, int, string, int)> { }
}
