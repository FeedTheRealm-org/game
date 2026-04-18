using FTR.Core.Common.Config;
using FTR.Gameplay.Bot.EntryPoints.Scopes;
using FTR.Gameplay.Bot.Linkers;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Bot.Characters
{
    public class BotCharacterNetworkInitializer : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            base.OnStartClient();

            var botWorldInitiator = FindFirstObjectByType<BotWorldInitiator>();
            if (
                botWorldInitiator == null
                || botWorldInitiator.CurrentRuntimeRole != RuntimeRole.Bot
            )
                return;

            var networkAdapter = GetComponent<NetworkAdapter>();
            if (networkAdapter == null || !networkAdapter.IsLocalPlayer)
                return;

            var botPlayerLinker = (BotPlayerLinker)
                botWorldInitiator.Container.Resolve(typeof(BotPlayerLinker));
            if (botPlayerLinker == null)
            {
                throw new System.NullReferenceException(
                    "BotPlayerLinker not found in BotWorldInitiator container."
                );
                return;
            }

            botPlayerLinker.Link(gameObject);

            Debug.Log($"BotCharacterNetworkInitializer: OnStartClient - Linked {gameObject.name}");
        }
    }
}
