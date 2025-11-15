
namespace API {
    /* Responses */

    [System.Serializable]
    public class SpriteCategoryResponse {
        public string category_id;
        public string category_name;
    }

    [System.Serializable]
    public class SpriteResponse {
        public string sprite_id;
        public string sprite_url;
    }

    [System.Serializable]
    public class SpriteCategoryListResponse {
        public SpriteCategoryResponse[] category_list;
    }

    [System.Serializable]
    public class SpritesListResponse {
        public SpriteResponse[] sprites_list;
    }
}

