using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Core.Interfaces;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.UI;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public class WorldUISetupService : ISetup
    {
        private readonly GameObject settingsMenu;
        private readonly IObjectResolver objectResolver;

        private OnWorldLeaveEvent onExitEvent;

        public WorldUISetupService(
            ClientPrefabProvider clientPrefabProvider,
            OnWorldLeaveEvent onExitEvent,
            IObjectResolver objectResolver
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
            this.objectResolver = objectResolver;
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

            var instantiatedMenu = objectResolver.Instantiate(settingsMenu);
            instantiatedMenu.name = "SettingsMenu";
            instantiatedMenu.SetActive(false);
        }

        private void DisconnectPlayer()
        {
            Debug.Log("Disconnecting player from server...");
            NetworkManager.singleton.StopClient();
        }
    }
}
