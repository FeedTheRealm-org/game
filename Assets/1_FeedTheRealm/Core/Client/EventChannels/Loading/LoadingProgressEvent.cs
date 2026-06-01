using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FeedTheRealm.Core.EventChannels.Setup
{
    [CreateAssetMenu(menuName = "Events/Client/LoadingProgressEvent")]
    public class LoadingProgressEvent : EventChannelSO<float> { }
}
