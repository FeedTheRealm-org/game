using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Loaders;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterInitializer : MonoBehaviour
    {
        [Inject]
        private IScriptLinker scriptLinker;

        [SerializeField]
        InitiatePlayerEvent initiatePlayerEvent;

        private void OnEnable()
        {
            initiatePlayerEvent.OnRaised += Initialize;
        }

        private void OnDisable()
        {
            initiatePlayerEvent.OnRaised -= Initialize;
        }

        public void Initialize()
        {
            if (scriptLinker == null)
                Debug.Log("Script linker is null");
            Debug.Log("Linking domain scripts for character");
            scriptLinker.LinkDomainScripts(gameObject);
        }
    }
}
