using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class NpcDialogInactivityTimer
    {
        private readonly MonoBehaviour owner;
        private readonly float timeout;
        private readonly Dictionary<uint, Coroutine> _coroutines = new();

        public NpcDialogInactivityTimer(MonoBehaviour owner, float timeout)
        {
            this.owner = owner;
            this.timeout = timeout;
        }

        public void Restart(uint playerNetId, IInteractor interactor)
        {
            Stop(playerNetId);
            if (interactor == null)
                return;
            _coroutines[playerNetId] = owner.StartCoroutine(
                TimerCoroutine(playerNetId, interactor)
            );
        }

        public void Stop(uint playerNetId)
        {
            if (_coroutines.TryGetValue(playerNetId, out var c) && c != null)
                owner.StopCoroutine(c);
            _coroutines.Remove(playerNetId);
        }

        private IEnumerator TimerCoroutine(uint playerNetId, IInteractor interactor)
        {
            yield return new WaitForSeconds(timeout);
            interactor.FinishInteracting();
        }
    }
}
