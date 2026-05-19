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

        private GameObject homeMenuInstance;
        private GameObject profileMenuInstance;
        private GameObject gemStoreInstance;

        private VisualElement _root;
        private Button homeButton;
        private Button playerProfileButton;
        private Button gemStoreButton;
        private Button settingsButton;

        private const string EditCharacterLabel = "Edit Character";

        // CSS class names for selected states
        private const string HomeSelectedClass = "navbar-button-home-selected";
        private const string ProfileSelectedClass = "navbar-button-profile-selected";
        private const string GemSelectedClass = "navbar-button-gem-selected";
        private const string SettingsSelectedClass = "navbar-button-settings-selected";

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
                    }
                )
            );

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
            playerProfileButton.clicked += OnProfileButtonClicked;

            gemStoreButton = body.Q<Button>("GemStoreButton");
            if (gemStoreButton == null)
            {
                logger.Log("GemStoreButton not found in Body.", this, Logging.LogType.Error);
                return;
            }
            gemStoreButton.clicked += OnGemStoreButtonClicked;

            settingsButton = body.Q<Button>("SettingsButton");
            if (settingsButton != null)
            {
                settingsButton.clicked += OnSettingsButtonClicked;
            }

            // Set home as selected by default
            SetSelectedButton(homeButton, HomeSelectedClass);
        }

        private void OnDisable()
        {
            if (homeButton != null)
                homeButton.clicked -= OnHomeButtonClicked;
            if (playerProfileButton != null)
                playerProfileButton.clicked -= OnProfileButtonClicked;
            if (gemStoreButton != null)
                gemStoreButton.clicked -= OnGemStoreButtonClicked;
            if (settingsButton != null)
                settingsButton.clicked -= OnSettingsButtonClicked;
        }

        /* --- Selected State Management --- */

        private void SetSelectedButton(Button button, string selectedClass)
        {
            // Remove all selected classes from all buttons
            homeButton?.RemoveFromClassList(HomeSelectedClass);
            playerProfileButton?.RemoveFromClassList(ProfileSelectedClass);
            gemStoreButton?.RemoveFromClassList(GemSelectedClass);
            settingsButton?.RemoveFromClassList(SettingsSelectedClass);

            // Add selected class to the active button
            button?.AddToClassList(selectedClass);
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

            SetSelectedButton(homeButton, HomeSelectedClass);
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

            SetSelectedButton(gemStoreButton, GemSelectedClass);
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

            SetSelectedButton(playerProfileButton, ProfileSelectedClass);
        }

        private void OnSettingsButtonClicked()
        {
            // Implement settings logic here when you have a settings menu
            logger.Log("Settings button clicked - not yet implemented", this, Logging.LogType.Info);

            SetSelectedButton(settingsButton, SettingsSelectedClass);
        }
    }
}
