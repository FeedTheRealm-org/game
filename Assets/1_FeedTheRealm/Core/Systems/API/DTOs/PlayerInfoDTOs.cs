using System.Collections.Generic;

namespace API {
    /* --- Requests --- */
    [System.Serializable]
    public class PatchCharacterInfoRequest {
        public string character_name;
        public string character_bio;
        public Dictionary<string, string> category_sprites;
    }

    /* --- Responses --- */
    [System.Serializable]
    public class CharacterInfoResponse {
        public string character_name;
        public string character_bio;
        public Dictionary<string, string> category_sprites;
    }
}

