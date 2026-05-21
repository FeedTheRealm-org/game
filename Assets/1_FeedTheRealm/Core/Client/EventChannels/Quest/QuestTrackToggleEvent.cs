using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Quest
{
    public struct QuestTrackData
    {
        public string QuestId;
        public bool IsTracked;
    }

    [CreateAssetMenu(
        fileName = "QuestTrackToggleEvent",
        menuName = "Events/Client/UI/Quest Track Toggle"
    )]
    public class QuestTrackToggleEvent : EventChannelSO<QuestTrackData> { }
}
