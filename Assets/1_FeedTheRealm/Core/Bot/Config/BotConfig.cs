using System;
using FTR.Core.Common.Utils;
using UnityEngine;
using VContainer;

namespace FTR.Core.Bot.Config
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Config/BotConfig")]
    public class BotConfig : ScriptableObject
    {
        [SerializeField]
        private Logging.Logger logger;

        [Header("Params/Args Settings")]
        // These come from <exec> --world-id=X --zone-id=Y
        [HideInInspector]
        public string BotId;

        [HideInInspector]
        public string WorldId;

        [HideInInspector]
        public int ZoneId;

        [HideInInspector]
        public string ServerFixedToken;

        public float MoveIntervalSeconds = 0.2f;
        public float ActionIntervalSeconds = 1.1f;
        public float InteractIntervalSeconds = 3.5f;
        public float DirectionChangeIntervalSeconds = 1.8f;

        public void LoadParams()
        {
            try
            {
                this.BotId = ParamsSerializer.GetArgs("bot-id", "1");
                this.WorldId = ParamsSerializer.GetArgs("world-id", string.Empty);
                this.ZoneId = int.Parse(ParamsSerializer.GetArgs("zone-id", "0"));
                this.ServerFixedToken = Environment.GetEnvironmentVariable("SERVER_FIXED_TOKEN");

                logger.Log(
                    $"[BotConfig] Loaded bot parameters: botId={BotId}, worldId={WorldId}, zoneId={ZoneId}"
                );
            }
            catch (System.Exception ex)
            {
                logger.Log(
                    $"[BotConfig] Failed to load bot parameters: {ex.Message}",
                    Logging.LogType.Error
                );
            }
        }
    }
}
