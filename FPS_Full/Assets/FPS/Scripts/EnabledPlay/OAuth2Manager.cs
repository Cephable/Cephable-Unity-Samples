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
    public VirtualController controller;
    private string REDIRECT_URI = "YOUR_REDIRECT_URI";

    private string authCode;
    private string accessToken;
    private string refreshToken;

    private void Start()
    {
        // check if we have an access token stored locally
        accessToken = PlayerPrefs.GetString("accessToken");
        if (!string.IsNullOrEmpty(accessToken))
        {
            // if do go refresh the token
            StartCoroutine(RefreshAccessToken());
        }
    }

    public async void Authenticate()
    {
        REDIRECT_URI = string.Format("http://{0}:{1}/", IPAddress.Loopback, 51772);
        string authUrl = $"{AUTH_ENDPOINT}?client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}&response_type=code";

        Application.OpenURL(authUrl);

        // Creates an HttpListener to listen for requests on that redirect URI.
        var http = new HttpListener();
        http.Prefixes.Add(REDIRECT_URI);
        output("Listening..");
        http.Start();

      
        // Waits for the OAuth authorization response.
        var context = await http.GetContextAsync();


        // Sends an HTTP response to the browser.
        var response = context.Response;
        string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://enabledplay.com'></head><body>Please return to the app.</body></html>");
        var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        var responseOutput = response.OutputStream;
        Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
        {
            responseOutput.Close();
            http.Stop();
            Console.WriteLine("HTTP server stopped.");
        });

        // Checks for errors.
        if (context.Request.QueryString.Get("error") != null)
        {
            output(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
            return;
        }
        if (context.Request.QueryString.Get("code") == null)
        {
            output("Malformed authorization response. " + context.Request.QueryString);
            return;
        }

        // extracts the code
        var code = context.Request.QueryString.Get("code");
        //var incoming_state = context.Request.QueryString.Get("state");

        // Compares the receieved state to the expected value, to ensure that
        // this app made the request which resulted in authorization.
        // if (incoming_state != state)
        // {
        //     output(String.Format("Received request with invalid state ({0})", incoming_state));
        //     return;
        // }
        output("Authorization code: " + code);

        StartCoroutine(GetAccessToken(code));

    }
    /// <summary>
    /// Appends the given string to the on-screen log, and the debug console.
    /// </summary>
    /// <param name="output">string to be appended</param>
    public void output(string output)
    {
        Console.WriteLine(output);
        Debug.Log(output);
    }
    public IEnumerator GetAccessToken(string authCode)
    {
        output("Getting access token...");
        this.authCode = authCode;

        UnityWebRequest www = UnityWebRequest.Post($"{TOKEN_ENDPOINT}?grant_type=code&code={authCode}&client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}", string.Empty);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;
            output(responseText);
            // Parse the response to extract the access token and refresh token
            // ...
            var tokenResponse = TokenResponse.CreateFromJSON(responseText);
            // // Store the tokens locally
            PlayerPrefs.SetString("accessToken", tokenResponse.access_token);
            PlayerPrefs.SetString("refreshToken", tokenResponse.refresh_token);

            StartCoroutine(controller.CreateVirtualController());
        }
        else
        {
            output($"Failed to get access token: {www.error}");
        }
    }

    public IEnumerator RefreshAccessToken()
    {
        string refreshToken = PlayerPrefs.GetString("refreshToken");

        UnityWebRequest www = UnityWebRequest.Post($"{TOKEN_ENDPOINT}?grant_type=refresh_token&client_secret={CLIENT_SECRET}&refresh_token={refreshToken}&client_id={CLIENT_ID}&redirect_uri={REDIRECT_URI}", string.Empty);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;

            // Parse the response to extract the new access token and refresh token
            // ...
            var tokenResponse = TokenResponse.CreateFromJSON(responseText);
            // // Store the tokens locally
            PlayerPrefs.SetString("accessToken", tokenResponse.access_token);
            PlayerPrefs.SetString("refreshToken", tokenResponse.refresh_token);

        }
        else
        {
            output($"Failed to refresh access token: {www.error}");
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

    public void Signout()
    {
        // delete access and refresh token and reset local state
        PlayerPrefs.DeleteKey("accessToken");
        PlayerPrefs.DeleteKey("refreshToken");
        accessToken = null;
        refreshToken = null;
    }
}

[System.Serializable]
public class TokenResponse
{
    public string access_token;
    public string refresh_token;
    public string access_token_expiration;
    public string refresh_token_expiration;

    public static TokenResponse CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<TokenResponse>(jsonString);
    }
}