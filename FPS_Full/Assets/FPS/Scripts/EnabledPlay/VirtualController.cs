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
using Unity.FPS.Gameplay;

public class VirtualController : MonoBehaviour
{
    public PlayerInputHandler inputHandler;
    public string DeviceTypeId;
    public string DefaultDeviceName = "Unity-Sample";
    private string userDeviceId;
    private string userDeviceToken;
    private DeviceProfileConfiguration currentProfile;
    private HubConnection hubConnection;

    // Start is called before the first frame update
    public async Task Start()
    {
        userDeviceId = PlayerPrefs.GetString("userDeviceId");
        userDeviceToken = PlayerPrefs.GetString("userDeviceToken");

        if (!string.IsNullOrEmpty(userDeviceId) && !string.IsNullOrEmpty(userDeviceToken))
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
            .WithUrl("https://services.cephable.com/device", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(userDeviceToken);
                options.Headers.Add("X-Device-Token", userDeviceToken);
                options.Headers.Add("User-Agent", "Unity-Sample");
            })
            .Build();

            connection.On<string>("DeviceCommand", (command) =>
            {
                // TODO: simulate based on profile
                output("Received command: " + command);

                // temp
                if (command == "jump")
                {
                    inputHandler.isJumping = true;
                }
                if (command == "fire")
                {
                    inputHandler.isShooting = true;
                }
                if (command == "turn right")
                {
                    // TODO: spin some percentage to the right
                }
                StartCoroutine(ResetKeys());
            });
            connection.On<UserDevice>("DeviceProfileUpdate", (device) =>
            {
                output("Received profile update: " + device);
                currentProfile = device.currentProfile?.configuration;
            });
            output("Connecting to hub");

            await connection.StartAsync();
            // Connection started, indicate to hub that we are listening
            await connection.InvokeAsync("VerifySelf");
            output("Connected to hub");
        }
        catch (Exception ex)
        {

            output(ex.Message);
            output(ex.StackTrace);
        }


    }

    public IEnumerator ResetKeys()
    {
        yield return new WaitForSeconds(0.1f);
        inputHandler.isJumping = false;
        inputHandler.isShooting = false;
    }

    public IEnumerator CreateVirtualController()
    {
        var accessToken = PlayerPrefs.GetString("accessToken");
        output(accessToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            output("No access token found, please sign in");
            yield break;
        }
        var www = UnityWebRequest.Post($"https://services.cephable.com/api/Device/userDevices/new/{DeviceTypeId}?name={DefaultDeviceName}", string.Empty);
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
        if (string.IsNullOrEmpty(accessToken))
        {
            output("No access token found, please sign in");
            yield break;
        }

        var www = UnityWebRequest.Post($"https://services.cephable.com/api/Device/userDevices/{userDeviceId}/tokens", string.Empty);
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
    public UserDeviceProfile currentProfile;
    public static UserDevice CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<UserDevice>(jsonString);
    }
}

[System.Serializable]
public class UserDeviceProfile
{
    public string id;
    public string name;
    public string userId;
    public string profileType;
    public DeviceProfileConfiguration configuration;
    public string createdDate;
    public string modifiedDate;
}



[System.Serializable]
public class DeviceProfileConfiguration
{
    public string profileType;
    public List<KeyCommandMapping> commandKeyMappings;
    public List<MacroModel> macros;
    public List<HotkeyModel> hotkeys;
    public List<string> dictationCommands;
    public List<AudioEventModel> audioEvents;
}

[System.Serializable]
public class KeyCommandMapping
{
    public string key;
    public List<string> commands;
}

[System.Serializable]
public class MacroModel
{
    public string name;
    public List<string> commands;
    public List<MacroEvent> events;
}

[System.Serializable]
public class MacroEvent
{
    public string eventType;
    public List<string> keys;
    public int? holdTimeMilliseconds;
    public string typedPhrase;
    public int? mouseMoveX;
    public int? mouseMoveY;
    public int? mouseMoveZ;
    public int? joystickLeftMoveX;
    public int? joystickLeftMoveY;
    public int? joystickRightMoveX;
    public int? joystickRightMoveY;
    public string outputSpeech;
}

[System.Serializable]
public class HotkeyModel
{
    public string displayName;
    public string command;
}

[System.Serializable]
public class AudioEventModel
{
    public string name;
    public List<string> commands;
    public string audioFileUrl;
    public string outputSpeech;
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