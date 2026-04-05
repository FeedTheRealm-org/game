using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Registry
{
    /// <summary>
    /// Static registry that exposes the current world's shops and their products
    /// to server-side
    /// </summary>
    public static class ServerShopRegistry
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
                Debug.LogWarning("[ServerShopRegistry] RegisterWorldData called with null data");
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

            Debug.Log($"[ServerShopRegistry] Registered {shopsById.Count} shops for world.");
        }

        public static ProductData GetProductById(string id)
        {
            foreach (var shop in shopsById.Values)
            {
                if (shop?.products == null)
                    continue;

                var product = shop.products.Find(p => p.productId == id);
                if (product != null)
                    return product;
            }
            return null;
        }
    }
}
