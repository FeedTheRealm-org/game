using System;
using System.Collections.Generic;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Server.WorldLoader.Loaders;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.WorldLoader
{
    public class ServerLoaderComponents : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> loaders;

        public IReadOnlyList<ILoader> GetLoaders()
        {
            return loaders.ConvertAll(l => l.GetComponent<ILoader>());
        }
    }
}
