using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Core.Interfaces;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Client.Interfaces;
using FTR.Core.Common.Config;
using FTR.Core.Common.Enums;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public class WorldUISetupService : ISetup
    {
        private readonly GameObject settingsMenu;
        private readonly GameObject questMenu;
        private readonly GameObject confirmPopupPrefab;
        private readonly IObjectResolver objectResolver;
        private readonly ConfirmPopupHandle confirmPopupHandle;
        private readonly Config config;

        private OnWorldLeaveEvent onExitEvent;

        public WorldUISetupService(
            ClientPrefabProvider clientPrefabProvider,
            OnWorldLeaveEvent onExitEvent,
            IObjectResolver objectResolver,
            Config config,
            ConfirmPopupHandle confirmPopupHandle
        )
        {
            if (clientPrefabProvider == null)
            {
                Debug.LogError("ClientPrefabProvider not set!");
                return;
            }
            this.onExitEvent = onExitEvent;
            this.onExitEvent.OnRaised += DisconnectPlayer;
            settingsMenu = clientPrefabProvider.SettingMenuComponent;
            questMenu = clientPrefabProvider.QuestsMenuPrefab;
            confirmPopupPrefab = clientPrefabProvider.ConfirmPopup;
            this.objectResolver = objectResolver;
            this.confirmPopupHandle = confirmPopupHandle;
            this.config = config;
        }

        public void Dispose()
        {
            onExitEvent.OnRaised -= DisconnectPlayer;
        }

        public void Setup()
        {
            if (settingsMenu == null)
                throw new System.Exception(
                    "SettingsMenu GameObject not set in WorldUIObjectProvider!"
                );
            if (questMenu == null)
                throw new System.Exception(
                    "QuestMenu GameObject not set in WorldUIObjectProvider!"
                );
            if (confirmPopupPrefab == null)
                throw new System.Exception("ConfirmPopup prefab not set in ClientPrefabProvider!");

            if (objectResolver == null)
                throw new System.Exception("IObjectResolver not provided to WorldUISetupService!");

            if (confirmPopupHandle == null)
                throw new System.Exception(
                    "ConfirmPopupHandle not provided to WorldUISetupService!"
                );

            var instantiatedMenu = objectResolver.Instantiate(settingsMenu);
            instantiatedMenu.name = "SettingsMenu";

            var instantiatedQuestMenu = objectResolver.Instantiate(questMenu);
            instantiatedQuestMenu.name = "QuestMenu";

            var instantiatedPopup = objectResolver.Instantiate(confirmPopupPrefab);
            instantiatedPopup.name = "ConfirmPopup";

            confirmPopupHandle.Controller = instantiatedPopup.GetComponent<IConfirmPopup>();
        }

        private void DisconnectPlayer()
        {
            Debug.Log("Disconnecting player from server...");
            config.DisconnectionEvent = DisconnectionEvents.ReturnHome;
            NetworkManager.singleton.StopClient();
        }
    }
}
