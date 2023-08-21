using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginButton : MonoBehaviour
{
    public OAuth2Manager oauth2Manager;
    public Button loginButton;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    private void OnLoginButtonClick()
    {
        oauth2Manager.StartAuthorization();
    }
}
