using System;
using System.Collections;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Firesplash.UnityAssets.TwitchAuthentication.Internal;
using Firesplash.UnityAssets.TwitchAuthentication.DataTypes;
using UnityEngine;
using UnityEngine.Networking;

namespace Firesplash.UnityAssets.TwitchAuthentication
{
    public class TwitchAuthenticationHelper : MonoBehaviour
    {
#if UNITY_WEBGL
#error The Twitch Authentication Library can not be used on WebGL builds.
#endif

        public enum AuthenticationFlow { ImplicitFlow, AuthorizationCodeFlow, ServerAssistedAuthorizationCodeFlow };

        public AuthenticationFlow UsedAuthenticationFlow = AuthenticationFlow.ImplicitFlow;
        public string ClientID;
        [Range(1025, 65535)]
        public int CallbackServerPort = 40501;
        public string ClientSecret;
        [Tooltip("This will be used to distinguish different tokens (every TwitchAuthenticationHelper you use for different purpose should have its own unique value). The default value is a randomly generated value that is unique to this instance. You can safely keep it if you wish. You should only use alphanumeric signs, no spaces!")]
        public string UniqueIdentifier;
        public string AssistantScriptURL;
        [Tooltip("This will be sent to the server side script and acts like a shared key to authenticate the client. Of course this is not real security, it's more like an additional layer of complexity for bad acting people trying to tamper with the served script api.")]
        public string ServerParole;
        [Tooltip("Twitch limits the amount of tokens assigned to a single refresh token. To prevent us from exceeding this limit, we use an intelligent system to only refresh a token when it is actually invalid or expiring soon (within 15 minutes). You can disable this safety limit but it is not recommended to do so.")]
        public bool DisableSafetyLimits = false;
        public bool EnableDebugOutput = false;
        [Header("This is the HTML of the page shown to the user after an interactive authentication.")]
        [TextArea]
        public string CallbackHTMLContent = "<!DOCTYPE HTML><html><body><h2>Twitch Authentication</h2><p>You can now close this window and return to the application.</p></body></html>";

        [Tooltip("If enabled, the helper will store the refresh token and request a new access token once it is expiring. Also it will automatically refresh the token when the application starts. Read the docs for this feature!")]
        public bool AutoRefreshToken;

        /// <summary>
        /// This event is called after an authentication attempt completed. Examine isSuccessful to get the outcome, authSource to see why this event has fired.
        /// Remember, with AutoRefresh turned on, not every call to this event means an authentication has actually taken place.
        /// This MAY be NULL in case of a timeout (user never completed the request at all within given time).
        /// </summary>
        public AuthenticationEvent OnAuthenticationFinished;

        private AuthCallbackListener cbListener;
        private Coroutine runningAuthFlow;
        private string csrfToken;
        public AuthenticationResult LastAuthenticationResult { get; private set; } = null;
        private float nextTokenRefresh = -1;

        internal void Log(string text)
        {
            if (EnableDebugOutput) Debug.Log("Twitch Authentication Debugging on " + gameObject.name + ": " + text);
        }

        private void Reset()
        {
            Debug.Log("Twitch Authentication: A new unique identifier has been created for TwitchAuthenticationHelper on '" + gameObject.name + "' - if you want to use an Authorization Code Flow along with AutoRefresh, you can change this identifier to a useful value. It is not relevant in any other case.");
            Debug.LogWarning("Twitch Authentication: The callback-server port has been randomly generated. Please review the inspector and set the redirect uri on twitch's developer console accordingly! You can also change the port. Don't know what this means or how to do that? Read this: <a href=\"https://dev.twitch.tv/docs/authentication/register-app\">https://dev.twitch.tv/docs/authentication/register-app</a> (Clicking this link only works in unity 2021.2+, copy it for older verisons)");
            UniqueIdentifier = Guid.NewGuid().ToString().Replace("-", "").Substring(5, 10) + UnityEngine.Random.Range(1000, 10000);
            CallbackServerPort = UnityEngine.Random.Range(40000, 50000);
        }

