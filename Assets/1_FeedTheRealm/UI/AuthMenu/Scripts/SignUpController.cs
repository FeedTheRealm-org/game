using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class SignUpController : MonoBehaviour {
    [SerializeField]
    private API.AuthService authService;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private SceneAsset targetScene;

    [SerializeField]
    private SceneAsset otherFormScene;

    [SerializeField]
    private Logging.Logger logger;

    private VisualElement ui;

    private Button _signUpButton;
    private TextField _emailField;
    private TextField _passwordField;
    private TextField _repeatedPasswordField;
    private Label _changeButton;

    private AsyncOperation preloadOperation;

    private void Awake() {
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    private void OnEnable() {
        logger.Log("SignUpController enabled.", this);

        _signUpButton = ui.Q<Button>("SignUpButton");
        _signUpButton.clicked += OnLoginClicked;

        _changeButton = ui.Q<Label>("LoginChangeButton");
        _changeButton.RegisterCallback<ClickEvent>(evt => {
            logger.Log("Navigating to " + otherFormScene.name + ".", this);
            SceneManager.LoadScene(otherFormScene.name);
        });

        _emailField = ui.Q<TextField>("EmailField");
        _passwordField = ui.Q<TextField>("PasswordField");
        _repeatedPasswordField = ui.Q<TextField>("RepeatPasswordField");
    }

    private void OnDisable() {
        if (_signUpButton != null)
            _signUpButton.clicked -= OnLoginClicked;
    }

    private void OnLoginClicked() {
        logger.Log("Login Button Clicked", this);
        logger.Log("Email: " + _emailField.value, this);
        logger.Log("Password: " + _passwordField.value, this);

        if (_passwordField.value != _repeatedPasswordField.value) {
            logger.Log("Passwords do not match", this, Logging.LogType.Error);
            return;
        }

        StartCoroutine(authService.SignUp(_emailField.value, _passwordField.value, (success) => {
            if (success) {
                logger.Log("SignUp successful, email: " + _emailField.value, this);
                if (preloadOperation != null) {
                    preloadOperation.allowSceneActivation = true;
                    logger.Log("Activating preloaded " + targetScene.name + ".", this);
                } else {
                    SceneManager.LoadScene(targetScene.name);
                }
            } else {
                logger.Log("Login failed", this, Logging.LogType.Error);
            }
        }));
    }
}
