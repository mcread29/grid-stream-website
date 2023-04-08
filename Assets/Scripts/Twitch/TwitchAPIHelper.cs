using System.Collections;
using System.Collections.Generic;
using TwitchLib.Unity;
using UnityEngine;
using UnityEngine.Networking;

public class TwitchAPIHelper : MonoBehaviour
{
    private static TwitchAPIHelper m_instance;
    public static TwitchAPIHelper Instance { get { return m_instance; } }

    private Api _api;

    private static TwitchLib.Api.Helix.Models.Users.GetUsers.User m_user;
    public static TwitchLib.Api.Helix.Models.Users.GetUsers.User User { get { return m_user; } }

    private static Texture2D m_profileTexture;
    public static Texture2D ProfileTexture { get { return m_profileTexture; } }

    private void Awake()
    {
        _api = new Api();
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
            _api.Settings.ClientId = TwitchAuth.Instance.ClientId;
            _api.Settings.AccessToken = TwitchAuth.Instance.AccessToken;

            StartCoroutine(GetUsers());
        }
    }

    private IEnumerator GetUsers()
    {
        TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse getUsersResponse = null;
        yield return _api.InvokeAsync(
            _api.Helix.Users.GetUsersAsync(logins: new List<string> { TwitchAuth.Instance.User }),
            (response) => getUsersResponse = response
        );

        m_user = getUsersResponse.Users[0];
        StartCoroutine(DownloadImage(m_user.ProfileImageUrl));
    }

    IEnumerator DownloadImage(string MediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError)
            Debug.Log(request.error);
        else
            m_profileTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
    }
}
