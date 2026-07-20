using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace FishingGame.Twitch
{
    public sealed class TwitchAuthService : MonoBehaviour
    {
        private const string ValidateEndpoint = "https://id.twitch.tv/oauth2/validate";
        private static readonly string[] RequiredScopes = { "chat:read", "chat:edit" };

        public static TwitchAuthService Instance { get; private set; }
        public TwitchAuthSession Session { get; private set; }
        public bool HasAuthenticatedSession => Session != null && Session.HasAccessToken;

        public event Action<string> StatusChanged;
        public event Action<TwitchAuthSession> Authenticated;
        public event Action<string> AuthenticationFailed;

        private string clientId;
        private string oauthWorkerBaseUrl;
        private Coroutine activeAuthentication;
        private Coroutine sessionValidation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Configure(string publicClientId, string workerBaseUrl)
        {
            clientId = (publicClientId ?? string.Empty).Trim();
            oauthWorkerBaseUrl = (workerBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        }

        public IEnumerator RestoreSavedSession(Action<bool> completed)
        {
            if (!TwitchTokenStore.TryLoad(out TwitchAuthSession saved))
            {
                completed?.Invoke(false);
                yield break;
            }

            Session = saved;
            bool valid = false;
            yield return ValidateCurrentSession(result => valid = result);
            if (!valid && Session.HasRefreshToken)
            {
                bool refreshed = false;
                yield return RefreshSession(result => refreshed = result);
                if (refreshed)
                {
                    yield return ValidateCurrentSession(result => valid = result);
                }
            }

            if (!valid)
            {
                Session = null;
                TwitchTokenStore.Delete();
            }
            else
            {
                StartHourlyValidation();
                EnsureChatConnected();
            }

            completed?.Invoke(valid);
        }

        public void BeginBrowserLogin()
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                Fail("Twitch Client ID is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(oauthWorkerBaseUrl))
            {
                Fail("Cloudflare Worker URL is not configured.");
                return;
            }

            if (activeAuthentication != null)
            {
                StopCoroutine(activeAuthentication);
            }

            activeAuthentication = StartCoroutine(BrowserLoginFlow());
        }

        public void SignOut()
        {
            if (activeAuthentication != null)
            {
                StopCoroutine(activeAuthentication);
            }

            if (sessionValidation != null)
            {
                StopCoroutine(sessionValidation);
            }

            activeAuthentication = null;
            sessionValidation = null;
            Session = null;
            TwitchTokenStore.Delete();
            TwitchChatClient chat = GetComponent<TwitchChatClient>();
            if (chat != null)
            {
                chat.Disconnect();
            }
        }

        private IEnumerator BrowserLoginFlow()
        {
            TwitchTokenStore.RememberClientId(clientId);
            SetStatus("Preparing a secure connection...");

            StartAuthorizationRequest body = new StartAuthorizationRequest
            {
                successTitle = "Twitch connection complete",
                successMessage = "You can close this window and return to the game.",
                failureTitle = "Twitch connection failed",
                failureMessage = "Return to the game and try again."
            };

            using UnityWebRequest request = CreateJsonPost(
                oauthWorkerBaseUrl + "/oauth/start",
                JsonUtility.ToJson(body));
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Fail("Could not connect to the Cloudflare Worker.");
                yield break;
            }

            StartAuthorizationResponse start =
                JsonUtility.FromJson<StartAuthorizationResponse>(request.downloadHandler.text);
            if (start == null || string.IsNullOrWhiteSpace(start.transactionId) ||
                string.IsNullOrWhiteSpace(start.pollSecret) ||
                string.IsNullOrWhiteSpace(start.authorizationUrl))
            {
                Fail("The Worker returned an invalid OAuth response.");
                yield break;
            }

            SetStatus("Authorize Twitch access in your browser.");
            Application.OpenURL(start.authorizationUrl);

            float deadline = Time.realtimeSinceStartup + 600f;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(1f);

                StatusRequest statusBody = new StatusRequest
                {
                    transactionId = start.transactionId,
                    pollSecret = start.pollSecret
                };
                using UnityWebRequest statusRequest = CreateJsonPost(
                    oauthWorkerBaseUrl + "/oauth/status",
                    JsonUtility.ToJson(statusBody));
                yield return statusRequest.SendWebRequest();

                OAuthStatusResponse status = JsonUtility.FromJson<OAuthStatusResponse>(
                    statusRequest.downloadHandler != null
                        ? statusRequest.downloadHandler.text
                        : string.Empty);
                if (statusRequest.responseCode == 202 ||
                    (status != null && status.status == "pending"))
                {
                    continue;
                }

                if (statusRequest.result == UnityWebRequest.Result.Success &&
                    status != null && status.status == "complete" && status.token != null &&
                    !string.IsNullOrWhiteSpace(status.token.access_token))
                {
                    Session = new TwitchAuthSession
                    {
                        clientId = clientId,
                        accessToken = status.token.access_token,
                        refreshToken = status.token.refresh_token,
                        scopes = status.token.scope,
                        savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    bool valid = false;
                    yield return ValidateCurrentSession(result => valid = result);
                    if (!valid)
                    {
                        Fail("The Twitch session could not be validated.");
                        yield break;
                    }

                    TwitchTokenStore.Save(Session);
                    StartHourlyValidation();
                    EnsureChatConnected();
                    activeAuthentication = null;
                    SetStatus("Twitch connection complete.");
                    Authenticated?.Invoke(Session);
                    yield break;
                }

                Fail("Twitch login could not be completed.");
                yield break;
            }

            Fail("Twitch login timed out. Please try again.");
        }

        private IEnumerator ValidateCurrentSession(Action<bool> completed)
        {
            if (Session == null || !Session.HasAccessToken)
            {
                completed?.Invoke(false);
                yield break;
            }

            using UnityWebRequest request = UnityWebRequest.Get(ValidateEndpoint);
            request.SetRequestHeader("Authorization", "OAuth " + Session.accessToken);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false);
                yield break;
            }

            ValidateResponse response =
                JsonUtility.FromJson<ValidateResponse>(request.downloadHandler.text);
            string[] grantedScopes = response != null
                ? response.scopes ?? Array.Empty<string>()
                : Array.Empty<string>();
            bool hasRequiredScopes = Array.TrueForAll(
                RequiredScopes,
                required => Array.Exists(grantedScopes, granted => granted == required));
            bool valid = response != null &&
                         !string.IsNullOrWhiteSpace(response.login) &&
                         response.client_id == Session.clientId &&
                         hasRequiredScopes;
            if (valid)
            {
                Session.login = response.login;
                Session.userId = response.user_id;
                Session.scopes = response.scopes;
                TwitchTokenStore.Save(Session);
            }

            completed?.Invoke(valid);
        }

        private IEnumerator RefreshSession(Action<bool> completed)
        {
            if (Session == null || !Session.HasRefreshToken ||
                string.IsNullOrWhiteSpace(oauthWorkerBaseUrl))
            {
                completed?.Invoke(false);
                yield break;
            }

            RefreshRequest body = new RefreshRequest { refreshToken = Session.refreshToken };
            using UnityWebRequest request = CreateJsonPost(
                oauthWorkerBaseUrl + "/oauth/refresh",
                JsonUtility.ToJson(body));
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(false);
                yield break;
            }

            TokenResponse token = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
            if (token == null || string.IsNullOrWhiteSpace(token.access_token))
            {
                completed?.Invoke(false);
                yield break;
            }

            Session.accessToken = token.access_token;
            if (!string.IsNullOrWhiteSpace(token.refresh_token))
            {
                Session.refreshToken = token.refresh_token;
            }

            Session.scopes = token.scope;
            Session.savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            TwitchTokenStore.Save(Session);
            completed?.Invoke(true);
        }

        private void StartHourlyValidation()
        {
            if (sessionValidation != null)
            {
                StopCoroutine(sessionValidation);
            }

            sessionValidation = StartCoroutine(HourlyValidationLoop());
        }

        private IEnumerator HourlyValidationLoop()
        {
            while (Session != null)
            {
                yield return new WaitForSecondsRealtime(3600f);
                bool valid = false;
                yield return ValidateCurrentSession(result => valid = result);
                if (!valid && Session != null && Session.HasRefreshToken)
                {
                    bool refreshed = false;
                    yield return RefreshSession(result => refreshed = result);
                    if (refreshed)
                    {
                        yield return ValidateCurrentSession(result => valid = result);
                    }
                }

                if (!valid)
                {
                    Session = null;
                    TwitchTokenStore.Delete();
                    AuthenticationFailed?.Invoke("The Twitch session has expired.");
                    sessionValidation = null;
                    if (SceneManager.GetActiveScene().name != "TwitchLogin")
                    {
                        SceneManager.LoadScene("TwitchLogin");
                    }

                    yield break;
                }
            }
        }

        private void SetStatus(string value)
        {
            StatusChanged?.Invoke(value);
        }

        private void EnsureChatConnected()
        {
            if (Session == null || !Session.HasAccessToken)
            {
                return;
            }

            TwitchChatClient chat = GetComponent<TwitchChatClient>();
            if (chat == null)
            {
                chat = gameObject.AddComponent<TwitchChatClient>();
            }

            chat.Connect(Session);
        }

        private void Fail(string message)
        {
            activeAuthentication = null;
            SetStatus(message);
            AuthenticationFailed?.Invoke(message);
        }

        private static UnityWebRequest CreateJsonPost(string url, string json)
        {
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        // JsonUtility populates these fields through reflection.
#pragma warning disable 0649
        [Serializable]
        private sealed class StartAuthorizationRequest
        {
            public string successTitle;
            public string successMessage;
            public string failureTitle;
            public string failureMessage;
        }

        [Serializable]
        private sealed class StartAuthorizationResponse
        {
            public string transactionId;
            public string pollSecret;
            public string authorizationUrl;
        }

        [Serializable]
        private sealed class StatusRequest
        {
            public string transactionId;
            public string pollSecret;
        }

        [Serializable]
        private sealed class OAuthStatusResponse
        {
            public string status;
            public string error;
            public TokenResponse token;
        }

        [Serializable]
        private sealed class RefreshRequest
        {
            public string refreshToken;
        }

        [Serializable]
        private sealed class TokenResponse
        {
            public string access_token;
            public int expires_in;
            public string refresh_token;
            public string[] scope;
            public string token_type;
        }

        [Serializable]
        private sealed class ValidateResponse
        {
            public string client_id;
            public string login;
            public string[] scopes;
            public string user_id;
            public int expires_in;
        }
#pragma warning restore 0649
    }
}
