using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Firesplash.UnityAssets.TwitchAuthentication.DataTypes
{
    public class AuthenticationEvent : UnityEvent<AuthenticationResult> { };

    /// <summary>
    /// Contains identity and validity information about an access token
    /// </summary>
    [Serializable]
    public class AccessTokenMetadata
    {
        [JsonProperty("client_id")]
        public string client_id { get; internal set; }

        [JsonProperty("login")]
        public string login { get; internal set; }

        [JsonProperty("scopes")]
        public string[] scopes { get; internal set; }

        [JsonProperty("user_id")]
        public string user_id { get; internal set; }

        public DateTime last_update { get; internal set; } = DateTime.Now;

        [JsonProperty("expires_in")]
        internal int expires_in_historical;

        [JsonProperty("expires_in_calculated")]
        public int expires_in { get
            {
                return (int)(valid_until - DateTime.Now).TotalSeconds;
            } 
        }

        public DateTime valid_until { get { 
            return last_update.AddSeconds(expires_in_historical);
        } }
    }

    [Serializable]
    internal class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string access_token { get; internal set; }

        [JsonProperty("refresh_token")]
        public string refresh_token { get; internal set; }
        //public string?[] scope { get; internal set; }

        internal DateTime received = DateTime.Now;

        [JsonProperty("expires_in")]
        internal int expires_in = 0;
        public DateTime valid_until { get { 
            return received.AddSeconds(expires_in);
        } }
    }

#pragma warning disable CS0649
    [Serializable]
    internal struct ValidateStatus
    {
        [JsonProperty("status")]
        public int status;

        [JsonProperty("message")]
        public string message;
    }
#pragma warning restore CS0649
}
