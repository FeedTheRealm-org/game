using System;
using System.Linq;

namespace Game.Core.Utils
{
    public static class GetParams
    {
        public static string GetArgs(string key, string defaultValue = "")
        {
            var args = Environment.GetCommandLineArgs();
            var arg = args.FirstOrDefault(a => a.StartsWith($"--{key}="));

            return arg != null ? arg.Split('=', 2)[1] : defaultValue;
        }
    }
}
