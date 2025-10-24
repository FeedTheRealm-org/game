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
    private Logging.Logger logger;

    private VisualElement ui;

    private Button _loginButton;
    private TextField _emailField;
    private TextField _passwordField;
    private TextField _repeatedPasswordField;

    private AsyncOperation preloadOperation;

    private void Awake() {
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    private void OnEnable() {
        logger.Log("SignUpController enabled.", this);

        _loginButton = ui.Q<Button>("SignUpButton");
        _loginButton.clicked += OnLoginClicked;

        _emailField = ui.Q<TextField>("EmailField");
        _passwordField = ui.Q<TextField>("PasswordField");
        _repeatedPasswordField = ui.Q<TextField>("RepeatPasswordField");
    }

    private void OnDisable() {
        if (_loginButton != null)
            _loginButton.clicked -= OnLoginClicked;
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
