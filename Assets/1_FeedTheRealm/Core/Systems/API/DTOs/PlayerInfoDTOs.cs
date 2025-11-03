
namespace API {
    /* --- Requests --- */
    [System.Serializable]
    public class UpdateCharacterInfoRequest {
        public string characterName;
        public string characterBio;
    }

    /* --- Responses --- */
    [System.Serializable]
    public class CharacterInfoResponse {
        public string characterName;
        public string characterBio;
    }
}

