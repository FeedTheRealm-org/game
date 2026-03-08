using UnityEngine;

public class DebugItem : ItemObject
{
    public override void OnPickup()
    {
        Debug.Log("Picked up " + ItemName);
    }
}
