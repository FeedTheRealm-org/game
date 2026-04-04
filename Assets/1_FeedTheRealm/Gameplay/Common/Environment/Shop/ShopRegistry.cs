using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Shop
{
    [CreateAssetMenu(fileName = "ShopRegistry", menuName = "Scriptable Objects/ShopRegistry")]
    public class ShopRegistry : ScriptableObject
    {
        [SerializeField]
        private List<ShopData> shops = new();

        private Dictionary<string, ShopData> _lookup;

        private void OnEnable() => BuildLookup(shops);

        public bool TryGetItem(string shopId, string productId, out ProductData productData)
        {
            productData = null;

            if (string.IsNullOrEmpty(shopId))
            {
                Debug.LogWarning("[ShopRegistry] TryGetItem called with null or empty shopId.");
                return false;
            }

            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogWarning("[ShopRegistry] TryGetItem called with null or empty productId.");
                return false;
            }

            if (_lookup == null)
                BuildLookup(shops);

            if (_lookup.TryGetValue(shopId, out var shopData))
            {
                productData = shopData.products.Find(p => p.productId == productId);
                return productData != null;
            }

            return false;
        }

        public void BuildLookup(List<ShopData> shopList)
        {
            _lookup = new Dictionary<string, ShopData>();

            if (shopList == null)
            {
                Debug.LogWarning("[ShopRegistry] BuildLookup called with null shopList.");
                return;
            }

            foreach (var shop in shopList)
            {
                if (string.IsNullOrEmpty(shop.id))
                {
                    Debug.LogWarning("[ShopRegistry] Skipping shop with null or empty ShopId.");
                    continue;
                }

                if (_lookup.ContainsKey(shop.id))
                {
                    Debug.LogWarning(
                        $"[ShopRegistry] Duplicate ShopId detected: {shop.id}. Skipping."
                    );
                    continue;
                }

                _lookup.Add(shop.id, shop);
            }
        }
    }
}
