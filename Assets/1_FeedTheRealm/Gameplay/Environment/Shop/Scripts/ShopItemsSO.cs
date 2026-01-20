using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemsSO", menuName = "Scriptable Objects/ShopItemsSO")]
public class ShopItemsSO : ScriptableObject
{
    public Models.ShopData shopData;

    public Models.ShopData GetShopData()
    {
        return shopData;
    }

    public void SetShopData(Models.ShopData newShopData)
    {
        shopData = newShopData;
    }
}
