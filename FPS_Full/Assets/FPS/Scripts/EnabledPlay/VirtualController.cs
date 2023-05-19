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
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.AspNetCore.SignalR.Client;

public class VirtualController : MonoBehaviour
{
    public GameObject player;
    public string DeviceTypeId;
    public string DefaultDeviceName = "Unity-Sample";
    private string userDeviceId;
    private string userDeviceToken;
    private HubConnection hubConnection;

    // Start is called before the first frame update
    public async Task Start()
    {
        userDeviceId = PlayerPrefs.GetString("userDeviceId");
        userDeviceToken = PlayerPrefs.GetString("userDeviceToken");

        if(!string.IsNullOrEmpty(userDeviceId) && !string.IsNullOrEmpty(userDeviceToken))
        {
            output($"User device id: {userDeviceId} with token: {userDeviceToken}");
            await ConnectToHub();
        }
        else
        {
            StartCoroutine(CreateVirtualController());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async Task ConnectToHub()
    {
        try
        {
            var connection = new HubConnectionBuilder()
            .WithUrl("https://services.enabledplay.com/device", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(userDeviceToken);
                options.Headers.Add("X-Device-Token", userDeviceToken);
                options.Headers.Add("User-Agent", "Unity-Sample");
            })
            .Build();

            connection.On<string>("DeviceCommand", (command) =>
            {
                output("Received command: " + command);
            });
            output("Connecting to hub");

            await connection.StartAsync();
            // Connection started, indicate to hub that we are listening
            await connection.InvokeAsync("VerifySelf");
            output("Connected to hub");
        } 
        catch(Exception ex)
        {
            
            output(ex.Message);
            output(ex.StackTrace);
        }
      

    }

    public IEnumerator CreateVirtualController()
    {
        var accessToken = PlayerPrefs.GetString("accessToken");
        output(accessToken);
        if(string.IsNullOrEmpty(accessToken))
        {
            output("No access token found, please sign in");
            yield break;
        }
        var www = UnityWebRequest.Post($"https://services.enabledplay.com/api/Device/userDevices/new/{DeviceTypeId}?name={DefaultDeviceName}",  string.Empty);
        // set the Authorization header
        www.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;
            // Parse the response to extract the access token and refresh token
            // ...
            var deviceResponse = UserDevice.CreateFromJSON(responseText);
  
            PlayerPrefs.SetString("userDeviceId", deviceResponse.id);
            userDeviceId = deviceResponse.id;
            StartCoroutine(CreateDeviceToken());
        }
        else
        {
            string responseText = www.downloadHandler.text;
            output($"Failed to create device: {www.error} {responseText}");
        }
    }

    public IEnumerator CreateDeviceToken()
    {
        var accessToken = PlayerPrefs.GetString("accessToken");
        if(string.IsNullOrEmpty(accessToken))
        {
            output("No access token found, please sign in");
            yield break;
        }

        var www = UnityWebRequest.Post($"https://services.enabledplay.com/api/Device/userDevices/{userDeviceId}/tokens",  string.Empty);
        www.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseText = www.downloadHandler.text;
            // Parse the response to extract the access token and refresh token
            // ...
            var tokenResponse = UserDeviceToken.CreateFromJSON(responseText);
            // // Store the tokens locally
            PlayerPrefs.SetString("userDeviceToken", tokenResponse.token);
            userDeviceToken = tokenResponse.token;
        }
        else
        {
            string responseText = www.downloadHandler.text;
            output($"Failed to create device token: {www.error} {responseText}");
        }
    }
    
    public void output(string output)
    {
        Console.WriteLine(output);
        Debug.Log(output);
    }
}

[System.Serializable]
public class UserDevice
{
    public string id;
    public string nameOverride;
    public bool isVerified;
    public bool isConnected;
    public bool isListening;
    public bool isAutoListen;
    public bool isOptimisticModel;
    public static UserDevice CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<UserDevice>(jsonString);
    }
}

[System.Serializable]
public class UserDeviceToken
{
    public string id;
    public string userDeviceId;
    public string token;
    public bool isDisabled;
    public static UserDeviceToken CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<UserDeviceToken>(jsonString);
    }

}