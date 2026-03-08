using UnityEngine;

namespace FTR.Core.Client
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Client/Prefab Provider")]
    public class ClientPrefabProvider : ScriptableObject
    {
        public GameObject ClientPlayerComponents;
        public GameObject hudComponent;
    }
}
