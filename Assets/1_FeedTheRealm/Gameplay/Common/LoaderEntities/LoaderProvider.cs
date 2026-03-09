using System.Collections.Generic;
using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Common.WorldLoader
{
    public class LoaderProvider : MonoBehaviour
    {
        [Header("Loaders")]
        [SerializeField]
        private List<GameObject> loaders;

        public IReadOnlyList<ILoader> GetLoaders()
        {
            return loaders.ConvertAll(l => l.GetComponent<ILoader>());
        }
    }
}
