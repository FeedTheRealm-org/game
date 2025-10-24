using UnityEngine;

namespace Session {
  [CreateAssetMenu(fileName = "Session", menuName = "Scriptable Objects/Session")]
  public class Session : ScriptableObject {
    public string APIToken { get; private set; }
    public string Email { get; private set; }

    public void SetAPIToken(string token) {
      APIToken = token;
    }

    public void SetEmail(string email) {
      Email = email;
    }

    public void ClearSession() {
      APIToken = null;
      Email = null;
    }
  }
}
