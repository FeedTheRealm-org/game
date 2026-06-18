using System;
using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Chest;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTRShared.Runtime.Core.Cache;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Environment.Chest
{
    public class ChestView : MonoBehaviour
    {
        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private WorldSelector worldSelector;

        [Inject]
        private ISoundPlayer soundPlayer;

        [Inject]
        private CacheManager cacheManager;

        [Inject]
        private ChestOpenedEvent chestOpenedEvent;

        private static readonly Dictionary<string, GameObject> modelCache = new();

        private GameObject openChestVisual;
        private GameObject closedChestVisual;
        private ChestStateStorage chestStateStorage;

        public async UniTask Initialize(ChestStateStorage chestStateStorage)
        {
            this.chestStateStorage = chestStateStorage;

            Dictionary<string, ModelInfo> modelsInfo = await modelService.ListWorldModels(
                worldSelector.GetSelectedWorldId()
            );

            var chestData = chestStateStorage.ChestData;
            string openModelUrl = modelsInfo[chestData.opendedChestModelData.modelId].url;
            string openModelUpdatedAt = modelsInfo[
                chestData.opendedChestModelData.modelId
            ].updated_at;
            string closedModelUrl = modelsInfo[chestData.closedChestModelData.modelId].url;
            string closedModelUpdatedAt = modelsInfo[
                chestData.closedChestModelData.modelId
            ].updated_at;

            GameObject openVisual = await GetModel(openModelUrl, openModelUpdatedAt);
            GameObject closedVisual = await GetModel(closedModelUrl, closedModelUpdatedAt);

            SetupMesh(openVisual, closedVisual);
            ToggleChestState(chestStateStorage.IsOpen);

            chestStateStorage.OnChestStateChanged += ToggleChestState;

            modelsInfo.Clear();
            ClearModelCache();
        }

        private void ToggleChestState(bool isOpen)
        {
            if (openChestVisual == null || closedChestVisual == null)
                return;
            openChestVisual.SetActive(isOpen);
            closedChestVisual.SetActive(!isOpen);
            if (isOpen)
            {
                chestOpenedEvent?.Raise();
                soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.ChestOpen, transform.position);
            }
            else
                soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.ChestClose, transform.position);
        }

        private void SetupMesh(GameObject openChestVisual, GameObject closedChestVisual)
        {
            this.openChestVisual = SetupChestVisuals(
                openChestVisual,
                chestStateStorage.ChestData.opendedChestModelData
            );
            this.closedChestVisual = SetupChestVisuals(
                closedChestVisual,
                chestStateStorage.ChestData.closedChestModelData
            );
            this.openChestVisual.SetActive(false);
            this.closedChestVisual.SetActive(true);
        }

        private GameObject SetupChestVisuals(GameObject visual, ChestModelData chestModelData)
        {
            var visualInstance = Instantiate(visual, transform);
            visualInstance.SetActive(true);
            visualInstance.transform.localPosition = chestModelData.relativePosition;
            visualInstance.transform.localRotation = Quaternion.Euler(
                chestModelData.relativeRotation
            );
            visualInstance.transform.localScale = chestModelData.relativeSize;
            return visualInstance;
        }

        private async UniTask<GameObject> GetModel(string modelUrl, string updatedAt)
        {
            if (modelCache.ContainsKey(modelUrl))
                return Instantiate(modelCache[modelUrl]);

            GameObject visual = null;
            try
            {
                var timeStamp = DateTimeHelper.ParseDateTimeOffset(updatedAt);
                visual = await cacheManager.GetModel(modelUrl, timeStamp);
            }
            catch
            {
                Debug.LogError($"Failed to load model: {modelUrl}.");
            }

            visual.SetActive(false);
            modelCache[modelUrl] = visual;
            return Instantiate(visual);
        }

        public static void ClearModelCache()
        {
            foreach (var model in modelCache.Values)
                if (model != null)
                    Destroy(model);
            modelCache.Clear();
        }

        void OnDestroy()
        {
            if (chestStateStorage != null)
                chestStateStorage.OnChestStateChanged -= ToggleChestState;
        }
    }
}
