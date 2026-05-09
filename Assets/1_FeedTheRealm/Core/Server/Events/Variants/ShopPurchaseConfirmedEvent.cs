using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class ShopPurchaseConfirmEvent : BaseServerEvent
    {
        public ShopPurchaseConfirmEvent(uint netId, int targetConnectionId)
            : base(netId, targetConnectionId) { }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.ShopPurchaseConfirmEvent,
                content = ByteString.Empty.ToByteArray(),
            };
        }
    }
}
