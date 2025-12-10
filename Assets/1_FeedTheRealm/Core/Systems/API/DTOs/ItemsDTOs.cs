using System;

namespace API {
    /// <summary>
    /// DTO for item metadata response from API.
    /// Matches backend ItemMetadataResponse structure.
    /// Contains basic item information: id, name, description, category_id, sprite_id.
    /// Server-side gameplay data (stats, prices) is handled separately.
    /// </summary>
    [Serializable]
    public class ItemMetadataResponse {
        public string id;           // Item UUID
        public string name;         // Item display name
        public string description;  // Item description
        public string category_id;  // Category UUID (references ItemCategory)
        public string sprite_id;    // UUID to download sprite via ItemAssetsService
        public string created_at;   // ISO 8601 timestamp
        public string updated_at;   // ISO 8601 timestamp
    }

    [Serializable]
    public class ItemCategoryResponse {
        public string id;
        public string name;
    }

    [Serializable]
    public class ItemCategoriesListResponse {
        public ItemCategoryResponse[] categories;
    }

    /// <summary>
    /// DTO for list of items metadata response.
    /// Matches backend ItemsListResponse structure.
    /// </summary>
    [Serializable]
    public class ItemsListResponse {
        public ItemMetadataResponse[] items;
    }
}
