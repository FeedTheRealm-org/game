using FTR.Core.Client.EventChannels.Gold;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks updates on the local player's gold and notifies the HUD via event channels.
/// </summary>
public class GoldView : MonoBehaviour
{
    [Inject]
    [SerializeField]
    private GoldChangedEvent goldChangedEvent;

    [SerializeField]
    private GoldStateStorage stateStorage;

    public void Initialize(GoldStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnGoldChanged += OnGoldChanged;
    }

    private void OnDestroy()
    {
        if (stateStorage == null)
            return;
        stateStorage.OnGoldChanged -= OnGoldChanged;
    }

    private void OnGoldChanged(int newGold)
    {
        Debug.Log($"GoldView gold changed: {newGold}");
        goldChangedEvent.Raise(newGold);
    }
}
