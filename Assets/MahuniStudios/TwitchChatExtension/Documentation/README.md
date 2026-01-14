# Unity Twitch IRC Chat Extension by Mahuni Game Studios

[![Downloads](https://img.shields.io/github/downloads/mahuni-game-studios/twitch-irc-chat-unity-extension/total.svg)](https://github.com/mahuni-game-studios/twitch-irc-chat-unity-extension/releases/)
[![Latest Version](https://img.shields.io/github/v/release/mahuni-game-studios/twitch-irc-chat-unity-extension)](https://github.com/mahuni-game-studios/twitch-irc-chat-unity-extension/releases/tag/v1.0)

A Unity extension to read and write into the Twitch chat using IRC protocol.

## Code Snippet Examples

The simplest implementation to give your application permission to use the Twitch API and finally connect, read and write to chat!

### Authentication

The authentication logic is packed in a git submodule and is needed if you want to use the extension as is. Read [this](#twitch-authentication-extension) to find out how to get it.

```cs
public class YourUnityClass : MonoBehaviour
{
    private void Start()
    {        
        // Register to authentication finished event
        TwitchAuthentication.OnAuthenticated += OnAuthenticated;
        
        // Set relevant information to the connection
        TwitchAuthentication.ConnectionInformation infos = new("your-client-id", new List<string>(){TwitchWebRequestAuthentication.ConnectionInformation.CHAT_READ, TwitchWebRequestAuthentication.ConnectionInformation.CHAT_EDIT});
        
        // Start authentication
        TwitchAuthentication.StartAuthenticationValidation(this, infos);
    }
    
    // Authentication has finished
    private void OnAuthenticated(bool success)
    {
        if (success)
        {
            // TODO: Start using the extension from here!
        }
    }
}
```

### Connect to chat

Please note that connecting to the chat only works after successful authentication.

```cs
public class YourUnityClass : MonoBehaviour
{   
    private void Start()
    {
        // Register to chat events
        TwitchChatConnection.OnConnectionReady += OnChatConnectionReady;
        TwitchChatConnection.OnConnected += OnChatConnected;        
        TwitchChatConnection.OnChatMessageReceived += OnChatMessageReceived;
        
        // Initialize the connection
        TwitchChatConnection.Init("channel-name");
    }
      
    private void OnChatConnectionReady(bool success)
    {
        if (success)
        {
           // Wait for chat to be connected 
           StartCoroutine(TwitchChatConnection.ConnectChat());
        }
    }
    
    private void OnChatConnected(bool success)
    {
        if (success)
        {
            // TODO: Now you can start to read from or write to chat!
        }
    }
}
```

### Read chat

Please note that reading the chat only works after successful authentication and chat connection.

```cs
public class YourUnityClass : MonoBehaviour
{   
    private void Start()
    {
        // Register to chat message received event       
        TwitchChatConnection.OnChatMessageReceived += OnChatMessageReceived;
    }
      
    private void OnChatMessageReceived(TwitchChatConnection.ChatUser user, string message)
    {
        // TODO: Use the chat message and user information as you like
    }
}
```

### Write to chat

Please note that writing to the chat only works after successful authentication and chat connection.

```cs
public class YourUnityClass : MonoBehaviour
{   
    private void Start()
    {
        TwitchChatConnection.Write("Hello world! <3");
    }
}
```

## Installation Guide

### Prerequisites

To be able to interact with the Twitch API, you need to register your Twitch application. You can follow how to do that with this [Guide from Twitch](https://dev.twitch.tv/docs/authentication/register-app/). In short:

1. Sign up to Twitch if you don't have already
2. Navigate to the [Twitch Developer Console](https://dev.twitch.tv/console/apps)
3. Create a new application and select an appropriate category, e.g. as Game Integration
4. Click on *Manage* on your application entry and you will be presented a `Client ID`. This ID will be needed to interact with Twitch.

<font color="red">The `Client ID` should stay secret, do not share or show it!</font>

#### Twitch Authentication Extension

This repository uses the [Unity Twitch Authentication Extension by Mahuni Game Studios](https://github.com/mahuni-game-studios/twitch-authentication-unity-extension) as git submodule. Be sure to either pull the submodule or grab / download / clone the authentication extension manually.

- To clone the repository with submodules: `git clone --recurse-submodules`
- To update the cloned repository to get the submodules: `git submodule update --init --recursive`
- To download the extension, go to [GitHub](https://github.com/mahuni-game-studios/twitch-authentication-unity-extension), download it and drag and drop it somewhere into the `Assets/` folder

#### Demo scene

To use the provided `TwitchChatExtension_Demo` scene, the `TextMeshPro` package is required. If you do not have it yet imported into your project, simply opening the `TwitchChatExtension_Demo.scene` will ask if you want to import it. Select the `Import TMP Essentials` option, close the `TMP Importer` and you are good to go.

### Setup Unity project
1. Either open this project directly or import it to your own project in the Unity Editor.
2. Make sure the git submodules are installed, see [here](#twitch-authentication-extension)
3. Start using the `TwitchChatConnection` script right away, or take a look into the `TwitchChatExtension_Demo` scene to find an easy example implementation.