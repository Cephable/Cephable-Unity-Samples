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
        // if we have a stored access token, we are already logged in
        var accessToken = PlayerPrefs.GetString("accessToken");
        if (!string.IsNullOrEmpty(accessToken))
        {
            // set the login button text to say sign out
            loginButton.GetComponentInChildren<Text>().text = "Sign Out";
        }
        else
        {
            loginButton.GetComponentInChildren<Text>().text = "Sign In";
        }
    }

    public void OnLoginButtonClick()
    {
        // if we have an access token, delete it and sign out
        var accessToken = PlayerPrefs.GetString("accessToken");
        if (!string.IsNullOrEmpty(accessToken))
        {
            oauth2Manager.Signout();
            //loginButton.GetComponentInChildren<Text>().text = "Sign In";
            return;
        }
        else
        {
            oauth2Manager.StartAuthorization();
        }

    }
}
