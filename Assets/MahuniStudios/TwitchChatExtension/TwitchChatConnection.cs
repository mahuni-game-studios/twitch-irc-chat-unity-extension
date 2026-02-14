// © Copyright 2026 Mahuni Game Studios

namespace Mahuni.Twitch.Extension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using UnityEngine;

    /// <summary>
    /// Connect to Twitch chat to read and write messages using IRC protocol
    /// More infos: https://dev.twitch.tv/docs/chat/irc#connecting-to-the-twitch-irc-server
    /// </summary>
    public static class TwitchChatConnection
    {
        public static event Action<bool> OnConnectionReady;
        public static event Action<bool> OnConnected;
        public static event Action<string> OnClientJoinedChat;
        public static event Action<ChatUser, string> OnChatMessageReceived;

        private static TcpClient tcpClient;
        private static StreamReader chatStreamReader;
        private static StreamWriter chatStreamWriter;
        private static string channelName;
        private static ChatUser currentUser; // This is us when writing into chat

        private const string TCP_CLIENT_HOST = "irc.chat.twitch.tv";
        private const string MESSAGE_CONNECT_SUCCESS = "Welcome, GLHF!";
        private const string MESSAGE_LOGIN_FAILED = "Login authentication failed";
        private const string MESSAGE_INVALID_FORMAT = "Improperly formatted auth";
        private const string USER_MESSAGE_CODE = "PRIVMSG";
        private const string USER_JOIN_CODE = "JOIN";
        private const string USER_STATE_CODE = "USERSTATE";
        private const string SERVER_PING_MESSAGE = "PING :tmi.twitch.tv";
        private const string CLIENT_PONG_MESSAGE = "PONG :tmi.twitch.tv";

        #region Initialization

        /// <summary>
        /// Initialize the connection
        /// </summary>
        /// <param name="channel">The name of the channel to connect the chat from</param>
        public static void Init(string channel)
        {
            channelName = channel.ToLower();
            if (!TwitchAuthentication.IsAuthenticated())
            {
                TwitchAuthentication.OnAuthenticated += OnAuthenticated;
            }
            else
            {
                OnAuthenticated(true);
            }
        }

        /// <summary>
        /// Authentication finished
        /// </summary>
        /// <param name="success">True if authentication was successful, false if it failed</param>
        private static void OnAuthenticated(bool success)
        {
            if (!success)
            {
                OnConnectionReady?.Invoke(false);
                Debug.LogError("Cannot connect chat when authentication failed.");
                return;
            }

            if (!TwitchAuthentication.Connection.permissionScope.Contains(TwitchAuthentication.ConnectionInformation.CHAT_READ) ||
                !TwitchAuthentication.Connection.permissionScope.Contains(TwitchAuthentication.ConnectionInformation.CHAT_EDIT))
            {
                OnConnectionReady?.Invoke(false);
                Debug.LogError("Cannot connect chat when authentication scope does not contain permission to read and edit chat.");
                return;
            }

            if (string.IsNullOrEmpty(channelName))
            {
                OnConnectionReady?.Invoke(false);
                Debug.LogError("Cannot connect chat channel name is empty.");
                return;
            }

            OnConnectionReady?.Invoke(true);
        }

        /// <summary>
        /// Coroutine to try to connect to chat and notify on the connection success / failure
        /// </summary>
        public static IEnumerator ConnectChat()
        {
            tcpClient = new TcpClient(TCP_CLIENT_HOST, 6667);

            chatStreamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };
            chatStreamWriter.WriteLine("PASS oauth:" + TwitchAuthentication.GetToken());
            chatStreamWriter.WriteLine("NICK " + channelName);
            chatStreamWriter.WriteLine("CAP REQ :twitch.tv/commands twitch.tv/tags twitch.tv/membership"); // This asks to get the user capabilities
            chatStreamWriter.WriteLine($"{USER_JOIN_CODE} #{channelName}");

            while (!tcpClient.Connected) yield return null;

            chatStreamReader = new StreamReader(tcpClient.GetStream());
            string answer = chatStreamReader.ReadLine();
            if (string.IsNullOrEmpty(answer))
            {
                Debug.LogError("Received an empty response when trying to connect to the chat.");
                OnConnected?.Invoke(false);
                yield break;
            }

            if (answer.Contains(MESSAGE_LOGIN_FAILED))
            {
                Debug.LogError("Chat connection failed: " + MESSAGE_LOGIN_FAILED);
                OnConnected?.Invoke(false);
            }
            else if (answer.Contains(MESSAGE_INVALID_FORMAT))
            {
                Debug.LogError("Chat connection failed: " + MESSAGE_INVALID_FORMAT);
                OnConnected?.Invoke(false);
            }
            else if (answer.Contains(MESSAGE_CONNECT_SUCCESS))
            {
                OnConnected?.Invoke(true);
                yield return ReadChat();
            }
            else
            {
                Debug.LogError($"Unexpected message received when connecting to Twitch chat: {answer}");
                OnConnected?.Invoke(false);
            }
        }

        #endregion

        #region Keep Alive

        /// <summary>
        /// Ping was received, so we respond with a pong to keep the chat connection alive
        /// </summary>
        private static void OnPingReceived()
        {
            Debug.Log("Ping received. Sending pong...");
            chatStreamWriter.WriteLine(CLIENT_PONG_MESSAGE);
        }

        #endregion

        #region Read Chat

        /// <summary>
        /// Coroutine to keep reading the chat every frame
        /// </summary>
        private static IEnumerator ReadChat()
        {
            while (tcpClient != null)
            {
                yield return null;
                if (tcpClient.Available <= 0) continue;
                Read();
            }
        }

        /// <summary>
        /// Read through the latest chat messages and forward to next methods according to the message type
        /// </summary>
        private static void Read()
        {
            string message = chatStreamReader.ReadLine();

            if (string.IsNullOrEmpty(message)) return;

            // You can enable this log for debugging
            // Debug.Log($"Raw message: '{message}'");

            if (message.Contains($"{USER_MESSAGE_CODE} #{channelName}")) OnMessageReceived(message);
            else if (message.Contains($"{USER_JOIN_CODE} #{channelName}")) OnJoinedToChat(message);
            else if (message.Equals(SERVER_PING_MESSAGE)) OnPingReceived();
            else if (message.Contains($"{USER_STATE_CODE} #{channelName}")) OnUserInfoUpdated(message);

            // If there are more chat messages to read, continue to call itself until all messages are processed
            if (chatStreamReader.Peek() > 0) Read();
        }

        /// <summary>
        /// A message event was received
        /// </summary>
        /// <param name="rawMessage">The raw message that was received</param>
        private static void OnMessageReceived(string rawMessage)
        {
            // Split the incoming data between tags and actual message
            string[] split = rawMessage.Split("#" + channelName + " :", 2);
            string messageContent = split[1];

            // Remove chars that are not letters or digits, especially important for control characters
            // todo: careful, messages tagging other users with @ will still break the parsing!
            messageContent = new string(messageContent.Where(c => char.IsLetterOrDigit(c) || (c >= ' ' && c <= byte.MaxValue)).ToArray());

            // Try to parse the message as JSON to receive the ChatUser information
            try
            {
                ChatUser twitchUser = JsonUtility.FromJson<ChatUser>(ConvertMessageToChatUser(rawMessage));
                OnChatMessageReceived?.Invoke(twitchUser, messageContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while grabbing chat user from JSON.\nMessage: '{rawMessage}'\nException: {ex}");
            }
        }

        /// <summary>
        /// A join event was received
        /// </summary>
        /// <param name="rawMessage">The raw message that was received</param>
        private static void OnJoinedToChat(string rawMessage)
        {
            string username = rawMessage.Split('!')[0].Substring(1);
            OnClientJoinedChat?.Invoke(username);
        }

        /// <summary>
        /// We get information about our own user
        /// </summary>
        /// <param name="rawMessage">The raw message that was received</param>
        private static void OnUserInfoUpdated(string rawMessage)
        {
            // We have already set the user information and can return
            if (!string.IsNullOrEmpty(currentUser.displayname)) return;

            string userName = rawMessage.Split("display-name=")[1].Split(";", 2)[0];
            
            string userColor = "#FFFFFF";
            if (rawMessage.Contains("color=#"))
            {
                userColor = rawMessage.Split("color=")[1].Split(";", 2)[0];
            }

            currentUser = new ChatUser
            {
                displayname = userName,
                color = userColor
            };
        }

        #endregion

        #region Write Chat

        /// <summary>
        /// Write a message into chat
        /// </summary>
        /// <param name="message">The message to send</param>
        public static void Write(string message)
        {
            chatStreamWriter.WriteLine($"{USER_MESSAGE_CODE} #{channelName} :{message}");
            OnChatMessageReceived?.Invoke(currentUser, message);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// The user data we get first need to be tweaked, as it is not readable for
        /// us at this stage.
        /// </summary>
        /// <param name="rawMessage">The raw message to convert into JSON readable data</param>
        /// <returns>The converted string, readable to convert to a ChatUser struct</returns>
        private static string ConvertMessageToChatUser(string rawMessage)
        {
            // First we split the message to remove the actual chat message from the rest
            string[] result = rawMessage.Split($" {USER_MESSAGE_CODE} ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (result.Length != 2)
            {
                Debug.LogError($"Error while parsing message, splitting string did not result in two parts.. Please check the message: '{rawMessage}'");
                return string.Empty;
            }

            string userData = result.First();

            // To make message compatible with C#, we need to remove variable names containing minus, so we turn e.g. "badge-info" to "badgeinfo"
            userData = userData.Replace("-", "");

            // Next we split the message into a list by using the semicolon as separator
            List<string> entryList = userData.Split(';').ToList();

            // Prepare variables to be in quotes for a string and non-quotes if it is a number
            string json = "{";
            for (int i = 0; i < entryList.Count; i++)
            {
                string[] entry = entryList[i].Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (entry.Length < 2) continue;

                string paramName = $"\"{entry[0]}\":"; // name is always a string

                bool isNumber = entry[1].All(char.IsDigit);
                if (!isNumber) entry[1] = $"\"{entry[1]}\""; // if not a number, wrap with parentheses

                json += paramName + entry[1] + (i == entryList.Count - 1 ? ' ' : ',');
            }

            json += "}";

            // You can enable this log for debugging
            // Debug.Log($"JSON: {json}");

            return json;
        }

        // https://dev.twitch.tv/docs/chat/irc#irc-tag-reference
        [Serializable]
        public struct ChatUser
        {
            public string badgeInfo;
            public string badges;
            public string bits;
            public string clientnonce;
            public string color;
            public string displayname;
            public string emotes;
            public int emoteOnly;
            public int firstMsg;
            public string flags;
            public string id;
            public int mod;
            public int returningChatter;
            public int roomId;
            public int subscriber;
            public ulong tmiSentTs;
            public int turbo;
            public int userId;
            public string userType;
            public int vip;
        }

        #endregion
    }
}