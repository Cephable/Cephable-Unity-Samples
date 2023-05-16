using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

public class OAuth2Manager : MonoBehaviour
{
    public string AUTH_ENDPOINT = "https://services.enabledplay.com/signin";
    public string TOKEN_ENDPOINT = "https://services.enabledplay.com/signin/token";
    public string CLIENT_ID = "YOUR_CLIENT_ID";
    public string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
    private string REDIRECT_URI = "YOUR_REDIRECT_URI";

    private string authCode;
    private string accessToken;
    private string refreshToken;

    public void Authenticate()
    {
        REDIRECT_URI = string.Format("http://{0}:{1}/", IPAddress.Loopback, 51772);
        string authUrl = $"{AUTH_ENDPOINT}?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code";

        Application.OpenURL(authUrl);
    }

    public IEnumerator GetAccessToken(string authCode)
    {
        this.authCode = authCode;

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("grant_type", "authorization_code"));
        formData.Add(new MultipartFormDataSection("code", authCode));
        formData.Add(new MultipartFormDataSection("client_id", CLIENT_ID));
        formData.Add(new MultipartFormDataSection("client_secret", CLIENT_SECRET));
        formData.Add(new MultipartFormDataSection("redirect_uri", REDIRECT_URI));

        UnityWebRequest www = UnityWebRequest.Post(TOKEN_ENDPOINT, formData);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;

            // Parse the response to extract the access token and refresh token
            // ...
            
            // Store the tokens locally
            PlayerPrefs.SetString("accessToken", accessToken);
            PlayerPrefs.SetString("refreshToken", refreshToken);
        }
        else
        {
            Debug.Log($"Failed to get access token: {www.error}");
        }
    }

    public IEnumerator RefreshAccessToken()
    {
        string refreshToken = PlayerPrefs.GetString("refreshToken");

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("grant_type", "refresh_token"));
        formData.Add(new MultipartFormDataSection("refresh_token", refreshToken));
        formData.Add(new MultipartFormDataSection("client_id", CLIENT_ID));
        formData.Add(new MultipartFormDataSection("client_secret", CLIENT_SECRET));

        UnityWebRequest www = UnityWebRequest.Post(TOKEN_ENDPOINT, formData);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;

            // Parse the response to extract the new access token and refresh token
            // ...

            // Store the tokens locally
            PlayerPrefs.SetString("accessToken", accessToken);
            PlayerPrefs.SetString("refreshToken", refreshToken);
        }
        else
        {
            Debug.Log($"Failed to refresh access token: {www.error}");
        }
    }

    public string GetAccessToken()
    {
        return PlayerPrefs.GetString("accessToken");
    }
    public void StartAuthorization()
    {
        Authenticate();
    }
}
