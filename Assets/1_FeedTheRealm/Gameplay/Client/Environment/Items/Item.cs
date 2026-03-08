using UnityEngine;

public abstract class ItemObject : MonoBehaviour
{
    [SerializeField]
    private string id;

    [SerializeField]
    private string itemName;

    [SerializeField]
    private Sprite sprite;

    [SerializeField]
    private string description;

    public string ItemName => itemName;
    public string Description => description;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        OnPickup();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;
        Destroy(gameObject);
    }

    public abstract void OnPickup();
}
