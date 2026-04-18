using System;
using System.Globalization;
using FTR.Core.Server.Utils;

namespace FTR.Core.Bot.Config
{
    public class BotRuntimeConfig
    {
        public string BotId { get; }
        public string WorldId { get; }
        public string ZoneId { get; }
        public string JoinToken { get; }
        public string ServerAddress { get; }
        public ushort ServerPort { get; }

        public float MoveIntervalSeconds { get; }
        public float ActionIntervalSeconds { get; }
        public float InteractIntervalSeconds { get; }
        public float DirectionChangeIntervalSeconds { get; }

        public BotRuntimeConfig()
        {
            BotId = ParamsSerializer.GetArgs("bot-id", Guid.NewGuid().ToString("N"));
            WorldId = ParamsSerializer.GetArgs("world-id", string.Empty);
            ZoneId = ParamsSerializer.GetArgs("zone-id", string.Empty);
            JoinToken = ParamsSerializer.GetArgs("join-token", string.Empty);
            ServerAddress = ParamsSerializer.GetArgs("server-address", string.Empty);
            ServerPort = ParseUShortArg("server-port", 0);

            MoveIntervalSeconds = ParseFloatArg("bot-move-interval", 0.2f);
            ActionIntervalSeconds = ParseFloatArg("bot-action-interval", 1.1f);
            InteractIntervalSeconds = ParseFloatArg("bot-interact-interval", 3.5f);
            DirectionChangeIntervalSeconds = ParseFloatArg("bot-direction-interval", 1.8f);
        }

        private static ushort ParseUShortArg(string key, ushort fallback)
        {
            var rawValue = ParamsSerializer.GetArgs(key, string.Empty);
            return ushort.TryParse(rawValue, out var parsedValue) ? parsedValue : fallback;
        }

        private static float ParseFloatArg(string key, float fallback)
        {
            var rawValue = ParamsSerializer.GetArgs(key, string.Empty);
            return float.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedValue
            )
                ? parsedValue
                : fallback;
        }
    }
}
