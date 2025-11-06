
namespace API {
    /* --- Requests --- */
    [System.Serializable]
    public class UpdateCharacterInfoRequest {
        public string character_name;

        public string character_bio;
    }

    /* --- Responses --- */
    [System.Serializable]
    public class CharacterInfoResponse {
        public string character_bame;

        public string character_bio;
    }
}

