using FTR.Core.Common.Utils;
using UnityEngine;

namespace FTR.Core.Client.Config
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Config/ClientConfig")]
    public class ClientConfig : ScriptableObject
    {
        [Header("Health View")]
        public float HealthUpdateDelay = 0.3f;
        public float MaxHealth = 100f;
    }
}
