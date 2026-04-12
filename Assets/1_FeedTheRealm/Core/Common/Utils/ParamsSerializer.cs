using System;
using System.Linq;

namespace FTR.Core.Server.Utils
{
    public static class ParamsSerializer
    {
        public static string GetArgs(string key, string defaultValue = "")
        {
            var args = Environment.GetCommandLineArgs();
            var arg = args.FirstOrDefault(a => a.StartsWith($"--{key}=", StringComparison.Ordinal));

            if (arg != null)
                return arg.Split('=', 2)[1];

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], $"--{key}", StringComparison.Ordinal))
                    return args[i + 1];
            }

            return defaultValue;
        }
    }
}
