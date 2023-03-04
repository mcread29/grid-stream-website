using Firesplash.GameDevAssets.SocketIOPlus.EngineIO;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Firesplash.GameDevAssets.SocketIOPlus.DataTypes;
using static Firesplash.GameDevAssets.SocketIOPlus.EngineIO.DataTypes;
using ConnectionState = Firesplash.GameDevAssets.SocketIOPlus.EngineIO.DataTypes.ConnectionState;

namespace Firesplash.GameDevAssets.SocketIOPlus
{
    /// <summary>
    /// This MonoBehavior derives from EngineIOClient and implements the main Socket.IO Manager logic on top of Engine.IO.
    /// It implements Socket.IO protocol version 5 (which is used by Socket.IO v3 and v4)
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Networking/Socket.IO/Socket.IO Client")]
    public class SocketIOClient : EngineIOClient
    {
        /// <summary>
        /// This event receives every single incoming Socket.IO Packet regardless of it's namespace and type. This is the quickest possible method to receive a packet.
        /// This event <b>fires on a thread</b> immediately after receiving the complete packet and before processing it further. You may not access Unity Engine functions directly from this callback.
        /// </summary>
        internal SocketIOPacketReceivedEvent OnIncomingSIOPacketCatchallThreaded;

        private Dictionary<string, SocketIONamespace> connectedNamespaces;

        private Dictionary<string, UnityEvent<object>> registeredEventCallbacks;

        private static List<string> validManagerEvents = new List<string> { "error", "reconnect", "reconnect_attempt", "reconnect_error", "reconnect_failed" };

        ConnectionState supposedEngineState = ConnectionState.None;
        Parser parser;
        SocketIOPacket incomingPacket;

        SocketIOAuthPayloadCallback authPayloadCallback;

        //When this is set to true, the next LateUpdate will trigger namespace connects - this is done to make auth payload callbacks thread safe
        bool readyForConnect = false;

        /// <summary>
        /// If the connection (or a reconnect) is not successful after n attempts, cancel trying to (re)connect.
        /// A value of zero means infinitely.
        /// </summary>
        public int maxConnectAttempts = 0;

        /// <summary>
        /// After a failure, (re)connection attempts will be delayed by initially one second.
        /// On every failure the delay is raised by 50% and ceiled (1, 2, 3, 5, 8, 12, ...) up to 60 seconds.
        /// On a successful (re)connect this delay is reset to one second.
        /// If you set this value to false, the delay will always be one second +/-20% jitter.
        /// </summary>
        public bool raisingReconnectDelay = true;

        int connectAttempts = 0;
        float connectAttemptCountdown = -1;

        /// <summary>
        /// This is a shorthand to access the default namespace without having to write the whole namespace call every time for simple applications.
        /// </summary>
        /// <see cref="DefaultNamespace"/>
        public SocketIONamespace D
        {
            get {
                return DefaultNamespace;
            }
        }

        /// <summary>
        /// This is a shorthand to access the default namespace without having to write the whole namespace call every time for simple applications.
        /// Want it even shorter? Use "D" :o)
        /// </summary>
        public SocketIONamespace DefaultNamespace
        {
            get
            {
                return GetNamespace("/");
            }
        }

        /// <summary>
        /// Returns the API of the Socket.IO Client for the given namesapce and connects to the namespace if it is not already connected.
        /// If the underlaying transport is not completely connected yet, the connect to the namespace is delayed until the transport is ready.
        /// </summary>
        /// <param name="namespacePath"></param>
        /// <param name="connectIfNotExists">Connect to this namespace if it is not connected (returns null if false and not existing)</param>
        /// <returns></returns>
        public SocketIONamespace GetNamespace(string namespacePath, bool connectIfNotExists = true) {
            lock (connectedNamespaces)
            {
                if (connectedNamespaces.ContainsKey(namespacePath)) return connectedNamespaces[namespacePath];
                else if (connectIfNotExists)
                {
                    SocketIONamespace newNS = new SocketIONamespace(namespacePath, this);
                    connectedNamespaces.Add(namespacePath, newNS);
                    if (State == EngineIO.DataTypes.ConnectionState.Open) newNS.Connect(authPayloadCallback?.Invoke(namespacePath));
                    return newNS;
                } 
                else
                {
                    return null;
                }
            }
        }

