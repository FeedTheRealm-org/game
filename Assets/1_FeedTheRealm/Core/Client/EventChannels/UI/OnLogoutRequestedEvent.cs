using UnityEngine;

namespace FTR.Core.Client.EventChannels.UI
{
    [CreateAssetMenu(
        menuName = "Events/Client/UI/OnLogoutRequestedEvent",
        fileName = "OnLogoutRequestedEvent"
    )]
    public class OnLogoutRequestedEvent : ScriptableObject
    {
        public event System.Action OnRaised;

        public void Raise()
        {
            OnRaised?.Invoke();
        }

        private void OnDisable()
        {
            OnRaised = null;
        }
    }
}
