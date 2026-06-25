using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Client.Registry;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

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

        [Inject]
        private ISoundPlayer soundPlayer;

        [Inject]
        private API.ExportsService exportService;

        private const string UpdateDownloadUrl = "https://www.feedtherealm.world/";

        private GameObject homeMenuInstance;
        private GameObject profileMenuInstance;
        private GameObject gemStoreInstance;
        private GameObject navBarSettingsInstance;

        private VisualElement _root;
        private Button homeButton;
        private Button playerProfileButton;
        private Button gemStoreButton;
        private Button settingsButton;

        private VisualElement _updateNotice;
        private Label _updateNoticeText;
        private Button _updateNoticeLink;
        private Button _updateNoticeCloseButton;

        private bool _profileLocked = false;

        private const string EditCharacterLabel = "Edit Character";

        // CSS class names for selected states
        private const string HomeSelectedClass = "navbar-button-home-selected";
        private const string ProfileSelectedClass = "navbar-button-profile-selected";
        private const string GemSelectedClass = "navbar-button-gem-selected";
        private const string SettingsSelectedClass = "navbar-button-settings-selected";

        private const string DisabledClass = "navbar-button-disabled";
        private const string UpdateNoticeHiddenClass = "update-notice--hidden";

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
                            logger.Log(
                                $"Failed to fetch character info for navbar: {err}",
                                this,
                                Logging.LogType.Warning
                            );

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
                settingsButton.clicked += OnSettingsButtonClicked;

            _updateNotice = _root.Q<VisualElement>("UpdateNotice");
            _updateNoticeText = _root.Q<Label>("UpdateNoticeText");
            _updateNoticeLink = _root.Q<Button>("UpdateNoticeLink");
            _updateNoticeCloseButton = _root.Q<Button>("UpdateNoticeCloseButton");

            if (_updateNoticeLink != null)
                _updateNoticeLink.clicked += OnUpdateNoticeLinkClicked;
            if (_updateNoticeCloseButton != null)
                _updateNoticeCloseButton.clicked += OnUpdateNoticeCloseClicked;

            ApplyLockVisuals();

            if (_profileLocked)
            {
                ToastNotification.Show("Profile creation required.", "error", Color.orange);
                SetSelectedButton(playerProfileButton, ProfileSelectedClass);
            }
            else
                SetSelectedButton(homeButton, HomeSelectedClass);

            CheckForUpdates();
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
            if (_updateNoticeLink != null)
                _updateNoticeLink.clicked -= OnUpdateNoticeLinkClicked;
            if (_updateNoticeCloseButton != null)
                _updateNoticeCloseButton.clicked -= OnUpdateNoticeCloseClicked;
        }

        private async void CheckForUpdates()
        {
            if (exportService == null)
            {
                logger.Log(
                    "[NavbarController] ExportService is not assigned.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            var (latestVersion, error) = await exportService.GetLatestVersion();

            // Guard against the component being disabled/destroyed while awaiting.
            if (this == null || _updateNotice == null)
                return;

            if (!string.IsNullOrEmpty(error))
            {
                logger.Log(
                    $"[NavbarController] Failed to check latest version: {error}",
                    this,
                    Logging.LogType.Warning
                );
                ShowUpdateNotice("Version could not be validated.");
                return;
            }

            string current = NormalizeVersion(Application.version);
            string latest = NormalizeVersion(latestVersion);

            if (current != latest)
                ShowUpdateNotice("A new version is available.");
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return string.Empty;

            version = version.Trim();
            return version.StartsWith("v", System.StringComparison.OrdinalIgnoreCase)
                ? version.Substring(1)
                : version;
        }

        private void ShowUpdateNotice(string message)
        {
            if (_updateNoticeText != null)
                _updateNoticeText.text = message;

            _updateNotice?.RemoveFromClassList(UpdateNoticeHiddenClass);
        }

        private void OnUpdateNoticeLinkClicked()
        {
            Application.OpenURL(UpdateDownloadUrl);
        }

        private void OnUpdateNoticeCloseClicked()
        {
            _updateNotice?.AddToClassList(UpdateNoticeHiddenClass);
        }

        public void SetHomeMenuInstance(GameObject instance)
        {
            homeMenuInstance = instance;
        }

        public void SetProfileMenuInstance(GameObject instance)
        {
            profileMenuInstance = instance;

            var characterEditorController =
                profileMenuInstance.GetComponentInChildren<CharacterEditController>();

            if (characterEditorController != null)
            {
                var playerId = session?.UserID?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(playerId))
                    logger.Log("Session.UserId is empty.", this, Logging.LogType.Warning);

                characterEditorController.SetAssetsWorldId(new System.Guid().ToString());
                characterEditorController.SetAssetsPlayerId(playerId);
            }
        }

        public void SetGemStoreInstance(GameObject instance)
        {
            gemStoreInstance = instance;
        }

        public void SetNavBarSettingsInstance(GameObject instance)
        {
            navBarSettingsInstance = instance;
        }

        public void SetProfileLocked(bool locked)
        {
            _profileLocked = locked;
            ApplyLockVisuals();

            if (locked)
                SetSelectedButton(playerProfileButton, ProfileSelectedClass);

            logger.Log(
                locked
                    ? "[NavbarController] Navigation locked — profile creation required."
                    : "[NavbarController] Navigation unlocked.",
                this
            );
        }

        private void ApplyLockVisuals()
        {
            SetButtonLockedVisual(homeButton, _profileLocked);
            SetButtonLockedVisual(gemStoreButton, _profileLocked);
            SetButtonLockedVisual(settingsButton, _profileLocked);
        }

        private void SetButtonLockedVisual(Button button, bool locked)
        {
            if (button == null)
                return;

            if (locked)
                button.AddToClassList(DisabledClass);
            else
                button.RemoveFromClassList(DisabledClass);
        }

        private void SetSelectedButton(Button button, string selectedClass)
        {
            homeButton?.RemoveFromClassList(HomeSelectedClass);
            playerProfileButton?.RemoveFromClassList(ProfileSelectedClass);
            gemStoreButton?.RemoveFromClassList(GemSelectedClass);
            settingsButton?.RemoveFromClassList(SettingsSelectedClass);
            button?.AddToClassList(selectedClass);
        }

        private void OnHomeButtonClicked()
        {
            if (_profileLocked)
            {
                ToastNotification.Show(
                    "Complete profile creation to access the Home menu.",
                    "error",
                    Color.orange
                );
                return;
            }

            if (homeMenuInstance == null)
            {
                logger.Log("HomeMenu instance is not set.", this, Logging.LogType.Error);
                return;
            }

            navBarSettingsInstance?.SetActive(false);
            gemStoreInstance?.SetActive(false);
            profileMenuInstance?.SetActive(false);
            homeMenuInstance.SetActive(true);

            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);

            SetSelectedButton(homeButton, HomeSelectedClass);
        }

        private void OnProfileButtonClicked()
        {
            if (profileMenuInstance == null)
            {
                logger.Log("ProfileMenu instance is not set.", this, Logging.LogType.Error);
                return;
            }

            navBarSettingsInstance?.SetActive(false);
            homeMenuInstance?.SetActive(false);
            gemStoreInstance?.SetActive(false);
            profileMenuInstance.SetActive(true);

            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
            SetSelectedButton(playerProfileButton, ProfileSelectedClass);
        }

        private void OnGemStoreButtonClicked()
        {
            if (_profileLocked)
            {
                ToastNotification.Show(
                    "Complete profile creation to access the Gem Store.",
                    "error",
                    Color.orange
                );
                return;
            }

            if (gemStoreInstance == null)
            {
                logger.Log("GemStore instance is not set.", this, Logging.LogType.Error);
                return;
            }

            navBarSettingsInstance?.SetActive(false);
            homeMenuInstance?.SetActive(false);
            profileMenuInstance?.SetActive(false);
            gemStoreInstance.SetActive(true);

            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
            SetSelectedButton(gemStoreButton, GemSelectedClass);
        }

        private void OnSettingsButtonClicked()
        {
            if (_profileLocked)
            {
                ToastNotification.Show(
                    "Complete profile creation to access the Settings menu.",
                    "error",
                    Color.orange
                );
                return;
            }

            if (navBarSettingsInstance == null)
            {
                logger.Log("NavBarSettings instance is not set.", this, Logging.LogType.Error);
                return;
            }

            homeMenuInstance?.SetActive(false);
            profileMenuInstance?.SetActive(false);
            gemStoreInstance?.SetActive(false);
            navBarSettingsInstance.SetActive(true);

            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
            SetSelectedButton(settingsButton, SettingsSelectedClass);
        }
    }
}
