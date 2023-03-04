using Firesplash.UnityAssets.TwitchAuthentication.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Firesplash.UnityAssets.TwitchAuthentication
{
    [Serializable]
    public class AuthenticationResult
    {
        /// <summary>
        /// If this result is originating from an interactive login, this contains the originating request that has been used to get this token. Can be handy for re-running the request.
        /// For refreshes of existing tokens (automatic or manual) using a refresh token (available on authorization code flows), there is no originating request so this field will be set to null instead.
        /// </summary>
        public AuthenticationRequest originatingRequest;

        /// <summary>
        /// If this is set to true, the authorization was successful
        /// </summary>
        public bool isSuccessful { get; internal set; } = false;

        /// <summary>
        /// This field allows you to distinguish between different reasons / methods of aquiring a token.
        /// Especially with authorization code flows not every AuthenticationResult means a login has taken place!
        /// It is up to your application to distinguish when and what to do upon result receival. Remember your application states.
        /// </summary>
        public Initiator initiator { get; internal set; } = Initiator.InteractiveLogin;

        /// <summary>
        /// If Metadate Extraction was requested in the authentication request, this field contains the metadata, else it is null
        /// </summary>
        public AccessTokenMetadata accessTokenMetadata { get; internal set; } = null;

        /// <summary>
        /// After authentication has been completed, this field contains the accessToken
        /// </summary>
        public string accessToken { get; internal set; }

        internal string refreshToken;

        /// <summary>
        /// In case of an error, this field contains the technical error name like access_denied
        /// </summary>
        public string error { get; internal set; }

        /// <summary>
        /// In case of an error (technical or authentication issue) this variable will provide a human readable short description of the error that occured. It is null otherwise.
        /// </summary>
        public string errorDescription { get; internal set; } = null;

        #region sub-routines
        internal IEnumerator UpdateTokenMetadata(bool isValidityProbe)
        {
            UnityWebRequest tkMetaRequest = UnityWebRequest.Get("https://id.twitch.tv/oauth2/validate");
            tkMetaRequest.SetRequestHeader("Authorization", "OAuth " + accessToken);
            yield return tkMetaRequest.SendWebRequest();
            if (tkMetaRequest.result == UnityWebRequest.Result.Success && tkMetaRequest.responseCode == 200)
            {
                this.accessTokenMetadata = JsonConvert.DeserializeObject<AccessTokenMetadata>(tkMetaRequest.downloadHandler.text);
                this.isSuccessful = true;
                this.error = null;
                this.errorDescription = null;
            }
            else 
            {
                ValidateStatus status = JsonConvert.DeserializeObject<ValidateStatus>(tkMetaRequest.downloadHandler.text);
                accessTokenMetadata = null;
                isSuccessful = false;
                accessToken = null;
                error = "validate-error-" + status.status;
                errorDescription = status.message;
                if (!isValidityProbe) Debug.LogError("Encountered an error validating the recently requested access token: " + tkMetaRequest.downloadHandler.text);
            }
            yield return 0;
        }
        #endregion



        public enum Initiator
        {
            /// <summary>
            /// The user was actively sent to twitch (maybe without an actual authorization request). This is usually the case when a user clicks login button.
            /// </summary>
            InteractiveLogin,

            /// <summary>
            /// The helper already had a stored refresh token and the result object is originating from the initial token refresh happening on Start() of the helper component OR after a manual call to RefreshToken() (only authorization code flows)
            /// </summary>
            InstantRefresh,

            /// <summary>
            /// The helper already had a stored refresh token and the result object is originating from an automatic background refresh at runtime. This can happen at any given time (only authorization code flows)
            /// </summary>
            BackgroundRefresh
        }
    }
}
