using Firesplash.GameDevAssets.SocketIOPlus.EngineIO;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Firesplash.GameDevAssets.SocketIOPlus.EngineIO.DataTypes;

//Note: EngineIO should create a new gameobject as a receiver using a prefix and a GUID as name to safely communicate with JSLib.

namespace Firesplash.GameDevAssets.SocketIOPlus
{
    internal class WebGLImplementation : Tranceiver
    {
        /// <summary>
        /// This is thrown when an error is received from the JSLib implementation. The browser console might contain additional information.
        /// </summary>
        public class EngineIOWebGLException : Exception
        {
            internal EngineIOWebGLException(string message) : base(message) { }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        SIOWebGLMessenger messenger;


        class SIOWebGLMessenger : MonoBehaviour
        {
            Queue<EngineIOPacket> sendQueue;

            [System.Runtime.InteropServices.DllImport("__Internal")]
            private static extern void EngineIOWSCreateInstance(string instanceName, string targetAddress);

            [System.Runtime.InteropServices.DllImport("__Internal")]
            private static extern void EngineIOWSSendString(string instanceName, string message);

            [System.Runtime.InteropServices.DllImport("__Internal")]
            private static extern void EngineIOWSSendBinary(string instanceName, byte[] message);

            [System.Runtime.InteropServices.DllImport("__Internal")]
            private static extern void EngineIOWSClose(string instanceName);

            internal ConnectionState State = 0;

            internal delegate void WebsocketDataReceivedEvent(bool isBinary, object data);
            internal delegate void WebsocketStateReceivedEvent(int newState, bool uncleanChange);
            internal delegate void WebsocketErrorEvent(string errorMessage);
            internal WebsocketDataReceivedEvent OnDataReceived;
            internal WebsocketStateReceivedEvent OnStateReceived;
            internal WebsocketErrorEvent OnError;

            internal SIOWebGLMessenger()
            {
                Debug.Log("[Engine.IO] Created JSLib Messenger for Engine.IO using name " + this.name);
                sendQueue = new Queue<EngineIOPacket>();
            }

            private void Update()
            {
                int count = 0;
                //limit to 50 packets per frame to save framerate as this synchronous
                while (sendQueue.Count > 0 && count++ < 50)
                {
                    EngineIOPacket packet = sendQueue.Dequeue();

                    if (packet.IsBinaryMessage())
                    {
                        EngineIOWSSendBinary(this.name, packet.GetPacketBytesForTransmission());
                    }
                    else
                    {
                        EngineIOWSSendString(this.name, packet.GetPacketStringForTransmission());
                    }
                }
            }

            internal void Connect(string serverAddress)
            {
                EngineIOWSCreateInstance(this.name, serverAddress);
            }

            internal void Close()
            {
                EngineIOWSClose(this.name);
            }

            // internal method called from JSLib
            public void EngineIOWebSocketState(int state)
            {
                OnStateReceived?.Invoke(state, false);
                State = (ConnectionState)state;
            }

            // internal method called from JSLib
            public void EngineIOWebSocketUncleanState(int state)
            {
                OnStateReceived?.Invoke(state, true);
                State = (ConnectionState)state;
            }

            // internal method called from JSLib
            public void EngineIOWebSocketStringMessage(string data)
            {
                OnDataReceived?.Invoke(false, data);
            }

            // internal method called from JSLib
            public void EngineIOWebSocketBinaryMessage(byte[] data)
            {
                OnDataReceived?.Invoke(true, data);
            }

            // internal method called from JSLib
            public void EngineIOWebSocketError(string error)
            {
                OnError?.Invoke(error);
            }

            public void Send(EngineIOPacket packet)
            {
                sendQueue.Enqueue(packet);
            }

            public void Send(EngineIOPacket[] packets)
            {
                for (int i = 0; i < packets.Length; i++) sendQueue.Enqueue(packets[i]);
            }

            ~SIOWebGLMessenger()
            {
                EngineIOWSClose(this.name);
            }
        }

        internal override ConnectionState State
        {
            get
            {
                if (messenger == null) return ConnectionState.None;
                if (!handshakeCompleted && messenger.State == ConnectionState.Open) return ConnectionState.Handshake;
                return messenger.State;
            }
        }

        internal WebGLImplementation(string pDefaultPath) : base(pDefaultPath)
        {
            messenger = (SIOWebGLMessenger)new GameObject("SIOMessenger-" + Guid.NewGuid().ToString()).AddComponent(typeof(SIOWebGLMessenger));
            messenger.OnDataReceived += (isBinary, data) =>
            {
                ReceiveMessage(EngineIOPacket.Parse(isBinary, (isBinary ? (byte[])data : Encoding.UTF8.GetBytes((string)data))));
            };
            messenger.OnError += (errorMsg) =>
            {
                OnError?.Invoke(new EngineIOWebGLException(errorMsg));
            };
            messenger.OnStateReceived += (newState, uncleanChange) =>
            {
                switch ((ConnectionState)newState)
                {
                    case ConnectionState.Open:
                        isClosingCleanly = false;
                        break;
                    case ConnectionState.Closed:
                        if (!isClosingCleanly || uncleanChange)
                        {
                            Disconnect("transport close");
                        }
                        Disconnect();
                        break;
                }
            };
        }

        ~WebGLImplementation()
        {
            if (messenger != null) GameObject.DestroyImmediate(messenger);
        }

        internal override void Connect(string serverAddress)
        {
            Log("Connecting to server using WebGL implementation");
            base.Connect(serverAddress);
            messenger.Connect(connectionTarget.ToString());
        }

        internal override void Disconnect(string uncleanReason = null)
        {
            base.Disconnect(uncleanReason);
            if (uncleanReason != null)
            {
                messenger.Close();
            }
            else
            {
                sendQueue.Enqueue(new EngineIOPacket(EIOPacketType.Close));
            }
        }

        internal override void SendPacket(EngineIOPacket packet)
        {
            messenger.Send(packet);
        }

        internal override void SendPackets(EngineIOPacket[] packets)
        {
            if (packets == null) return;
            messenger.Send(packets);
        }

#else
        internal WebGLImplementation(string pDefaultPath) : base(pDefaultPath)
        {
            throw new NotImplementedException("The app has been compiled for Native targets so the WebGL implementation is not included.");
        }
#endif

    }
}
