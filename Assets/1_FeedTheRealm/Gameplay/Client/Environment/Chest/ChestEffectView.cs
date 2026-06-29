using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.Chest;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Environment.Chest
{
    public class ChestEffectView : MonoBehaviour
    {
        [Inject]
        private ChestOpenedEvent chestOpenedEvent;

        [Inject]
        private ClientPrefabProvider prefabProvider;

        private GameObject chestEffectInstance;

        public async UniTask Initialize()
        {
            chestEffectInstance = Instantiate(prefabProvider.ChestOpenEffectPrefab, transform);
            chestEffectInstance.transform.localPosition = new Vector3(0, 1.5f, 0);
            chestEffectInstance.transform.localScale = new Vector3(3f, 3f, 3f);
            chestEffectInstance.SetActive(false);

            chestOpenedEvent.OnRaised += HandleChestOpened;
        }

        private void OnDestroy()
        {
            chestOpenedEvent.OnRaised -= HandleChestOpened;
        }

        private void HandleChestOpened()
        {
            chestEffectInstance.SetActive(true);
            chestEffectInstance.GetComponent<ParticleSystem>().Play();
        }
    }
}
