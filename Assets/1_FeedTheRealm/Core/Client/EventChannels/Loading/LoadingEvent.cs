using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FeedTheRealm.Core.EventChannels.Setup
{
    [CreateAssetMenu(menuName = "Events/Client/LoadingEvent")]
    public class LoadingEvent : EventChannelSO<bool> { }
}
