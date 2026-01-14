// © Copyright 2026 Mahuni Game Studios

using System.Collections.Generic;
using Mahuni.Twitch.Extension;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TwitchChatExtensionDemoUI : MonoBehaviour
{
    [Header("Authentication")] 
    public TMP_InputField channelNameText;
    public TMP_InputField twitchClientIdText;
    public Button authenticateButton;
    
    [Header("Chat connection")] 
    public TextMeshProUGUI chatText;
    public TMP_InputField chatMessageText;
    public Button sendMessageButton;
    
    private void Start()
    {
        // This way you receive chat message instantly, even when application is not in focus
        Application.runInBackground = true;
        
        TwitchAuthentication.OnAuthenticated += OnAuthenticated;
        TwitchAuthentication.Reset();  // If you authenticated before, it might be better to reset the token, to be sure the right permissions are set
        
        channelNameText.onValueChanged.AddListener(ValidateAuthenticationFields);
        twitchClientIdText.onValueChanged.AddListener(ValidateAuthenticationFields);
        authenticateButton.interactable = false;
        authenticateButton.onClick.AddListener(OnAuthenticationButtonClicked);
        ValidateAuthenticationFields();
        
        TwitchChatConnection.OnConnectionReady += OnChatConnectionReady;
        TwitchChatConnection.OnConnected += OnChatConnected;
        TwitchChatConnection.OnClientJoinedChat += OnClientJoinedChat;
        TwitchChatConnection.OnChatMessageReceived += OnChatMessageReceived;
        
        chatText.text = string.Empty;
        sendMessageButton.interactable = false;
        sendMessageButton.onClick.AddListener(OnSendMessageButtonClicked);
        chatMessageText.interactable = false;
        chatMessageText.onValueChanged.AddListener(OnChatMessageChanged);
    }

    #region Authentication

    public void ValidateAuthenticationFields(string value = "")
    {
        authenticateButton.interactable = !string.IsNullOrEmpty(channelNameText.text) && !string.IsNullOrEmpty(twitchClientIdText.text);
    }

    /// <summary>
    /// The authentication button was clicked by the user
    /// </summary>
    private void OnAuthenticationButtonClicked()
    {
        TwitchAuthentication.ConnectionInformation infos = new(twitchClientIdText.text, new List<string>{TwitchAuthentication.ConnectionInformation.CHAT_READ, TwitchAuthentication.ConnectionInformation.CHAT_EDIT});
        TwitchAuthentication.StartAuthenticationValidation(this, infos);
    }
    
    /// <summary>
    /// The authentication returned with a result
    /// </summary>
    /// <param name="success">True if authentication was successful</param>
    private void OnAuthenticated(bool success)
    {
        if (!success)
        {
            Debug.LogError("<color=\"red\">Could not authenticate to Twitch!");
            return;
        }
        
        Debug.Log("<color=\"green\">Authenticated!");
        authenticateButton.interactable = false;
        TwitchChatConnection.Init(channelNameText.text);
    }

    #endregion

    #region Connect & Read Chat

    private void OnChatConnectionReady(bool success)
    {
        if (!success)
        {
            Debug.LogError("<color=\"red\">Could not connect to Twitch chat!");
            return;
        }
        
        Debug.Log("<color=\"green\">Chat connection ready!");
        StartCoroutine(TwitchChatConnection.ConnectChat());
    }
    
    private void OnChatConnected(bool success)
    {
        if (!success)
        {
            Debug.LogError("<color=\"red\">Could not connect to Twitch chat!");
            return;
        }
        
        Debug.Log("<color=\"green\">Chat connected!");
        chatMessageText.interactable = true;
    }
    
    private void OnClientJoinedChat(string username)
    {
        string message = $"<i><color=\"purple\">--> Client '{username}' joined the chat! <-- </color></i>";
        Debug.Log(message);
        chatText.text += "\n" + message;
    }

    private void OnChatMessageReceived(TwitchChatConnection.ChatUser user, string message)
    {
        string userName = user.displayname;
        
        // Set username color if they set it to something specific
        if (!string.IsNullOrEmpty(user.color))
        {
            userName = "<color=" + user.color + ">" + userName + "</color>";
        }
        
        message = $"{userName}: {message}";
        Debug.Log($"{message}");
        
        chatText.text += "\n" + message; 
    }
    
    #endregion

    #region Write to Chat
    
    private void OnChatMessageChanged(string message)
    {
        sendMessageButton.interactable = !string.IsNullOrEmpty(chatMessageText.text);
    }
    
    private void OnSendMessageButtonClicked()
    {
        TwitchChatConnection.Write(chatMessageText.text);
        chatMessageText.text = string.Empty;
    }
    
    #endregion
}
