using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LoginController : MonoBehaviour {
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

  private AsyncOperation preloadOperation;

  private void Awake() {
    ui = GetComponent<UIDocument>().rootVisualElement;
  }

  private void OnEnable() {
    logger.Log("LoginController enabled.", this);

    _loginButton = ui.Q<Button>("LoginButton");
    _loginButton.clicked += OnLoginClicked;

    _emailField = ui.Q<TextField>("EmailField");
    _passwordField = ui.Q<TextField>("PasswordField");
  }

  private void OnDisable() {
    if (_loginButton != null)
      _loginButton.clicked -= OnLoginClicked;
  }

  private void OnLoginClicked() {
    logger.Log("Login Button Clicked", this);
    logger.Log("Email: " + _emailField.value, this);
    logger.Log("Password: " + _passwordField.value, this);

    StartCoroutine(authService.Login(_emailField.value, _passwordField.value, (token) => {
      if (!string.IsNullOrEmpty(token)) {
        logger.Log("Login successful, token: " + token, this);
        session.SetAPIToken(token);
        logger.Log($"Navigating to {session.APIToken}", this);
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
