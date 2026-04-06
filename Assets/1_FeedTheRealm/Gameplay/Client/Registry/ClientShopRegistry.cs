using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Registry
{
    /// <summary>
    /// Static registry that exposes the current world's shops and their products
    /// to client-side gameplay systems (UI, tooltips, visuals).
    /// </summary>
    public static class ClientShopRegistry
    {
        public static CreatablesData CurrentWorldData { get; private set; }

        private static readonly Dictionary<string, ShopData> shopsById =
            new Dictionary<string, ShopData>();

        public static void RegisterWorldData(CreatablesData data)
        {
            CurrentWorldData = data;

            shopsById.Clear();

            if (data == null)
            {
                Debug.LogWarning("[ClientShopRegistry] RegisterWorldData called with null data");
                return;
            }

            if (data.shops != null)
            {
                foreach (var shop in data.shops)
                {
                    if (shop != null && !string.IsNullOrEmpty(shop.id))
                        shopsById[shop.id] = shop;
                }
            }

            Debug.Log($"[ClientShopRegistry] Registered {shopsById.Count} shops for world.");
        }

        public static ShopData GetShopById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            shopsById.TryGetValue(id, out var shop);
            return shop;
        }

        public static bool TryGetShop(string id, out ShopData shop)
        {
            shop = null;
            if (string.IsNullOrEmpty(id))
                return false;
            return shopsById.TryGetValue(id, out shop);
        }

        public static List<ProductData> GetProductsForShop(string shopId)
        {
            if (TryGetShop(shopId, out var shop))
                return shop.products;
            return null;
        }
    }
}
