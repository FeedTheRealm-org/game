using System.Collections.Generic;

namespace API
{
    /* --- Requests --- */
    [System.Serializable]
    public class PatchCharacterInfoRequest
    {
        public string character_name;
        public string character_bio;
        public Dictionary<string, string> category_sprites;
    }

    [System.Serializable]
    public class IssueWorldJoinTokenRequest
    {
        public string world_id;
    }

    [System.Serializable]
    public class ConsumeWorldJoinTokenRequest
    {
        public string token_id;
    }

    /* --- Responses --- */
    [System.Serializable]
    public class CharacterInfoResponse
    {
        public string character_name;
        public string character_bio;
        public Dictionary<string, string> category_sprites;
    }

    [System.Serializable]
    public class WorldJoinTokenResponse
    {
        public string token_id;
        public string expires_at;
    }

    [System.Serializable]
    public class ConsumeWorldJoinTokenResponse
    {
        public string user_id;
        public string world_id;
    }
}
