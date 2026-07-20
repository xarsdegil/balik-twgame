using UnityEngine;

namespace FishingGame.Twitch
{
    public sealed class TwitchLoginMotion : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform islandArt;
        [SerializeField] private RectTransform loginButton;
        [SerializeField] private float entranceDuration = 0.7f;
        [SerializeField] private float islandFloatPixels = 9f;
        [SerializeField] private float islandFloatSpeed = 0.8f;

        private Vector2 islandStart;
        private Vector3 buttonStartScale;
        private float elapsed;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (islandArt != null)
            {
                islandStart = islandArt.anchoredPosition;
            }

            if (loginButton != null)
            {
                buttonStartScale = loginButton.localScale;
            }
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, entranceDuration));
            }

            if (islandArt != null)
            {
                float offset = Mathf.Sin(elapsed * islandFloatSpeed * Mathf.PI * 2f) *
                               islandFloatPixels;
                islandArt.anchoredPosition = islandStart + Vector2.up * offset;
            }

            if (loginButton != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 2.2f) * 0.018f;
                loginButton.localScale = buttonStartScale * pulse;
            }
        }
    }
}
