using FeedTheRealm.Core.EventChannels.Setup;
using FTR.Core.Client;
using NUnit.Framework.Internal.Commands;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FeedTheRealm.Gameplay.Client.SceneSetup
{
    public class WorldUISetupService : SetupService
    {
        private readonly GameObject settingsMenu;
        private readonly GameObject loadingScreenPrefab;
        private readonly IObjectResolver objectResolver;
        private readonly PlayerInputReader playerInputReader;

        public WorldUISetupService(
            ClientPrefabProvider clientPrefabProvider,
            IObjectResolver objectResolver,
            PlayerInputReader playerInputReader,
            WorldSetupEvent setupEvent
        )
            : base(setupEvent)
        {
            if (clientPrefabProvider == null)
            {
                Debug.LogError("ClientPrefabProvider not set!");
                return;
            }
            settingsMenu = clientPrefabProvider.SettingMenuComponent;
            loadingScreenPrefab = clientPrefabProvider.LoadingScreenPrefab;
            this.objectResolver = objectResolver;
            this.playerInputReader = playerInputReader;
        }

        public override void Setup()
        {
            if (settingsMenu == null)
                throw new System.Exception(
                    "SettingsMenu GameObject not set in WorldUIObjectProvider!"
                );

            objectResolver.Instantiate(loadingScreenPrefab).name = "LoadingScreen";
            var instantiatedMenu = objectResolver.Instantiate(settingsMenu);
            instantiatedMenu.name = "SettingsMenu";
            instantiatedMenu.SetActive(false);
        }
    }
}
