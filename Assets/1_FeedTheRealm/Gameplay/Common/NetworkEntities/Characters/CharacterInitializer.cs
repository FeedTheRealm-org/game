using FTR.Core.Common.Loaders;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterInitializer : MonoBehaviour
    {
        [Inject]
        private IScriptLinker scriptLinker;

        private void Awake()
        {
            scriptLinker.LinkDomainScripts(gameObject);
        }
    }
}
