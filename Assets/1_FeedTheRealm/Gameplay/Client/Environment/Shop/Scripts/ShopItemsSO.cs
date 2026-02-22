using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemsSO", menuName = "Scriptable Objects/ShopItemsSO")]
public class ShopItemsSO : ScriptableObject
{
    public FTRShared.Runtime.Models.ShopData shopData;

    public FTRShared.Runtime.Models.ShopData GetShopData()
    {
        return shopData;
    }

    public void SetShopData(FTRShared.Runtime.Models.ShopData newShopData)
    {
        Debug.Log($"Setting Shop Data: {newShopData}");
        shopData = newShopData;
    }
}