        new private void Awake()
        {
            //Override default path
            defaultPath = "/socket.io/";
            connectedNamespaces = new Dictionary<string, SocketIONamespace>();
            registeredEventCallbacks = new Dictionary<string, UnityEvent<object>>();

            base.Awake();
            tranceiver.OnConnectionReady += (x) => {
                //Don't connect right away. We hand this to LateUpdate for thread safety
                readyForConnect = true;
            };
            tranceiver.OnDisconnect += PropagateDisconnect;
            tranceiver.OnDataReceived += ProcessEngineIOMessage;
            tranceiver.OnError += (exc) => {

                if (supposedEngineState != ConnectionState.Open) FireManagerEvent("error", exc);
                else FireManagerEvent("reconnect_error", exc);

                if (maxConnectAttempts == 0 || connectAttempts < maxConnectAttempts)
                {
                    connectAttemptCountdown = (raisingReconnectDelay ? Mathf.Clamp(Mathf.Ceil(Mathf.Pow(1.5f, connectAttempts-1)), 1, 60) : UnityEngine.Random.Range(0.8f, 1.2f));
#if VERBOSE
                    Debug.Log("[Socket.IO] Next reconnect attempt in " + connectAttemptCountdown + " seconds");
#endif
                } 
                else
                {
                    if (supposedEngineState == ConnectionState.Connecting) FireManagerEvent("reconnect_failed", null);
                } 

                if (supposedEngineState == ConnectionState.Connecting)
                {
                    foreach (SocketIONamespace ns in connectedNamespaces.Values)
                    {
                        ns.FireEvent("connect_error", new object[] { exc });
                    }
                }
            };
        }

        new protected void LateUpdate()
        {
            base.LateUpdate();

            if (connectAttemptCountdown > 0 && (tranceiver.State == ConnectionState.Closed || tranceiver.State == ConnectionState.Aborted)) //it is important to only try to reconnect if the state is already closed else we could end up calncelling out ourselves
            {
                connectAttemptCountdown -= Time.unscaledDeltaTime;
                if (connectAttemptCountdown <= 0 )
                {
                    connectAttemptCountdown = -1; //this will be set to a new value on failure callback
                    FireManagerEvent("reconnect_attempt", ++connectAttempts);
                    Connect();
                }
            }

            if (readyForConnect)
            {
                readyForConnect = false;
                ConnectNamespaces();
            }
        }

        /// <summary>
        /// This callback will be called whenever a namespace connects.
        /// If the callback returns a value other than null, it will be sent as authentication payload while connecting the namespace.
        /// The function is called from GetNamespace, so if you call this method from a thread, the callback also runs on a thread.
        /// Internally generated connect sequences always call the callback from the main thread.
        /// </summary>
        /// <param name="callback">A SocketIOAuthPayloadCallback delegate</param>
        public void SetAuthPayloadCallback(SocketIOAuthPayloadCallback callback)
        {
            authPayloadCallback = callback;
        }

        public override void Connect(string pServerAddress = null)
        {
            supposedEngineState = ConnectionState.Connecting;
            base.Connect(pServerAddress);
        }

        public override void Disconnect()
        {
            supposedEngineState = ConnectionState.Closed;
            base.Disconnect();
        }

        //TODO check if we must change the protection to allow overriding
        internal protected virtual Parser GetParser()
        {
            if (parser == null)
            {
                parser = new DefaultParser();
            }
            return parser;
        }

        /// <summary>
        /// You can override the used parser using this method. You can implement your own (or a publicly available) parser and message format.
        /// Please remember, that server and clients need to use the same parser (or better said the same message format).
        /// </summary>
        /// <param name="newParser"></param>
        public void SetParser(Parser newParser)
        {
            if (parser != null && tranceiver != null && tranceiver.State == EngineIO.DataTypes.ConnectionState.Open)
            {
                Debug.LogWarning("[Socket.IO] You are overriding the parser at runtime and after it has been initialized. This should be done before connecting to prevent unforeseeable issues!");
            }
            parser = newParser;
        }

        private void ConnectNamespaces()
        {
            supposedEngineState = ConnectionState.Open;

            if (connectAttempts > 0)
            {
                FireManagerEvent("reconnect", null);
            }

            connectAttempts = 0;
            connectAttemptCountdown = -1;
            if (connectedNamespaces != null && connectedNamespaces.Count > 0)
            {
                foreach(var ns in connectedNamespaces)
                {
                    if (ns.Value.shallConnect) ns.Value.Connect(authPayloadCallback?.Invoke(ns.Key));
                }
            }
        }

