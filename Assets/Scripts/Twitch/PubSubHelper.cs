using System.Collections;
using TwitchLib.Unity;
using UnityEngine;

public class PubSubHelper : MonoBehaviour
{
    private PubSub m_pubsub;

    private void Awake()
    {
        m_pubsub = new PubSub();
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
            StartCoroutine(CreatePubSub());
        }
        else
        {
            DisconnectPubSub();
        }
    }

    private IEnumerator CreatePubSub()
    {
        while (TwitchAPIHelper.User == null)
        {
            yield return null;
        }

        m_pubsub.OnPubSubServiceConnected += OnPubSubServiceConnected;
        m_pubsub.OnBitsReceivedV2 += OnBitsReceivedV2;

        m_pubsub.Connect();
    }

    private void DisconnectPubSub()
    {

        m_pubsub.OnPubSubServiceConnected -= OnPubSubServiceConnected;
        m_pubsub.OnBitsReceivedV2 -= OnBitsReceivedV2;

        m_pubsub.Disconnect();
    }

    private void OnPubSubServiceConnected(object sender, System.EventArgs e)
    {
        Debug.Log("PubSubServiceConnected!");

        // On connect listen to Bits evadsent
        // Please note that listening to the whisper events requires the chat_login scope in the OAuth token.
        // m_pubsub.ListenToWhispers(TwitchAuth.Instance.User);
        m_pubsub.ListenToBitsEventsV2("avghans");

        // SendTopics accepts an oauth optionally, which is necessary for some topics, such as bit events.
        m_pubsub.SendTopics(TwitchAuth.Instance.AccessToken);
    }

    private void OnBitsReceivedV2(object sender, TwitchLib.PubSub.Events.OnBitsReceivedV2Args args)
    {
        Debug.Log(args.ChatMessage);
    }

    private void OnWhisper(object sender, TwitchLib.PubSub.Events.OnWhisperArgs e)
    {
        Debug.Log($"{e.Whisper.Data}");
        // Do your bits logic here.
    }
}
