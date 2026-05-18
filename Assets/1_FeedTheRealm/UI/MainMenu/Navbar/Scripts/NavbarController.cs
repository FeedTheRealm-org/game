using FTR.Core.Client.EntryPoints;
using FTR.Gameplay.Client.EntryPoints;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace FTR.UI.Homepage.Navbar
{
    public class NavbarController : MonoBehaviour, INavbarController
    {
        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private API.PlayerService playerService;

        [SerializeField]
        private string sectionName = "Section";

        private GameObject homeMenuInstance;
        private GameObject profileMenuInstance;
        private GameObject gemStoreInstance;

        private VisualElement _root;
        private Button homeButton;
        private Button playerProfileButton;
        private Button gemStoreButton;
        private Label sectionLabel;

        private const string EditCharacterLabel = "Edit Character";

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
                        if (!string.IsNullOrEmpty(err))
                        {
                            logger.Log(
                                $"Failed to fetch character info for navbar: {err}",
                                this,
                                Logging.LogType.Warning
                            );
                        }

                        session.CharacterName = data?.character_name?.Trim() ?? string.Empty;
                        UpdateProfileButtonText();
                    }
                )
            );

            sectionLabel = _root.Q<Label>("SectionLabel");
            if (sectionLabel == null)
            {
                logger.Log(
                    "SectionLabel not found in the UI Document.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
            sectionLabel.text = sectionName;

            var body = _root.Q<VisualElement>("Body");
            if (body == null)
            {
                logger.Log("Body not found in the UI Document.", this, Logging.LogType.Error);
                return;
            }

            homeButton = body.Q<Button>("HomeButton");
            if (homeButton == null)
            {
                logger.Log("HomeButton not found in Body.", this, Logging.LogType.Error);
                return;
            }
            homeButton.clicked += OnHomeButtonClicked;

            playerProfileButton = body.Q<Button>("ProfileButton");
            if (playerProfileButton == null)
            {
                logger.Log("ProfileButton not found in Body.", this, Logging.LogType.Error);
                return;
            }
            UpdateProfileButtonText();
            playerProfileButton.clicked += OnProfileButtonClicked;

            gemStoreButton = body.Q<Button>("GemStoreButton");
            if (gemStoreButton == null)
            {
                logger.Log("GemStoreButton not found in Body.", this, Logging.LogType.Error);
                return;
            }
            gemStoreButton.clicked += OnGemStoreButtonClicked;
        }

        private void OnDisable()
        {
            if (homeButton != null)
                homeButton.clicked -= OnHomeButtonClicked;
            if (playerProfileButton != null)
                playerProfileButton.clicked -= OnProfileButtonClicked;
            if (gemStoreButton != null)
                gemStoreButton.clicked -= OnGemStoreButtonClicked;
        }

        /* --- Setters --- */

        public void SetHomeMenuInstance(GameObject instance)
        {
            homeMenuInstance = instance;
        }

        public void SetProfileMenuInstance(GameObject instance)
        {
            profileMenuInstance = instance;
            CharacterEditController characterEditorController =
                profileMenuInstance.GetComponentInChildren<CharacterEditController>();
            if (characterEditorController != null)
            {
                var playerId = session?.UserID?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(playerId))
                {
                    logger.Log(
                        "Session.UserId is empty. Character editor will receive an empty player id.",
                        this,
                        Logging.LogType.Warning
                    );
                }

                characterEditorController.SetAssetsWorldId(new System.Guid().ToString());
                characterEditorController.SetAssetsPlayerId(playerId);
            }
        }

        public void SetGemStoreInstance(GameObject instance)
        {
            gemStoreInstance = instance;
        }

        /* --- Handlers --- */

        private void OnHomeButtonClicked()
        {
            if (homeMenuInstance == null)
            {
                logger.Log("HomeMenu instance is not set.", this, Logging.LogType.Error);
                return;
            }
            gemStoreInstance?.SetActive(false);
            profileMenuInstance?.SetActive(false);
            homeMenuInstance.SetActive(true);
        }

        private void OnGemStoreButtonClicked()
        {
            if (gemStoreInstance == null)
            {
                logger.Log("GemStore instance is not set.", this, Logging.LogType.Error);
                return;
            }
            homeMenuInstance?.SetActive(false);
            profileMenuInstance?.SetActive(false);
            gemStoreInstance.SetActive(true);
        }

        private void OnProfileButtonClicked()
        {
            if (profileMenuInstance == null)
            {
                logger.Log("ProfileMenu instance is not set.", this, Logging.LogType.Error);
                return;
            }
            homeMenuInstance?.SetActive(false);
            gemStoreInstance?.SetActive(false);
            profileMenuInstance.SetActive(true);
        }

        private void UpdateProfileButtonText()
        {
            if (playerProfileButton == null)
                return;

            if (string.IsNullOrWhiteSpace(session?.CharacterName))
            {
                playerProfileButton.text = EditCharacterLabel;
                return;
            }

            playerProfileButton.text = $"Edit {session.CharacterName}";
        }
    }
}
