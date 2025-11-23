using System.Collections.Generic;

namespace API {
  [System.Serializable]
  public class WorldsData {
    public string id;
    public string user_id;
    public string name;
    public string data;
    public string created_at;
    public string updated_at;
  }

  /* --- Responses --- */
  [System.Serializable]
  public class WorldInfoResponse {
    public List<WorldsData> worlds;

    public int amount;
    public int limit;
    public int offset;
  }
}

