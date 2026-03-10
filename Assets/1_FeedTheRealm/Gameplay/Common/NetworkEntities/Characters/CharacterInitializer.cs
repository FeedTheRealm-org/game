using FTR.Core.Common.Enums;
using FTR.Core.Common.Loaders;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterInitializer : MonoBehaviour
    {
        [Inject]
        [Key(RegisterTypes.Character)]
        private IScriptLinker scriptLinker;

        [SerializeField]
        private bool linkNPC;

        public void Initialize()
        {
            if (scriptLinker == null)
            {
                Debug.LogWarning(
                    $"[CharacterInitializer] scriptLinker not injected for '{gameObject.name}', skipping initialization."
                );
                return;
            }
            Debug.Log("Linking domain scripts for character");
            scriptLinker.LinkDomainScripts(gameObject, linkNPC);
        }
    }
}
