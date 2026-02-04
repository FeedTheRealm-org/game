using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class NavbarController : MonoBehaviour
{
    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private GameObject profileMenuPrefab;

    [SerializeField]
    private SceneReference gameplayScene;

    [SerializeField]
    private string sectionName = "Section";

    private VisualElement _root;
    private Button playerProfileButton;
    private Label sectionLabel;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        if (_root == null)
        {
            logger.Log("UIDocument root not found.", this, Logging.LogType.Error);
            return;
        }

        if (session == null)
        {
            logger.Log("Session is not assigned.", this, Logging.LogType.Error);
            return;
        }

        StartCoroutine(
            playerService.GetCharacterInfo(
                (data, err) =>
                {
                    session.CharacterName = data?.character_name ?? "Guest";
                }
            )
        );

        sectionLabel = _root.Q<Label>("SectionLabel");
        if (sectionLabel == null)
        {
            logger.Log("SectionLabel not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }
        sectionLabel.text = sectionName;

        var body = _root.Q<VisualElement>("Body");
        if (body == null)
        {
            logger.Log("Body not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        playerProfileButton = body.Q<Button>("ProfileButton");
        if (playerProfileButton == null)
        {
            logger.Log("ProfileButton not found in Body.", this, Logging.LogType.Error);
            return;
        }
        playerProfileButton.text = string.IsNullOrEmpty(session.CharacterName)
            ? "Guest"
            : session.CharacterName;
        playerProfileButton.clicked += onProfileButtonClicked;
    }

    private void OnDisable()
    {
        if (playerProfileButton != null)
        {
            playerProfileButton.clicked -= onProfileButtonClicked;
        }
    }

    private void onProfileButtonClicked()
    {
        if (profileMenuPrefab == null)
        {
            logger.Log("ProfileMenuPrefab is not assigned.", this, Logging.LogType.Error);
            return;
        }
        profileMenuPrefab.SetActive(!profileMenuPrefab.activeSelf);
    }
}
