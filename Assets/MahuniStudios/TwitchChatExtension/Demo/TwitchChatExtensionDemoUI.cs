// © Copyright 2026 Mahuni Game Studios

using System.Collections.Generic;
using Mahuni.Twitch.Extension;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called for the first time.
    /// </summary>
    private void Start()
    {
        // This way you receive chat messages instantly, even when application is not in focus
        Application.runInBackground = true;
        
        TwitchAuthentication.OnAuthenticated += OnAuthenticated;
        TwitchAuthentication.Reset();  // If you authenticated before, it might be better to reset the token, to be sure the right permissions are set
        
        TwitchChatConnection.OnConnectionReady += OnChatConnectionReady;
        TwitchChatConnection.OnConnected += OnChatConnected;
        TwitchChatConnection.OnClientJoinedChat += OnClientJoinedChat;
        TwitchChatConnection.OnChatMessageReceived += OnChatMessageReceived;
        
        channelNameText.onValueChanged.AddListener(ValidateAuthenticationFields);
        twitchClientIdText.onValueChanged.AddListener(ValidateAuthenticationFields);
        authenticateButton.interactable = false;
        authenticateButton.onClick.AddListener(OnAuthenticationButtonClicked);
        ValidateAuthenticationFields();
        
        chatText.text = string.Empty;
        sendMessageButton.interactable = false;
        sendMessageButton.onClick.AddListener(OnSendMessageButtonClicked);
        chatMessageText.interactable = false;
        chatMessageText.onValueChanged.AddListener(OnChatMessageChanged);
        chatMessageText.onSubmit.AddListener(OnChatMessageSubmitted);
    }

    #region Authentication

    /// <summary>
    /// Validate if authentication can be started by checking the required input fields
    /// </summary>
    private void ValidateAuthenticationFields(string value = "")
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

    /// <summary>
    /// The chat connection class returned if it is ready to connect
    /// </summary>
    /// <param name="success">True if the connection can be established, false if it cannot</param>
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
    
    /// <summary>
    /// The chat connection attempt returned
    /// </summary>
    /// <param name="success">True if chat connection was successful, false it if failed</param>
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
    
    /// <summary>
    /// A user connected to the chat
    /// </summary>
    /// <param name="username">The name of the user that connected</param>
    private void OnClientJoinedChat(string username)
    {
        string message = $"<i><color=\"purple\">--> Client '{username}' joined the chat! <-- </color></i>";
        Debug.Log(message);
        chatText.text += "\n" + message;
    }

    /// <summary>
    /// A new chat message was received
    /// </summary>
    /// <param name="user">The user that sent the message</param>
    /// <param name="message">The message that was sent</param>
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
    
    /// <summary>
    /// The chat message was changed in the demo scene
    /// </summary>
    /// <param name="message">The new message content</param>
    private void OnChatMessageChanged(string message)
    {
        sendMessageButton.interactable = !string.IsNullOrEmpty(chatMessageText.text);
    }

    /// <summary>
    /// The chat message was submitted (e.g. by hitting Enter key)
    /// </summary>
    /// <param name="message">The message content</param>
    private void OnChatMessageSubmitted(string message)
    {
        OnSendMessageButtonClicked();
    }
    
    /// <summary>
    /// The send chat message button was clicked
    /// </summary>
    private void OnSendMessageButtonClicked()
    {
        TwitchChatConnection.Write(chatMessageText.text);
        chatMessageText.text = string.Empty;
    }
    
    #endregion

    #region Helpers

    /// <summary>
    /// Update is called every frame if the MonoBehaviour is enabled
    /// </summary>
    private void Update()
    {
        // Tab through formular
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (EventSystem.current.currentSelectedGameObject == null || EventSystem.current.currentSelectedGameObject == chatMessageText.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(channelNameText.gameObject, new BaseEventData(EventSystem.current));
            }
            else if (EventSystem.current.currentSelectedGameObject == channelNameText.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(twitchClientIdText.gameObject, new BaseEventData(EventSystem.current));
            }
            else if (EventSystem.current.currentSelectedGameObject == twitchClientIdText.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(chatMessageText.interactable ? chatMessageText.gameObject : channelNameText.gameObject, new BaseEventData(EventSystem.current));
            }
        }
    }

    #endregion
}