        private void Awake()
        {
            OnAuthenticationFinished = new AuthenticationEvent();
        }

        private void Start()
        {
            StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            yield return 0;
            if (AutoRefreshToken && UsedAuthenticationFlow != AuthenticationFlow.ImplicitFlow)
            {
                Log("Token Auto Refresh is enabled, checking for previously aquired token (Startup)...");
                string refreshToken = PlayerPrefs.GetString("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".RT", "");
                string accessToken = PlayerPrefs.GetString("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".AT", "");
                if (refreshToken != null && refreshToken.Length > 0)
                {
                    Log("Found a token. Trying to refresh it (Startup)");
                    StartCoroutine(RefreshCurrentToken(refreshToken, accessToken, AuthenticationResult.Initiator.InstantRefresh, null));
                }
                else
                {
                    Log("No token available. Waiting for authenticate call and sending a failed callback");
                    AuthenticationResult result = new AuthenticationResult();
                    result.isSuccessful = false;
                    result.initiator = AuthenticationResult.Initiator.InstantRefresh;
                    result.error = "no_refresh_token";
                    result.errorDescription = "You were never logged in before";
                    OnAuthenticationFinished.Invoke(result);
                }
            }
        }

        private void Update()
        {
            if (AutoRefreshToken && LastAuthenticationResult != null && UsedAuthenticationFlow != AuthenticationFlow.ImplicitFlow && nextTokenRefresh > 0 && Time.realtimeSinceStartup > nextTokenRefresh)
            {
                nextTokenRefresh = -1;
                Log("Our maintained token is expiring soon. Refreshing it now...");
                StartCoroutine(RefreshCurrentToken(LastAuthenticationResult.refreshToken, LastAuthenticationResult.accessToken, AuthenticationResult.Initiator.BackgroundRefresh, null));
            }
        }

        private void OnDestroy()
        {
            if (cbListener != null) cbListener.Cancel();
        }

        /// <summary>
        /// Call this to see if an authentication is currently running (this can also be an automatic refresh of a token)
        /// </summary>
        /// <returns>true if any authenticaiton is running right now, false anyways</returns>
        public bool IsAuthenticating()
        {
            return runningAuthFlow != null;
        }

        /// <summary>
        /// Cancel a currently running authentication process (Fire-And-Forget: This method will not error out if none is running)
        /// </summary>
        public void CancelAuthentication()
        {
            if (cbListener != null) cbListener.Cancel();
            if (runningAuthFlow != null) StopCoroutine(runningAuthFlow);
        }

        /// <summary>
        /// Checks if the current token can be refreshed and does so if it is expiring in less than 900 seconds or already expired (or invalidated)
        /// </summary>
        public void RefreshToken()
        {
            RefreshToken(null);
        }

        /// <summary>
        /// Checks if the current token can be refreshed and does so if it is expiring in less than 900 seconds or already expired (or invalidated)
        /// </summary>
        /// <param name="additionalCallback">An optional callback to be invoked ONCE after the refresh finished (regardless of its outcome) additionally to the normal event.</param>
        public void RefreshToken(Action<AuthenticationResult> additionalCallback)
        {
            nextTokenRefresh = -1;
            Log("A manueal refresh has been requested");
            StartCoroutine(RefreshCurrentToken(LastAuthenticationResult.refreshToken, LastAuthenticationResult.accessToken, AuthenticationResult.Initiator.BackgroundRefresh, additionalCallback));
        }

        /// <summary>
        /// Stops auto-refreshing and removes the stored token from the persistant storage.
        /// </summary>
        public void ResetAuthentication()
        {
            CancelAuthentication();
            LastAuthenticationResult = null;
            nextTokenRefresh = -1;
            PlayerPrefs.DeleteKey("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".RT");
            PlayerPrefs.DeleteKey("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".AT");
        }

        /// <summary>
        /// This starts the configured interactive authentication flow by opening a browser and directing the user to the login page (if required).
        /// </summary>
        /// <param name="request">The authentication request to send</param>
        public void Authenticate(AuthenticationRequest request)
        {
            Debug.Log(request.requestedScopes.Count);
            Authenticate(request, null);
        }

        /// <summary>
        /// This starts the configured authentication flow.
        /// </summary>
        /// <param name="request">The AuthenticationRequest to send</param>
        /// <param name="additionalCallback">If specified, this action will be executed upon authentication (no matter if successful or failed, you can find this info in the given object). This callback will be executed in addition to the OnAuthenticationFinished event</param>
        public void Authenticate(AuthenticationRequest request, Action<AuthenticationResult> additionalCallback)
        {
            if (cbListener != null)
            {
                cbListener.Cancel();
            }

            if (runningAuthFlow != null)
            {
                StopCoroutine(runningAuthFlow);
            }

            csrfToken = Guid.NewGuid().ToString();

            switch (UsedAuthenticationFlow)
            {
                case AuthenticationFlow.ImplicitFlow:
                    runningAuthFlow = StartCoroutine(ImplicitGrantFlow(request, additionalCallback));
                    break;

                case AuthenticationFlow.AuthorizationCodeFlow:
                    runningAuthFlow = StartCoroutine(AuthorizationCodeFlow(request, additionalCallback));
                    break;

                case AuthenticationFlow.ServerAssistedAuthorizationCodeFlow:
                    runningAuthFlow = StartCoroutine(AuthorizationCodeFlow(request, additionalCallback));
                    break;
            }
        }

        IEnumerator ImplicitGrantFlow(AuthenticationRequest request, Action<AuthenticationResult> additionalCallback)
        {
            if (request == null) throw new NullReferenceException("A request object must be given");


            NameValueCollection response = null;
            cbListener = new AuthCallbackListener(CallbackServerPort, (pResponse) =>
            {
                response = System.Web.HttpUtility.ParseQueryString("?" + pResponse.Substring(1));
            }, CallbackHTMLContent);

            UriBuilder requestUri = new UriBuilder("https://id.twitch.tv/oauth2/authorize");
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString("");
            queryString.Add("client_id", ClientID);
            queryString.Add("force_verify", request.forceVerify.ToString().ToLower());
            queryString.Add("redirect_uri", "http://localhost:" + CallbackServerPort);
            queryString.Add("scope", string.Join(" ", request.requestedScopes));
            queryString.Add("response_type", "token");
            queryString.Add("state", csrfToken);
            requestUri.Query = queryString.ToString();

            Application.OpenURL(requestUri.ToString());

            for (float t = 180; t > 0; t -= Time.deltaTime) //180 seconds timeout
            {
                if (response != null)
                {
                    yield return 0;
                    AuthenticationResult result = new AuthenticationResult();
                    result.originatingRequest = request;

                    if (response["error"] != null)
                    {
                        result.error = response["error"];
                        result.errorDescription = response["error_description"];
                    }
                    else if (response["state"] != csrfToken)
                    {
                        result.error = "invalid_csrf_token";
                        result.errorDescription = "The CSRF Token was invalid. The authentication has likely been tampered with. Please try authenticating again.";
                    }
                    else if (response["access_token"] != null)
                    {
                        //success
                        result.isSuccessful = true;
                        result.accessToken = response["access_token"];
                    }

                    yield return result.UpdateTokenMetadata(false);

                    //we will store the token in our current instance
                    LastAuthenticationResult = result;

                    if (additionalCallback != null) additionalCallback.Invoke(result);
                    OnAuthenticationFinished.Invoke(result);

                    yield break;
                }
                yield return 0;
            }

            //Landing here means something went wrong
            cbListener.Cancel();
            if (additionalCallback != null) additionalCallback.Invoke(null);
            OnAuthenticationFinished.Invoke(null);
        }

        IEnumerator AuthorizationCodeFlow(AuthenticationRequest request, Action<AuthenticationResult> additionalCallback)
        {
            Log("Entering " + UsedAuthenticationFlow.ToString() + " coroutine...");

            if (request == null) throw new NullReferenceException("A request object must be given");


            NameValueCollection response = null;
            Log("Starting Callback Listener");
            cbListener = new AuthCallbackListener(CallbackServerPort, (pResponse) =>
            {
                response = System.Web.HttpUtility.ParseQueryString("?" + pResponse.Substring(1));
            }, CallbackHTMLContent);

            UriBuilder requestUri = new UriBuilder("https://id.twitch.tv/oauth2/authorize");
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString("");
            queryString.Add("client_id", ClientID);
            queryString.Add("force_verify", request.forceVerify.ToString().ToLower());
            queryString.Add("redirect_uri", "http://localhost:" + CallbackServerPort);
            queryString.Add("scope", string.Join(" ", request.requestedScopes));
            queryString.Add("response_type", "code");
            queryString.Add("state", csrfToken);
            requestUri.Query = queryString.ToString();

            Log("Opening browser to authenticate the user");
            Application.OpenURL(requestUri.ToString());

            for (float t = 180; t > 0; t -= Time.deltaTime) //180 seconds timeout
            {
                if (response != null)
                {
                    yield return 0;
                    Log("Callback was called");
                    AuthenticationResult result = new AuthenticationResult();
                    result.originatingRequest = request;

                    if (response["error"] != null)
                    {
                        Log("Callback received an error: " + response["error_description"]);
                        result.error = response["error"];
                        result.errorDescription = response["error_description"];
                    }
                    else if (response["state"] != csrfToken)
                    {
                        Log("CSRF token was invalid");
                        result.error = "invalid_csrf_token";
                        result.errorDescription = "The CSRF Token was invalid. The authentication has likely been tampered with. Please try authenticating again.";
                    }
                    else if (response["code"] != null)
                    {
                        //success in stage one - we got an authorization code. Now we need to use it to request the access token
                        Log("We received an authorization code, requesting access token...");
                        UnityWebRequest tkRequest;
                        if (UsedAuthenticationFlow == AuthenticationFlow.AuthorizationCodeFlow)
                        {
                            WWWForm requestData = new WWWForm();
                            requestData.AddField("client_id", ClientID);
                            requestData.AddField("client_secret", ClientSecret);
                            requestData.AddField("code", response["code"]);
                            requestData.AddField("grant_type", "authorization_code");
                            requestData.AddField("redirect_uri", "http://localhost:" + CallbackServerPort);
                            tkRequest = UnityWebRequest.Post("https://id.twitch.tv/oauth2/token", requestData);

                            Log("Sending request to twitch");
                            yield return tkRequest.SendWebRequest();
                        }
                        else
                        {
                            UriBuilder assistantUri = new UriBuilder(AssistantScriptURL);
                            NameValueCollection assistantQueryString = System.Web.HttpUtility.ParseQueryString("");
                            assistantQueryString.Add("ident", UniqueIdentifier);
                            assistantQueryString.Add("action", "get");
                            assistantQueryString.Add("code", response["code"]);
                            assistantQueryString.Add("port", CallbackServerPort.ToString());
                            assistantUri.Query = assistantQueryString.ToString();

                            tkRequest = UnityWebRequest.Get(assistantUri.Uri);
                            if (ServerParole != null && ServerParole.Length > 0)
                            {
                                if (assistantUri.Scheme != "https") throw new InvalidOperationException("You must not use a parole in an unencrypted connection. Use HTTPS or remove the Server Parole.");
                                tkRequest.SetRequestHeader("X-Parole", ServerParole);
                            }

                            Log("Sending request to our assistant server using uri " + assistantUri.Uri);
                            yield return tkRequest.SendWebRequest();
                        }

                        if (tkRequest.result == UnityWebRequest.Result.Success && tkRequest.responseCode == 200)
                        {
                            Log("Request finished with success");
                            AccessTokenResponse tkResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(tkRequest.downloadHandler.text);

                            if (AutoRefreshToken)
                            {
                                if (UniqueIdentifier.Length < 1) throw new InvalidOperationException("Cannot store / auto refresh a token without a unique identifier");
                                else
                                {
                                    PlayerPrefs.SetString("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".RT", tkResponse.refresh_token);
                                    PlayerPrefs.SetString("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".AT", tkResponse.access_token);
                                    PlayerPrefs.Save();

                                    float timeUntilRefresh = Mathf.Clamp(tkResponse.expires_in * UnityEngine.Random.Range(0.960001f, 0.993f), tkResponse.expires_in - 850, tkResponse.expires_in - 30);
                                    Log("Scheduling next refresh in about " + Mathf.FloorToInt(timeUntilRefresh) + " seconds");
                                    nextTokenRefresh = Time.realtimeSinceStartup + timeUntilRefresh; //we will randomize this a bit again to make sure we won't ahmmer twitch if many instances are running in parallel
                                }
                            }

                            //success
                            result.isSuccessful = true;
                            result.accessToken = tkResponse.access_token;

                            Log("Updating token metadata");
                            yield return result.UpdateTokenMetadata(false);

                            //we will store the token in our current instance
                            LastAuthenticationResult = result;

                            Log("Invoking callback");
                            if (additionalCallback != null) additionalCallback.Invoke(result);
                            OnAuthenticationFinished.Invoke(result);
                        }
                        else
                        {
                            Debug.LogError("Encountered an error while requesting the access token from twitch: " + tkRequest.downloadHandler.text);
                            result.error = "unknown";
                            result.errorDescription = "Encountered an error while requesting the access token from twitch: " + tkRequest.downloadHandler.text;
                            if (additionalCallback != null) additionalCallback.Invoke(result);
                            OnAuthenticationFinished.Invoke(result);
                        }
                    }
                    else
                    {
                        Debug.LogError("Encountered an error while retrieving the authorization code from twitch. Raw response: " + response);
                        result.error = "unknown";
                        result.errorDescription = "Encountered an error while requesting the access token from twitch. Raw response: " + response;
                        if (additionalCallback != null) additionalCallback.Invoke(result);
                        OnAuthenticationFinished.Invoke(result);
                    }

                    yield break;
                }
                yield return 0;
            }

            //Landing here means something went wrong
            cbListener.Cancel();
            if (additionalCallback != null) additionalCallback.Invoke(null);
            OnAuthenticationFinished.Invoke(null);
        }


        IEnumerator RefreshCurrentToken(string refreshTokenToUse, string lastKnownAccessToken, AuthenticationResult.Initiator authSource, Action<AuthenticationResult> additionalCallback)
        {
            Log("Entering token refresh coroutine");

            if (authSource == AuthenticationResult.Initiator.InstantRefresh) yield return 0; //this is here to make sure all scripts are initialized in case of the initial refresh

            AuthenticationResult result = new AuthenticationResult();
            result.initiator = authSource;
            result.refreshToken = refreshTokenToUse;
            result.accessToken = lastKnownAccessToken;

            if (lastKnownAccessToken != null && lastKnownAccessToken.Length > 1 && !DisableSafetyLimits)
            {

                Log("Updating metadata for given access token...");
                yield return result.UpdateTokenMetadata(true);
                yield return 0;

                if (result.isSuccessful && result.accessTokenMetadata.expires_in > 900)
                {
                    Log("Done refreshing metadata. Current validity is about " + result.accessTokenMetadata?.expires_in + " seconds");

                    //this token still has a long validity. We will Re-Use it to prevent us from exceeding the 50 tokens limit
                    Log("As current validity is more than 900 seconds we will not refresh the token, instead we are going to fire a successful refresh event with the old token.");
                    result.isSuccessful = true;
                    nextTokenRefresh = Time.realtimeSinceStartup + (result.accessTokenMetadata.expires_in * 0.95f);
                    LastAuthenticationResult = result;
                    OnAuthenticationFinished.Invoke(result);
                    yield break;
                }
                else if (!result.isSuccessful)
                {
                    Log("Could not refresh metadata: Token was already invalid");
                }
                yield return 0;
            }
            else if (DisableSafetyLimits)
            {
                Log("Safety limit is disabled. This could cause death to cute kitties.");
            }

            Log("Token needs to be refreshed, initializing web request");
            UnityWebRequest tkRequest;
            if (UsedAuthenticationFlow == AuthenticationFlow.AuthorizationCodeFlow)
            {
                WWWForm requestData = new WWWForm();
                requestData.AddField("client_id", ClientID);
                requestData.AddField("client_secret", ClientSecret);
                requestData.AddField("grant_type", "refresh_token");
                requestData.AddField("refresh_token", refreshTokenToUse);
                tkRequest = UnityWebRequest.Post("https://id.twitch.tv/oauth2/token", requestData);
                tkRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                yield return new WaitForSecondsRealtime(0.2f); //we noticed some weird behaviour on twitch's side without this minimal delay, maybe some kind of rate limiting

                Log("Sending token refresh request to twitch");
                yield return tkRequest.SendWebRequest();
            }
            else
            {
                UriBuilder assistantUri = new UriBuilder(AssistantScriptURL);
                NameValueCollection assistantQueryString = System.Web.HttpUtility.ParseQueryString("");
                assistantQueryString.Add("ident", UniqueIdentifier);
                assistantQueryString.Add("action", "refresh");
                assistantQueryString.Add("code", refreshTokenToUse);
                assistantQueryString.Add("port", CallbackServerPort.ToString());
                assistantUri.Query = assistantQueryString.ToString();

                tkRequest = UnityWebRequest.Get(assistantUri.Uri);
                if (ServerParole != null && ServerParole.Length > 0)
                {
                    if (assistantUri.Scheme != "https") throw new InvalidOperationException("You must not use a parole in an unencrypted connection. Use HTTPS or remove the Server Parole.");
                    tkRequest.SetRequestHeader("X-Parole", ServerParole);
                }

                Log("Sending request to our assistant server");
                yield return tkRequest.SendWebRequest();
            }

            if (tkRequest.result == UnityWebRequest.Result.Success && tkRequest.responseCode == 200)
            {
                Log("Token refresh seems to have finished successfully");
                AccessTokenResponse tkResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(tkRequest.downloadHandler.text);

                if (AutoRefreshToken)
                {
                    if (UniqueIdentifier.Length < 1) throw new InvalidOperationException("Cannot store / auto refresh a token without a unique identifier");
                    else
                    {
                        PlayerPrefs.SetString("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".RT", tkResponse.refresh_token);
                        PlayerPrefs.SetString("Firesplash.TwitchAuthentication.Store." + UniqueIdentifier + ".AT", tkResponse.access_token);
                        PlayerPrefs.Save();

                        float timeUntilRefresh = Mathf.Clamp(tkResponse.expires_in * UnityEngine.Random.Range(0.960001f, 0.993f), tkResponse.expires_in - 850, tkResponse.expires_in - 30);
                        Log("Scheduling next refresh in about " + Mathf.FloorToInt(timeUntilRefresh) + " seconds");
                        nextTokenRefresh = Time.realtimeSinceStartup + timeUntilRefresh; //we will randomize this a bit again to make sure we won't hammer twitch if many instances are running in parallel
                    }
                }

                //success
                result.isSuccessful = true;
                result.accessToken = tkResponse.access_token;

                Log("Updating token metadata after refresh");
                yield return result.UpdateTokenMetadata(false);

                //we will store the token in our current instance
                LastAuthenticationResult = result;

                Log("Refresh has finished. Invoking callback");
                if (additionalCallback != null) additionalCallback.Invoke(result);
                OnAuthenticationFinished.Invoke(result);
            }
            else
            {
                Debug.LogError("Encountered an error while refreshing the access token: " + tkRequest.downloadHandler.text);
                result.error = "unknown";
                result.errorDescription = "Encountered an error while refreshing the access token: " + tkRequest.downloadHandler.text;
                OnAuthenticationFinished.Invoke(result);
            }
        }

    }
}
