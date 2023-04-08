using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class TwitchChat : MonoBehaviour
{
    private static TwitchChat m_instance;
    public static TwitchChat Instance
    {
        get
        {
            return m_instance;
        }
    }

    private TcpClient m_twitchClient;
    StreamReader m_reader;
    StreamWriter m_writer;
    private float m_reconnectTimer;
    private float m_reconnectAfter = 60;

    private string m_user;
    public string User
    {
        get { return m_user; }
    }

    private void Awake()
    {
        if (m_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        m_instance = this;
        DontDestroyOnLoad(this);
    }

    private void OnEnable()
    {
        TwitchAuth.Instance.OnLoginResult.AddListener(OnLogin);
    }

    private void OnDisable()
    {
        TwitchAuth.Instance.OnLoginResult.RemoveListener(OnLogin);
    }

    private void OnLogin(bool loginResult, string status = null)
    {
        if (loginResult)
        {
            Connect(TwitchAuth.Instance.AccessToken, TwitchAuth.Instance.User);
        }
        else
        {

        }
    }

    public void Connect(string accessToken, string user)
    {
        m_user = user;

        m_twitchClient = new TcpClient("irc.chat.twitch.tv", 6667);
        m_reader = new StreamReader(m_twitchClient.GetStream());
        m_writer = new StreamWriter(m_twitchClient.GetStream());
        m_writer.WriteLine($@"PASS oauth:{accessToken}");
        m_writer.WriteLine($@"NICK {user}");
        m_writer.WriteLine($@"JOIN #{user}");
        m_writer.WriteLine($@"PRIVMSG # {user}:Connected!");
        m_writer.Flush();

        print("connect to chat");
    }

    private void Update()
    {
        if (m_twitchClient == null)
        {
            return;
        }

        if (!m_twitchClient.Connected)
        {
            print("Can't connect");
            Connect(TwitchAuth.Instance.AccessToken, TwitchAuth.Instance.User);
        }

        if (m_twitchClient.Available == 0)
        {
            m_reconnectTimer += Time.deltaTime;
            if (m_reconnectTimer >= m_reconnectAfter)
            {
                Connect(TwitchAuth.Instance.AccessToken, TwitchAuth.Instance.User);
                m_reconnectTimer = 0;
            }
        }
    }

    private void FixedUpdate()
    {
        if (m_twitchClient == null) return;

        if (m_twitchClient.Available > 0)
        {
            string message = m_reader.ReadLine();
            print(message);
        }
    }
}
