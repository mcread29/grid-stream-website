using System.Diagnostics;
using Firesplash.UnityAssets.TwitchAuthentication.DataTypes;
using System.Collections.Generic;
using System.IO;

namespace Firesplash.UnityAssets.TwitchAuthentication
{
    public class AuthenticationRequest
    {
        /// <summary>
        /// Setting this to true will make sure thate user is asked for verification/login. This allows the user to authenticate with another twitch account if desired. Especially useful for connecting Bot-Accounts.
        /// </summary>
        public bool forceVerify = false;

        /// <summary>
        /// The list of scopes requested by this authentication request
        /// </summary>
        internal List<string> requestedScopes;

        #region constructors
        /// <summary>
        /// Create a new AuthenticationRequest and specify requested scopes right now (as a Set)
        /// </summary>
        /// <param name="forceVerify">Shall the user receive a propmpt by twitch even if he already accepted? This is useful for authenticating bot accounts.</param>
        /// <param name="requestedScopes">A scopeSet to be requested</param>
        public AuthenticationRequest(bool forceVerify, ScopeSet requestedScopes)
        {
            this.requestedScopes = new List<string>();
            this.forceVerify = forceVerify;
            this.RequestScope(requestedScopes);
        }

        /// <summary>
        /// Create a new AuthenticationRequest without specifying a scope set
        /// </summary>
        /// <param name="forceVerify">Shall the user receive a propmpt by twitch even if he already accepted? This is useful for authenticating bot accounts.</param>
        public AuthenticationRequest(bool forceVerify)
        {
            this.requestedScopes = new List<string>();
            this.forceVerify = forceVerify;
        }


        /// <summary>
        /// Create a new AuthenticationRequest without specific scopes but including metadata. This is used for authentication only without requiring any acces. You can also add scopes using "RequestScope" after creating the request object.
        /// </summary>
        public AuthenticationRequest()
        {
            this.requestedScopes = new List<string>();
        }
        #endregion

        #region scope management
        /// <summary>
        /// Manually adds a requested scope to this request using the scope name from twitch.
        /// </summary>
        /// <param name="scope">A valid scope name string from https://dev.twitch.tv/docs/authentication/scopes</param>
        public void RequestScope(string scope)
        {
            if (scope.Length < 1 || scope.Length > 60 || scope.Contains(" ") || scope.Contains(",")) throw new InvalidDataException("Plausibility check: You must specify exactly one valid scope name");
            if (requestedScopes.Contains(scope)) return;
            requestedScopes.Add(scope);
        }

        /// <summary>
        /// Adds requested scopes to this request using a predefined purpose (ScopeSet).
        /// </summary>
        /// <param name="set">The ScopeSet you want to add to this request</param>
        public void RequestScope(ScopeSet set)
        {
            switch (set)
            {
                case ScopeSet.DevAnalyticsRead:
                    RequestScope("analytics:read:extensions");
                    RequestScope("analytics:read:games");
                    break;

                case ScopeSet.Whispers:
                    RequestScope("whispers:read");
                    RequestScope("whispers:edit");
                    break;

                case ScopeSet.ChannelModerationFull:
                    RequestScope("moderator:manage:banned_users");
                    RequestScope("moderator:manage:blocked_terms");
                    RequestScope("moderator:manage:automod");
                    RequestScope("moderator:manage:automod_settings");
                    RequestScope("moderator:manage:chat_settings");
                    goto case ScopeSet.ChannelModerationLight;

                case ScopeSet.ChannelModerationLight:
                    RequestScope("moderation:read");
                    RequestScope("moderator:read:automod_settings");
                    RequestScope("moderator:read:chat_settings");
                    RequestScope("moderator:read:blocked_terms");
                    goto case ScopeSet.ChatModerate;

                case ScopeSet.ChatModerate:
                    RequestScope("channel:moderate");
                    goto case ScopeSet.ChatReadWrite;

                case ScopeSet.ChatReadWrite:
                    RequestScope("chat:edit");
                    goto case ScopeSet.ChatReadOnly;

                case ScopeSet.ChatReadOnly:
                    RequestScope("chat:read");
                    break;
            }
        }


        /// <summary>
        /// Contains some commonly used combinations of scopes. You can use specific single scopes by specifying their name (as a string) instead of a set
        /// </summary>
        public enum ScopeSet
        {
            /// <summary>
            /// Read-Access to all developer analytics
            /// Contains analytics:read:extensions, analytics:read:games
            /// </summary>
            DevAnalyticsRead,

            /// <summary>
            /// Read and Write-Access to the users whisper messages
            /// Contains whispers:read, whispers:edit
            /// </summary>
            Whispers,

            /// <summary>
            /// Read-Access to Twitch's Chat impersonating the user
            /// Contains chat:read
            /// </summary>
            ChatReadOnly,

            /// <summary>
            /// Write-Access to Twitch's Chat impersonating the user
            /// Contains chat:read, chat:edit
            /// </summary>
            ChatReadWrite,

            /// <summary>
            /// Enables usage of (only) chat moderation commands
            /// Contains chat:read, chat:edit, channel:moderate
            /// </summary>
            ChatModerate,

            /// <summary>
            /// Read channel moderation data via API and fully moderate the chat
            /// Contains chat:read, chat:edit, channel:moderate, moderation:read, moderator:read:automod_settings, moderator:read:chat_settings, moderator:read:blocked_terms
            /// </summary>
            ChannelModerationLight,

            /// <summary>
            /// Enables to access all moderation APIs as well as full chat access including moderation
            /// Contains chat:read, chat:edit, channel:moderate, moderation:read, moderator:read:automod_settings, moderator:read:chat_settings, moderator:read:blocked_terms, moderator:manage:banned_users, moderator:manage:blocked_terms, moderator:manage:automod, moderator:manage:automod_settings, moderator:manage:chat_settings
            /// </summary>
            ChannelModerationFull
        }
        #endregion
    }
}
