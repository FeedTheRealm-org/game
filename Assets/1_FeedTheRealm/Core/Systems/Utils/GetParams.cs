using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Utils
{
    public static class GetParams
    {
        public static string GetEnvVars(string variableName, string defaultValue = "")
        {
#if !UNITY_EDITOR
            return Environment.GetEnvironmentVariable(variableName) ?? defaultValue;
#else
            return defaultValue;
#endif
        }
    }
}
