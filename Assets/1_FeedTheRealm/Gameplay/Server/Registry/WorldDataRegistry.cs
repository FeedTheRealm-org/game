using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Registry
{
    /// <summary>
    ///  This is meant for components (like portals) who require knowledge of the current world and zone,
    ///  to execute actions depending on the current info on the world
    /// </summary>
    public static class WorldDataRegistry
    {
        private static string worldId;
        private static int zoneId;

        public static string WorldId
        {
            get => worldId;
            set => worldId = value;
        }

        public static int ZoneId
        {
            get => zoneId;
            set => zoneId = value;
        }
    }
}
