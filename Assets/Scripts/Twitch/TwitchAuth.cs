using System.Collections;
using System.Collections.Generic;
using Firesplash.UnityAssets.TwitchAuthentication;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TwitchAuth : MonoBehaviour
{
    private static TwitchAuth m_instance;
    public static TwitchAuth Instance { get { return m_instance; } }

    private bool m_connected = false;
    public bool Connected
    {
        get
        {
            return m_connected;
        }
    }

    private TwitchAuthenticationHelper m_twitchAuth;
    public string AccessToken
    {
        get
        {
            if (m_twitchAuth == null) return "";
            else return m_twitchAuth.LastAuthenticationResult.accessToken;
        }
    }
    public string User
    {
        get
        {
            if (m_twitchAuth == null) return "";
            else return m_twitchAuth.LastAuthenticationResult.accessTokenMetadata.login;
        }
    }
    public string ClientId
    {
        get
        {
            if (m_twitchAuth == null) return "";
            return m_twitchAuth.LastAuthenticationResult.accessTokenMetadata.client_id;
        }
    }

    private bool m_areWeAwaitingAuthentication = true;

    [HideInInspector]
    public UnityEvent<bool, string> OnLoginResult;

    private void Awake()
    {
        if (m_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        m_instance = this;
        DontDestroyOnLoad(this);
        m_twitchAuth = GetComponent<TwitchAuthenticationHelper>();

        OnLoginResult = new UnityEvent<bool, string>();
    }

    private void Start()
    {
        m_twitchAuth.OnAuthenticationFinished.AddListener(TwitchAuthFinished);
    }

    private void TwitchAuthFinished(AuthenticationResult result)
    {
        if (result == null)
        {
            m_connected = false;
            OnLoginResult?.Invoke(false, "You are not logged in");
        }
        else if (m_areWeAwaitingAuthentication)
        {
            if (result.isSuccessful)
            {
                m_connected = true;
                OnLoginResult?.Invoke(true, "Logged in as: " + result.accessTokenMetadata.login);
            }
            else if (result.error.Equals("no_refresh_token"))
            {
                m_connected = true;
                // user not logged in
                OnLoginResult?.Invoke(false, "You are not logged in");
            }
            else
            {
                m_connected = false;
                // error with login in result.errorDescription
                OnLoginResult?.Invoke(false, "Login was not successful: " + result.errorDescription);
            }
        }
    }

    public void LogIn()
    {
        m_areWeAwaitingAuthentication = true;
        AuthenticationRequest authRequest = new AuthenticationRequest(true, AuthenticationRequest.ScopeSet.ChatReadWrite);
        m_twitchAuth.Authenticate(authRequest);
    }

    public void LogOut()
    {
        OnLoginResult?.Invoke(false, "You have been logged out");
        m_twitchAuth.ResetAuthentication();
    }
}
