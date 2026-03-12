using System;
using UnityEngine;

/// <summary>
/// Manages the authentication flow within a single scene.
/// Instantiates the three panels (Login, SignUp, VerifyCode), connects them via
/// events, and exposes OnAuthComplete when the user is authenticated.
/// </summary>
public class AuthFlowManager
{
    public event Action OnAuthComplete;

    private readonly GameObject loginPanel;
    private readonly GameObject signUpPanel;
    private readonly GameObject verifyCodePanel;

    public AuthFlowManager(
        GameObject loginPanel,
        GameObject signUpPanel,
        GameObject verifyCodePanel
    )
    {
        this.loginPanel = loginPanel;
        this.signUpPanel = signUpPanel;
        this.verifyCodePanel = verifyCodePanel;
    }

    /// <summary>
    /// Shows only the login panel and connects the navigation events between panels.
    /// </summary>
    public void Initialize()
    {
        signUpPanel.SetActive(false);
        verifyCodePanel.SetActive(false);
        loginPanel.SetActive(true);

        SetupLoginController();
        SetupSignUpController();
        SetupVerifyCodeController();
    }

    private void SetupLoginController()
    {
        var ctrl = loginPanel.GetComponent<LoginController>();
        if (ctrl == null)
        {
            Debug.LogWarning("LoginController not found on loginPanel.");
            return;
        }

        ctrl.OnNavigateToSignUp += () =>
        {
            loginPanel.SetActive(false);
            signUpPanel.SetActive(true);
        };

        ctrl.OnNavigateToVerifyCode += () =>
        {
            loginPanel.SetActive(false);
            verifyCodePanel.SetActive(true);
        };

        ctrl.OnLoginSuccess += () =>
        {
            loginPanel.SetActive(false);
            OnAuthComplete?.Invoke();
        };
    }

    private void SetupSignUpController()
    {
        var ctrl = signUpPanel.GetComponent<SignUpController>();
        if (ctrl == null)
        {
            Debug.LogWarning("SignUpController not found on signUpPanel.");
            return;
        }

        ctrl.OnNavigateToLogin += () =>
        {
            signUpPanel.SetActive(false);
            loginPanel.SetActive(true);
        };

        ctrl.OnSignUpSuccess += () =>
        {
            signUpPanel.SetActive(false);
            verifyCodePanel.SetActive(true);
        };
    }

    private void SetupVerifyCodeController()
    {
        var ctrl = verifyCodePanel.GetComponent<VerifyCodeController>();
        if (ctrl == null)
        {
            Debug.LogWarning("VerifyCodeController not found on verifyCodePanel.");
            return;
        }

        ctrl.OnNavigateBack += () =>
        {
            verifyCodePanel.SetActive(false);
            loginPanel.SetActive(true);
        };

        ctrl.OnVerifySuccess += () =>
        {
            verifyCodePanel.SetActive(false);
            OnAuthComplete?.Invoke();
        };
    }

    public void Destroy()
    {
        if (loginPanel != null)
            UnityEngine.Object.Destroy(loginPanel);
        if (signUpPanel != null)
            UnityEngine.Object.Destroy(signUpPanel);
        if (verifyCodePanel != null)
            UnityEngine.Object.Destroy(verifyCodePanel);
    }
}
