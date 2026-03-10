using FTR.Core.Common.Enums;
using FTR.Core.Common.Loaders;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public class LootItemInitializer : MonoBehaviour
    {
        [Inject]
        [Key(RegisterTypes.LootItem)]
        private IScriptLinker scriptLinker;

        public void Initialize()
        {
            if (scriptLinker == null)
            {
                Debug.LogError(
                    $"[LootItemInitializer] scriptLinker not injected for '{gameObject.name}', skipping initialization."
                );
                return;
            }
            scriptLinker.LinkDomainScripts(gameObject, false);
        }
    }
}
