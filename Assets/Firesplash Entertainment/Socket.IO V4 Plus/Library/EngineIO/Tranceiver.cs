using Firesplash.GameDevAssets.SocketIOPlus.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using static Firesplash.GameDevAssets.SocketIOPlus.EngineIO.DataTypes;

namespace Firesplash.GameDevAssets.SocketIOPlus.EngineIO
{
    internal class Tranceiver
    {
        internal Uri connectionTarget { get; private set; }

        internal virtual ConnectionState State { get; private set; }

        private protected string defaultPath = "/engine.io/";

        private protected bool handshakeCompleted = false;

        /// <summary>
        /// This fires for any message packets AND PING
        /// </summary>
        internal EngineIOMessageReceivedEvent OnDataReceived;

        internal EngineIOConnectionReadyEvent OnConnectionReady;

        internal EngineIODisconnectEvent OnDisconnect;

        internal EngineIOConnectErrorEvent OnError;

        private protected ConcurrentQueue<EngineIOPacket> sendQueue;

        internal ConnectionParameters connectionParams;

        protected bool isClosingCleanly = false; //this is only actually used by WebGL implementation but we need to set it when parsing EIO packets


        public Tranceiver(string pDefaultPath) {
            connectionTarget = null;
            defaultPath = pDefaultPath;
            sendQueue = new ConcurrentQueue<EngineIOPacket>();
        }

        ~Tranceiver()
        {
            Disconnect();
        }

        internal virtual void Connect(string serverAddress) {
            isClosingCleanly = false;

            //This code will called BEFORE the actual connect happens (base.Connect() is first line)
            UriBuilder uri = new UriBuilder(serverAddress);

            //switch to websocket protocols
            if (uri.Scheme.StartsWith("http"))
            {
                uri.Scheme = "ws" + uri.Scheme.Substring(4);
            }

            //Add engine.io path if none specified
            if (!serverAddress.Substring(serverAddress.IndexOf("//") + 2).Contains('/'))
            {
                uri.Path = defaultPath;
            }

            //Add required query parameters
            handshakeCompleted = false;
            var queryDictionary = System.Web.HttpUtility.ParseQueryString((uri.Query == null ? "" : uri.Query));
            queryDictionary["EIO"] = "4";
            queryDictionary["transport"] = "websocket";
            queryDictionary.Remove("sid"); //just in case
            uri.Query = queryDictionary.ToString();

            //Store new URI
            connectionTarget = uri.Uri;
        }

        /// <summary>
        /// disconnects the tranceiver.
        /// If a reason is given (not null) this means the disconnect was unclean
        /// </summary>
        /// <param name="reason"></param>
        internal virtual void Disconnect(string uncleanReason = null) {
            OnDisconnect?.Invoke(false, (uncleanReason != null ? uncleanReason : "io client disconnect"));
        }

        internal void ReceiveMessage(EngineIOPacket packet)
        {
            try
            {
                switch (packet.GetPacketType())
                {
                    case EIOPacketType.Open:
                        isClosingCleanly = false; //this is a good point to reset the flag
                        connectionParams = JsonConvert.DeserializeObject<ConnectionParameters>(packet.GetPayloadString());
                        handshakeCompleted = true;
                        if (OnConnectionReady != null) OnConnectionReady.Invoke(connectionParams);
                        break;

                    case EIOPacketType.Close:
                        Log("Received CLOSE from server");
                        isClosingCleanly = true;
                        OnDisconnect?.Invoke(true, "io server disconnect");
                        break;

                    case EIOPacketType.Ping:
                        SendPacket(new EngineIOPacket(EIOPacketType.Pong));
                        if (OnDataReceived != null) OnDataReceived.Invoke(packet);
                        break;

                    case EIOPacketType.Message:
                        if (OnDataReceived != null) OnDataReceived.Invoke(packet);
                        break;
                }
            }
            catch (Exception e)
            {
                SIODispatcher.Instance.LogException(e);
            }

        }

        internal virtual void SendPacket(EngineIOPacket packet)
        {
            sendQueue.Enqueue(packet);
        }

        internal virtual void SendPackets(EngineIOPacket[] packets)
        {
            if (packets == null) return;
            for (int i=0; i < packets.Length; i++) SendPacket(packets[i]);
        }

        internal void Log(string message)
        {
#if VERBOSE
            SIODispatcher.Instance.Log("[EngineIO] " + message);
#endif
        }

        internal void LogException(Exception e)
        {
            SIODispatcher.Instance.LogException(e);
        }

        internal void LogError(string message)
        {
            SIODispatcher.Instance.LogError("[EngineIO] " + message);
        }
    }
}
