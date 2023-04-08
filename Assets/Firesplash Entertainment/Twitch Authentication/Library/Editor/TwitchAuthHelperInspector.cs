using System;
using UnityEditor;
using UnityEngine;

namespace Firesplash.UnityAssets.TwitchAuthentication.Internal
{


    [CustomEditor(typeof(TwitchAuthenticationHelper))]
    [CanEditMultipleObjects]
    public class TwitchAuthHelperInspector : Editor
    {
        bool isFlowInfoUnfolded = false, isAdvancedUnfolded = false;

        public override void OnInspectorGUI()
        {
            GUIStyle s_topheader = new GUIStyle(GUI.skin.label);
            s_topheader.fontSize = 25;
            s_topheader.fontStyle = FontStyle.Bold;

            serializedObject.Update();

            EditorGUILayout.TextField("Twitch Authentication Helper", s_topheader);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Twitch gives us two possible methods to aquire a user scoped token. Unfortunately none of both is a best practice solution for standalone applications without having a server assisting because implicit requires javascript enabled on user's browser and authorization flow may leak your secret. If possible, you should use server assisted or implicit flow. Unfold the boxes below for more information.", MessageType.Info);

            isFlowInfoUnfolded = EditorGUILayout.Foldout(isFlowInfoUnfolded, "More information on available flows");
            if (isFlowInfoUnfolded)
            {
                EditorGUILayout.LabelField("Implicit Grant Flow");
                EditorGUILayout.HelpBox("This is the easiest flow. No credentials are leaked.", MessageType.Info);
                EditorGUILayout.HelpBox("If the user has disabled JavaScript or is using a script blocker (should be a rare case), he might not be able to login or need to manually change a char in the address bar after accepting the account link on twitch.\nTokens aquired via this flow can not be refreshed, the user has to go through the flow again when he needs a new token.", MessageType.Warning);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Authorization Code Flow");
                EditorGUILayout.HelpBox("This flow allows refreshing expired tokens without user intervention and also works with disabled JavaScript", MessageType.Info);
                EditorGUILayout.HelpBox("You need to include your application secret from the twitch developer dashboard in your build which is reverse-engineerable without requiring extensive knowledge. This is considered a bad practice and a security risk. If using this flow, you should at least use some obfuscation asset to make it harder to find the token.", MessageType.Warning);
                           EditorGUILayout.Space();
                EditorGUILayout.LabelField("Server Assisted Authorization Code Flow");
                EditorGUILayout.HelpBox("Same as 'Authorization Code Flow' but does not require to deliver the secret with your application, so it is much more secure.", MessageType.Info);
                EditorGUILayout.HelpBox("Requires hosting a PHP-Script on a server or webspace. This server should be SSL-enabled.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            SerializedProperty authFlow = serializedObject.FindProperty("UsedAuthenticationFlow");
            SerializedProperty twitchSecret = serializedObject.FindProperty("ClientSecret");
            EditorGUILayout.PropertyField(authFlow);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("The following data can be found in the developer console");
            SerializedProperty clientID = serializedObject.FindProperty("ClientID");
            EditorGUILayout.PropertyField(clientID);
            if (clientID.stringValue.Length < 10)
            {
                EditorGUILayout.HelpBox("The ClientID is a string consisting of random characters which can be found in the Twitch Developer Console after creating your application there.", MessageType.Info);
            }

            if (authFlow.intValue == 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This flow requires providing your App's ClientSecret (from Twitch Developer Console) here. It will be stored unencrypted and will be contained in the build (unencrypted). It can be easily reverse-engineered if not secured otherwise. If possible, you should preferr the Server Assisted flow.", MessageType.Warning);
                EditorGUILayout.PropertyField(twitchSecret);
            }
            else if (twitchSecret.stringValue.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("You have selected a flow that does not require the ClientSecret so please remove it from the box below to secure your build.", MessageType.Error);
                EditorGUILayout.PropertyField(twitchSecret);
            }

            if (authFlow.intValue == 1 || authFlow.intValue == 2)
            {
                SerializedProperty autoRefresh = serializedObject.FindProperty("AutoRefreshToken");
                EditorGUILayout.PropertyField(autoRefresh);
                if (autoRefresh.boolValue)
                {
                    EditorGUILayout.HelpBox("This will cause the user's refresh token to be stored unencrypted in the registry. Please note, that enabled AutoRefresh will cause a failed result callback if the user was never logged in. You can filter for error no_refresh_token", MessageType.Info);
                }

                SerializedProperty instanceIdentifier = serializedObject.FindProperty("UniqueIdentifier");
                EditorGUILayout.PropertyField(instanceIdentifier);
            }

            if (authFlow.intValue == 2)
            {
                SerializedProperty scriptUri = serializedObject.FindProperty("AssistantScriptURL");
                EditorGUILayout.PropertyField(scriptUri);
                Uri uriTest;
                try
                {
                    uriTest = new Uri(scriptUri.stringValue);
                }
                catch
                {
                    uriTest = null;
                }
                if (uriTest == null || (uriTest.Scheme != "http" && uriTest.Scheme != "https") || uriTest.Query.Length > 0 || uriTest.Fragment.Length > 0) EditorGUILayout.HelpBox("The given Server-side script URL seems to be invalid. It must start with http:// or https:// (required for parole) and must not contain any hash- or query string parameters", MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            SerializedProperty port = serializedObject.FindProperty("CallbackServerPort");
            EditorGUILayout.LabelField("You can set this to any value between 1025 and 65535, in most cases the default plays well.");
            EditorGUILayout.PropertyField(port);
            EditorGUILayout.HelpBox("In Twitch Developer Console, set the callback redirect url to this value with no trailing slash: http://localhost:" + port.intValue, MessageType.Info);
            
            isAdvancedUnfolded = EditorGUILayout.Foldout(isAdvancedUnfolded, "Advanced Configuration Options");
            if (isAdvancedUnfolded)
            {
                EditorGUILayout.LabelField("Please read the documentation hints before changing these.");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DisableSafetyLimits"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableDebugOutput"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ServerParole"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CallbackHTMLContent"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
