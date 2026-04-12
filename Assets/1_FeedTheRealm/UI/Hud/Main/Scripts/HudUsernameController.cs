using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.Hud.Main
{
    [RequireComponent(typeof(UIDocument))]
    public class HudUsernameController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private API.PlayerService playerService;

        private Label _usernameLabel;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                logger.Log("UIDocument root not found.", this, Logging.LogType.Error);
                return;
            }

            _usernameLabel = root.Q<Label>("Username");
            if (_usernameLabel == null)
            {
                logger.Log("Username label not found in UIDocument.", this, Logging.LogType.Error);
                return;
            }

            UpdateUsernameLabel(session?.CharacterName ?? "Guest");

            if (playerService == null)
            {
                logger.Log("PlayerService is not assigned.", this, Logging.LogType.Error);
                return;
            }

            var hasSession = session != null;
            StartCoroutine(
                playerService.GetCharacterInfo(
                    (data, error) =>
                    {
                        var username = data?.character_name;
                        if (!string.IsNullOrEmpty(username))
                        {
                            if (hasSession)
                            {
                                session.CharacterName = username;
                            }
                            UpdateUsernameLabel(username);
                        }
                        else if (!string.IsNullOrEmpty(error))
                        {
                            logger.Log(
                                $"Failed to load username: {error}",
                                this,
                                Logging.LogType.Warning
                            );
                        }
                    }
                )
            );
        }

        private void UpdateUsernameLabel(string username)
        {
            if (_usernameLabel == null)
                return;

            _usernameLabel.text = string.IsNullOrEmpty(username) ? "Guest" : username;
        }
    }
}
