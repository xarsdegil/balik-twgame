#if UNITY_EDITOR
using System.IO;
using System.Linq;
using FishingGame.Twitch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FishingGame.Editor
{
    public static class TwitchLoginSceneBuilder
    {
        private const string LoginScenePath = "Assets/Scenes/TwitchLogin.unity";
        private const string GameScenePath = "Assets/Scenes/SampleScene.unity";
        private const string IslandArtPath = "Assets/Assets/exp.png";
        private const string BuilderPath = "Assets/Editor/TwitchLoginSceneBuilder.cs";
        private const string TwitchClientId = "ulltj8z49x0rvmi94624d6obf0erys";
        private const string OAuthWorkerUrl =
            "https://balloon-twitch-auth.xarsleech.workers.dev";

        [InitializeOnLoadMethod]
        private static void BuildMissingLoginScene()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                bool sceneMissing = !File.Exists(LoginScenePath);
                bool builderIsNewer = !sceneMissing &&
                    File.GetLastWriteTimeUtc(BuilderPath) > File.GetLastWriteTimeUtc(LoginScenePath);
                if (sceneMissing || builderIsNewer)
                {
                    BuildLoginScene();
                }
                else
                {
                    UpdateBuildSettings();
                }
            };
        }

        [MenuItem("Tools/Fishing Game/Build Twitch Login Scene")]
        public static void BuildLoginScene()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            Color background = new Color32(8, 15, 31, 255);
            Color primary = new Color32(241, 247, 244, 255);
            Color muted = new Color32(145, 172, 174, 255);
            Color twitch = new Color32(145, 70, 255, 255);
            Color gold = new Color32(244, 193, 76, 255);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            GameObject canvasObject = new GameObject(
                "Twitch Login Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = Rect(
                "Login Screen",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            ImageRect("Night Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, background);

            RectTransform copyColumn = Rect(
                "Login Copy",
                root,
                new Vector2(0.25f, 0.08f),
                new Vector2(0.75f, 0.55f),
                Vector2.zero,
                Vector2.zero);
            Label(
                "BALIK AVI",
                copyColumn,
                font,
                28,
                FontStyle.Bold,
                gold,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0.8f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);
            Label(
                "TWITCH İLE BAĞLAN",
                copyColumn,
                font,
                44,
                FontStyle.Bold,
                primary,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0.58f),
                new Vector2(1f, 0.8f),
                Vector2.zero,
                Vector2.zero);
            Label(
                "Hesabınla giriş yap ve oyuna devam et.",
                copyColumn,
                font,
                20,
                FontStyle.Normal,
                muted,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0.44f),
                new Vector2(1f, 0.58f),
                Vector2.zero,
                Vector2.zero);

            RectTransform loginGroup = Rect(
                "Login Group",
                copyColumn,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Button loginButton = Button(
                "TWITCH İLE GİRİŞ YAP",
                loginGroup,
                font,
                twitch,
                primary,
                new Vector2(0f, -50f),
                new Vector2(430f, 70f));
            RectTransform loginButtonRect = loginButton.GetComponent<RectTransform>();
            loginButtonRect.anchorMin = new Vector2(0.5f, 0.25f);
            loginButtonRect.anchorMax = new Vector2(0.5f, 0.25f);
            loginButtonRect.pivot = new Vector2(0.5f, 0.5f);
            loginButtonRect.anchoredPosition = Vector2.zero;

            Text statusText = Label(
                "Yayına bağlanmak için Twitch hesabınla giriş yap.",
                loginGroup,
                font,
                18,
                FontStyle.Normal,
                muted,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.16f),
                Vector2.zero,
                Vector2.zero);

            RectTransform loadingGroup = Rect(
                "Loading Group",
                copyColumn,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Label(
                "OTURUM KONTROL EDİLİYOR...",
                loadingGroup,
                font,
                22,
                FontStyle.Bold,
                gold,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0.12f),
                new Vector2(1f, 0.3f),
                Vector2.zero,
                Vector2.zero);

            RectTransform artFrame = Rect(
                "Island Art",
                root,
                new Vector2(0.35f, 0.55f),
                new Vector2(0.65f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            Image artImage = artFrame.gameObject.AddComponent<Image>();
            artImage.sprite = LoadIslandSprite();
            artImage.preserveAspect = true;
            artImage.raycastTarget = false;
            artImage.color = Color.white;

            GameObject controllerObject = new GameObject(
                "Twitch Login Controller",
                typeof(TwitchAuthService),
                typeof(TwitchLoginController),
                typeof(TwitchLoginMotion));
            TwitchLoginController controller = controllerObject.GetComponent<TwitchLoginController>();
            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("gameSceneName").stringValue = "SampleScene";
            controllerData.FindProperty("twitchClientId").stringValue = TwitchClientId;
            controllerData.FindProperty("oauthWorkerBaseUrl").stringValue = OAuthWorkerUrl;
            controllerData.FindProperty("loadingGroup").objectReferenceValue = loadingGroup.gameObject;
            controllerData.FindProperty("loginGroup").objectReferenceValue = loginGroup.gameObject;
            controllerData.FindProperty("loginButton").objectReferenceValue = loginButton;
            controllerData.FindProperty("statusText").objectReferenceValue = statusText;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            TwitchLoginMotion motion = controllerObject.GetComponent<TwitchLoginMotion>();
            SerializedObject motionData = new SerializedObject(motion);
            motionData.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            motionData.FindProperty("islandArt").objectReferenceValue = artFrame;
            motionData.FindProperty("loginButton").objectReferenceValue = loginButtonRect;
            motionData.ApplyModifiedPropertiesWithoutUndo();

            loginGroup.gameObject.SetActive(false);
            loadingGroup.gameObject.SetActive(true);

            EditorSceneManager.SaveScene(scene, LoginScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Twitch giriş sahnesi oluşturuldu: " + LoginScenePath);
        }

        private static Sprite LoadIslandSprite()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IslandArtPath);
            if (sprite != null)
            {
                return sprite;
            }

            return AssetDatabase.LoadAllAssetsAtPath(IslandArtPath)
                .OfType<Sprite>()
                .FirstOrDefault();
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LoginScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static RectTransform Rect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static Image ImageRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Label(
            string value,
            Transform parent,
            Font font,
            int size,
            FontStyle style,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            RectTransform rect = Rect(
                value.Replace("\n", " ") + " Label",
                parent,
                anchorMin,
                anchorMax,
                anchoredPosition,
                sizeDelta);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Button Button(
            string title,
            Transform parent,
            Font font,
            Color background,
            Color textColor,
            Vector2 position,
            Vector2 size)
        {
            Image image = ImageRect(
                title + " Button",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size,
                background);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.2f);
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.44f);
            button.colors = colors;

            Label(
                title,
                image.transform,
                font,
                23,
                FontStyle.Bold,
                textColor,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            return button;
        }
    }
}
#endif
