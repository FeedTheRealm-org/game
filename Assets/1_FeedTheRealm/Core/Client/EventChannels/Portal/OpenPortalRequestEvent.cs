using System;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Portal
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Open Portal UI Event")]
    public class OpenPortalUIEvent : EventChannelSO<OpenPortalUiContent> { }

    public class OpenPortalUiContent
    {
        public Action OnAccept;
        public Action OnReject;
        public string PortalName;
        public string DestinationName;
    }
}
