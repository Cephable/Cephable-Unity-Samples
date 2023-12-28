# Cephable-Unity-Samples

Sample projects for integrating Cephable with Unity. This allows users of Cephable's adaptive and accessibility control to play your games and apps in ways that work best for them with things like:

- Adaptive voice controls
- Head movement
- Face expressions
- Virtual buttons
- more!

# Overview and Getting Started

To use this demo on your own, you'll need a licensed setup of Cephable controls to receive a:

- OAuth Client ID
- OAuth Client Secret
- Device Type ID

Don't have one yet? [Get in touch - Contact us!](https://www.cephable.com/contact)

These demo projects showcase the ability to integrate Cephable controls from the Cephable app into Unity apps and games. There are a couple key components to this integration:

- Account Linking menu for users
- OAuth implementation with Cephable OAuth to get user access tokens
- Generating a Cephable Virtual User Device for the user
- Generating a Cephable Device Token for the device
- Connecting to the Cephable device hub with SignalR

In the `MainScene` heirarchy, there are a few key objects:

- CephableAuth -> LoginButton:
- Player
  ![Heirarchy pane](docs/unity-heirarchy.png)

In the `LoginButton` is where you'll see settings for OAuth with Cephable to get things started:

![LoginButton Settings](docs/unity-auth.png)

And on the `Player` is where we add the virtual controller tie-in:

![Player Settings](docs/unity-controller.png)

With these configured, the user can run the game, hit `tab` to open the settings menu, and see the sign-in / account linking process:

![Settings pane](docs/unity-game-settings.png)

Clicking the `Link Account` button will open the Cephable OAuth flow in a browser window. Once the user has logged in or made an account and authorized the app, they'll be redirected back to the game and the Cephable device will be created for them:
![Cephable authentication](docs/app-auth.png)

The user can then use the Cephable app on Mac, PC, Android, or iOS and will see the generated virtual device to send commands too. They can then add "fire" and "jump" commands to a control profile or use [this share link for the existing demo.](https://share.cephable.com/profileshare/copy/YzJjNGMwZWYtNzY5MS00MzM2LWE2ZGEtNmFmODgyNGUwOWZiLWQxMDRjYzc5LWJlMDQtNDcyNi1iYmVkLTM5OWRkMzUxZTI1Mw)

![Cephable device](docs/app-device.jpg)

Then in the app, they can send those commands from virtual buttons, voice controls, or camera controls. For example:
![Cephable buttons](docs/app-buttons.jpg)

# Concepts not yet included in this demo

- Generating a Cephable Control Profile Automatically or showing a link/qr code for an existing profile to add
- Deep integration of Cephable models into game/app
- Keyboard/mouse automated shortcuts (infers static inputs)
- Customizing inputs in the Cephable app (infers static inputs)

## Static input setup

This demo uses only static inputs which means that the app will receive the raw commands and the app decides how to handle specific values. This is done in the `VirtualController.cs` script:

```csharp
 connection.On<string>("DeviceCommand", (command) =>
{
    output("Received command: " + command);

    if (command == "jump" || command == "hotkey_jump" || command == "eyebrows_raised")
    {
        inputHandler.isJumping = true;
    }

    // add other commands and logic you want to handle
    StartCoroutine(ResetKeys());
});

```
