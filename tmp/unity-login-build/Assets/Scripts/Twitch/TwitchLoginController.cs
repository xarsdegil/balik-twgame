using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FishingGame.Twitch
{
    public sealed class TwitchLoginController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameSceneName = "SampleScene";

        [Header("Cloudflare OAuth")]
        [Tooltip("Public Twitch Client ID. Client secret yalnızca Cloudflare Worker'da kalır.")]
        [SerializeField] private string twitchClientId = "ulltj8z49x0rvmi94624d6obf0erys";
        [SerializeField] private string oauthWorkerBaseUrl =
            "https://balloon-twitch-auth.xarsleech.workers.dev";

        [Header("UI")]
        [SerializeField] private GameObject loadingGroup;
        [SerializeField] private GameObject loginGroup;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text statusText;

        private TwitchAuthService authService;
        private bool loginInProgress;

        private void Start()
        {
            authService = TwitchAuthService.Instance;
            if (authService == null)
            {
                authService = new GameObject("Twitch Authentication")
                    .AddComponent<TwitchAuthService>();
            }

            authService.Configure(twitchClientId, oauthWorkerBaseUrl);
            authService.StatusChanged += SetStatus;
            authService.Authenticated += OnAuthenticated;
            authService.AuthenticationFailed += OnAuthenticationFailed;
            loginButton.onClick.AddListener(BeginLogin);

            SetLoading(true);
            StartCoroutine(CheckSavedLogin());
        }

        private void OnDestroy()
        {
            if (authService == null)
            {
                return;
            }

            authService.StatusChanged -= SetStatus;
            authService.Authenticated -= OnAuthenticated;
            authService.AuthenticationFailed -= OnAuthenticationFailed;
        }

        private IEnumerator CheckSavedLogin()
        {
            SetStatus("Kaydedilmiş Twitch oturumu kontrol ediliyor...");
            bool valid = false;
            yield return authService.RestoreSavedSession(result => valid = result);
            if (valid)
            {
                OpenGame();
                yield break;
            }

            SetLoading(false);
            SetStatus("Yayına bağlanmak için Twitch hesabınla giriş yap.");
        }

        private void BeginLogin()
        {
            loginInProgress = true;
            loginButton.interactable = false;
            authService.BeginBrowserLogin();
        }

        private void OnAuthenticated(TwitchAuthSession session)
        {
            loginInProgress = false;
            SetStatus("Hoş geldin, " + session.login + ". Oyun açılıyor...");
            OpenGame();
        }

        private void OnAuthenticationFailed(string message)
        {
            loginInProgress = false;
            SetStatus(message);
            loginButton.interactable = true;
        }

        private void OpenGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void SetLoading(bool loading)
        {
            if (loadingGroup != null)
            {
                loadingGroup.SetActive(loading);
            }

            if (loginGroup != null)
            {
                loginGroup.SetActive(!loading);
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }

            if (loginButton != null && authService != null &&
                !authService.HasAuthenticatedSession && !loginInProgress)
            {
                loginButton.interactable = true;
            }
        }
    }
}