        private void PropagateDisconnect(bool serverInitiated, string message)
        {
            if (serverInitiated) supposedEngineState = ConnectionState.Closed;
            if (connectedNamespaces != null && connectedNamespaces.Count > 0)
            {
                foreach(var ns in connectedNamespaces)
                {
                    ns.Value.Disconnect(serverInitiated, message);
                }
            }

            //Should we reconnect?
            if (supposedEngineState == ConnectionState.Open)
            {
                supposedEngineState = ConnectionState.Connecting;
                connectAttemptCountdown = 0.001f; //first reconnect is instantly
            }
        }

        internal void Send(SocketIOPacket packet)
        {
            SendEngineIOPackets(GetParser().Encode(packet, this));
        }

        private void ProcessEngineIOMessage(EngineIOPacket packet)
        {
            if (packet.GetPacketType() == EIOPacketType.Ping) return;

            if (!packet.IsBinaryMessage())
            {
                if (incomingPacket != null)
                {
                    Debug.Log("[Socket.IO] Received a Socket.IO packet while the previous one was not yet complete. This can be a server issue or data got lost during transmission.");
                }
                incomingPacket = GetParser().Parse(packet, this);
            }
            else if (incomingPacket == null)
            {
                Debug.LogWarning("[Socket.IO] Received orphan binary payload. It has been ignored. This can be a server issue or data got lost during transmission.");
            } 
            else
            {
                incomingPacket.AddReceivedBinaryPayload(packet.GetPayloadBytes());
            }

            if (incomingPacket.IsComplete())
            {
                if (connectedNamespaces.ContainsKey(incomingPacket.namespacePath))
                {
                    if (OnIncomingSIOPacketCatchallThreaded != null) OnIncomingSIOPacketCatchallThreaded.Invoke(incomingPacket);
                    connectedNamespaces[incomingPacket.namespacePath].Submit(incomingPacket);
                } 
                else
                {
                    Debug.LogWarning("[Socket.IO] Received a Socket.IO packet designated for a namespace " + incomingPacket.namespacePath + " we are not connected to. This is a server issue: " + GetParser().Encode(incomingPacket, this)[0].GetPayloadString());
                }
                incomingPacket = null;
            }
        }

        internal void FireManagerEvent(string eventName, object payload)
        {
#if VERBOSE
            Debug.Log("[Socket.IO] Firing Manager Event: " + eventName + " " + payload);
#endif
            if (registeredEventCallbacks.ContainsKey(eventName))
            {
                registeredEventCallbacks[eventName].Invoke(payload);
            }
        }

        /// <summary>
        /// Allows registering to "low level" manager events. This is NOT a namespaced event listener!
        /// </summary>
        /// <param name="eventName">The event name (one of error, reconnect, reconnect_attempt, reconnect_error, reconnect_failed)</param>
        /// <param name="callback">The callback to be called. The parameter contains values according to the official Socket.IO documentation. The Error event has a string. For events having no payload, the value is null.</param>
        /// <exception cref="SocketIOException">Thrown, if the given eventName is not a valid Manager event</exception>
        /// <see cref="https://socket.io/docs/v4/client-api/#events"/>
        public void On(string eventName, UnityAction<object> callback)
        {
            if (!validManagerEvents.Contains(eventName))
            {
                throw new SocketIOException(eventName + " is not a valid manager event. Did you mean to subscribe to a namespaced event?");
            }

            if (!registeredEventCallbacks.ContainsKey(eventName))
            {
                registeredEventCallbacks.Add(eventName, new UnityEvent<object>());
            }
            registeredEventCallbacks[eventName].AddListener(callback);
        }

        /// <summary>
        /// Unregisters a previously registered manager event callback
        /// </summary>
        /// <param name="eventName">The event name - For valid values see On(...)</param>
        /// <param name="callback">The callback to unregister</param>
        /// <exception cref="SocketIOException">Thrown, if the given eventName is not a valid Manager event</exception>
        public void Off(string eventName, UnityAction<object> callback)
        {
            if (!validManagerEvents.Contains(eventName))
            {
                throw new SocketIOException(eventName + " is not a valid manager event. Did you mean to unsubscribe from a namespaced event?");
            }

            if (!registeredEventCallbacks.ContainsKey(eventName))
            {
                registeredEventCallbacks[eventName].RemoveListener(callback);
            }
        }
    }
}
