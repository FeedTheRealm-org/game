using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class NavbarController : MonoBehaviour {
    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private GameObject profileMenuPrefab;

    [SerializeField]
    private SceneReference gameplayScene;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Containers
    private VisualElement _root;
    private Button _profileButton;
    private Button _playButton;
    private Label _nameTag;

    private void OnEnable() {
        if (session == null) {
            logger.Log("Session is not assigned.", this, Logging.LogType.Error);
            return;
        }

        _root = GetComponent<UIDocument>().rootVisualElement;
        var body = _root.Q<VisualElement>("Body");
        if (body == null) {
            logger.Log("Body not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        _profileButton = body.Q<Button>("ProfileButton");
        if (_profileButton == null) {
            logger.Log("ProfileButton not found in Body.", this, Logging.LogType.Error);
            return;
        }
        _playButton = body.Q<Button>("PlayButton");
        if (_profileButton == null) {
            logger.Log("PlayButton not found in Body.", this, Logging.LogType.Error);
            return;
        }
        _nameTag = body.Q<Label>("NameTag");
        if (_nameTag == null) {
            logger.Log("NameTag not found in Body.", this, Logging.LogType.Error);
            return;
        }

        _nameTag.text = session.CharacterName ?? "Guest";
        _profileButton.clicked += onProfileButtonClicked;
        _playButton.clicked += onPlayButtonClicked;
    }

    private void OnDisable() {
        if (_profileButton != null) {
            _profileButton.clicked -= onProfileButtonClicked;
            _playButton.clicked -= onPlayButtonClicked;
        }
    }

    private void onProfileButtonClicked() {
        if (profileMenuPrefab == null) {
            logger.Log("ProfileMenuPrefab is not assigned.", this, Logging.LogType.Error);
            return;
        }
        profileMenuPrefab.SetActive(!profileMenuPrefab.activeSelf);
    }

    private void onPlayButtonClicked() {
        logger.Log("Play button clicked - starting game...", this);
        SceneManager.LoadScene(gameplayScene.SceneName);
    }
}
