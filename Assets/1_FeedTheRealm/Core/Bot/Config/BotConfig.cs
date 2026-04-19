using FTR.Core.Common.Utils;
using UnityEngine;
using VContainer;

namespace FTR.Core.Bot.Config
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Config/BotConfig")]
    public class BotConfig : ScriptableObject
    {
        [Inject]
        private Logging.Logger logger;

        public string BotId;
        public string WorldId;
        public string ZoneId;

        public float MoveIntervalSeconds = 0.2f;
        public float ActionIntervalSeconds = 1.1f;
        public float InteractIntervalSeconds = 3.5f;
        public float DirectionChangeIntervalSeconds = 1.8f;

        public void LoadParams()
        {
            BotId = ParamsSerializer.GetArgs("bot-id", "1");
            WorldId = ParamsSerializer.GetArgs("world-id", string.Empty);
            ZoneId = ParamsSerializer.GetArgs("zone-id", string.Empty);

            logger.Log(
                $"[BotConfig] Loaded bot parameters: botId={BotId}, worldId={WorldId}, zoneId={ZoneId}"
            );
        }
    }
}
