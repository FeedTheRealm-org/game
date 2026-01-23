using System;
using System.Threading;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

/// <summary>
/// Handles binding to the local player's <see cref="PlayerGold"/> in a network-aware,
/// async-friendly way. Exposes an event to notify subscribers when the player's gold
/// value changes and performs a best-effort bind with a timeout.
/// </summary>
namespace FeedTheRealm.UI
{
    public class HUDGoldBinder : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        private PlayerGold _boundPlayerGold;

        /// <summary>
        /// Raised when the bound player's gold changes.
        /// </summary>
        public event Action<int> OnGoldChanged;

        public void SetLogger(Logging.Logger l) => logger = l;

        /// <summary>
        /// Attempts to bind to a PlayerGold instance for the local player. Waits up to "timeoutSeconds".
        /// Returns true if binding succeeded and the OnGoldChanged subscription is active.
        /// </summary>
        public async Task<bool> BindAsync(float timeoutSeconds, CancellationToken token)
        {
            float elapsed = 0f;
            const int waitMs = 500;

            while (!token.IsCancellationRequested && elapsed < timeoutSeconds)
            {
                // Prefer the network-local player when possible
                if (NetworkClient.active && NetworkClient.localPlayer != null)
                {
                    var candidate = NetworkClient.localPlayer.GetComponent<PlayerGold>();
                    if (candidate != null)
                    {
                        Attach(candidate);
                        return true;
                    }
                }

                // find any PlayerGold in the scene
                var any = UnityEngine.Object.FindFirstObjectByType<PlayerGold>();
                if (any != null)
                {
                    Attach(any);
                    return true;
                }

                try
                {
                    await Task.Delay(waitMs, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                elapsed += waitMs / 1000f;
            }

            logger?.Log(
                "HUDGoldBinder: Bind attempt timed out or canceled",
                this,
                Logging.LogType.Warning
            );
            return false;
        }

        private void Attach(PlayerGold pg)
        {
            if (_boundPlayerGold == pg)
                return;

            Unbind();

            _boundPlayerGold = pg;
            _boundPlayerGold.OnGoldChanged += Bound_OnGoldChanged;

            // Push current value immediately
            OnGoldChanged?.Invoke(_boundPlayerGold.Gold);

            logger?.Log("HUDGoldBinder: Successfully bound to PlayerGold", this);
        }

        private void Bound_OnGoldChanged(int newValue)
        {
            OnGoldChanged?.Invoke(newValue);
        }

        public void Unbind()
        {
            if (_boundPlayerGold != null)
            {
                _boundPlayerGold.OnGoldChanged -= Bound_OnGoldChanged;
                _boundPlayerGold = null;
            }
        }
    }
}
