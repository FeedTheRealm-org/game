using System;
using UnityEngine;

namespace FTR.Gameplay.Client.EntryPoints
{
    /// <summary>
    /// Disposable helper that fires OnDisabled when the GameObject
    /// it lives on receives OnDisable (that is, when SetActive(false)
    /// is called on it).
    public class OnDisableNotifier : MonoBehaviour
    {
        public event Action OnDisabled;

        private void OnDisable()
        {
            OnDisabled?.Invoke();
        }
    }
}
