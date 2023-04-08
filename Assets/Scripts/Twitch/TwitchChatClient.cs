using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TwitchLib.Client.Models;
using TwitchLib.Unity;

public class TwitchChatClient : MonoBehaviour
{
    private Client m_client;

    private void Awake()
    {
        m_client = new Client();
    }

    private void OnEnable()
    {
        TwitchAuth.Instance.OnLoginResult.AddListener(OnLogin);
    }

    private void OnDisable()
    {
        TwitchAuth.Instance.OnLoginResult.RemoveListener(OnLogin);
    }

    private void OnLogin(bool loginResult, string status)
    {
        if (loginResult)
        {
            print("test");
            StartCoroutine(CreateClient());
        }
        else
        {
            DisconnectClient();
        }
    }

    private IEnumerator CreateClient()
    {
        while (TwitchAPIHelper.User == null)
        {
            yield return null;
        }

        // string token = "g93qb4a4ipjugmxrv6u45u29pwri87";
        // string user = "avrunnerbot";

        string token = TwitchAuth.Instance.AccessToken;
        string user = TwitchAuth.Instance.User;

        Debug.Log(TwitchAuth.Instance.AccessToken);
        ConnectionCredentials credentials = new ConnectionCredentials(user, token);

        m_client.Initialize(credentials, TwitchAuth.Instance.User);

        m_client.OnConnected += OnConnected;
        m_client.OnConnectionError += OnConnectionError;
        m_client.OnError += OnError;
        m_client.OnJoinedChannel += OnJoinedChannel;
        m_client.OnMessageReceived += OnMessageReceived;
        m_client.OnChatCommandReceived += OnChatCommandReceived;

        m_client.Connect();
    }

    private void DisconnectClient()
    {
        if (m_client.IsInitialized)
        {
            m_client.Disconnect();

            m_client.OnConnected -= OnConnected;
            m_client.OnConnectionError -= OnConnectionError;
            m_client.OnError -= OnError;
            m_client.OnJoinedChannel -= OnJoinedChannel;
            m_client.OnMessageReceived -= OnMessageReceived;
            m_client.OnChatCommandReceived -= OnChatCommandReceived;
        }
    }

    private void OnError(object sender, TwitchLib.Communication.Events.OnErrorEventArgs args)
    {
        Debug.Log(args.Exception);
    }

    private void OnConnectionError(object sender, TwitchLib.Client.Events.OnConnectionErrorArgs args)
    {
        Debug.Log(args.BotUsername + ", " + args.Error);
    }

    private void OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
    {
        Debug.Log("test3");
        Debug.Log($"The bot {e.BotUsername} succesfully connected to Twitch.");

        if (!string.IsNullOrWhiteSpace(e.AutoJoinChannel))
            Debug.Log($"The bot will now attempt to automatically join the channel provided when the Initialize method was called: {e.AutoJoinChannel}");
    }

    private void OnJoinedChannel(object sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
    {
        Debug.Log($"The bot {e.BotUsername} just joined the channel: {e.Channel}");
        m_client.SendMessage(e.Channel, "I just joined the channel! PogChamp");
    }

    private void OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
    {
        Debug.Log($"Message received from {e.ChatMessage.Username}: {e.ChatMessage.Message}");
    }

    private void OnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
    {
        switch (e.Command.CommandText)
        {
            case "hello":
                m_client.SendMessage(e.Command.ChatMessage.Channel, $"Hello {e.Command.ChatMessage.DisplayName}!");
                break;
            case "about":
                m_client.SendMessage(e.Command.ChatMessage.Channel, "I am a Twitch bot running on TwitchLib!");
                break;
            default:
                m_client.SendMessage(e.Command.ChatMessage.Channel, $"Unknown chat command: {e.Command.CommandIdentifier}{e.Command.CommandText}");
                break;
        }
        // e.Command.ChatMessage.IsModerator
    }

    private void Update()
    {
        // Don't call the client send message on every Update,
        // this is sample on how to call the client,
        // not an example on how to code.
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     m_client.SendMessage(user, "I pressed the space key within Unity.");
        // }
    }
}
