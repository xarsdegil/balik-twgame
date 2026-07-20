#if UNITY_EDITOR
using System.IO;
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
            bool keepLoginSceneOpen = previousScene.path == LoginScenePath;
            for (int index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                Scene loadedScene = SceneManager.GetSceneAt(index);
                if (loadedScene.path != LoginScenePath)
                {
                    continue;
                }

                EditorSceneManager.CloseScene(loadedScene, true);
            }

            if (keepLoginSceneOpen)
            {
                previousScene = default;
            }

            bool hasRestorablePreviousScene = previousScene.IsValid() &&
                                              previousScene.isLoaded &&
                                              !string.IsNullOrEmpty(previousScene.path);
            NewSceneMode creationMode = hasRestorablePreviousScene
                ? NewSceneMode.Additive
                : NewSceneMode.Single;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, creationMode);
            SceneManager.SetActiveScene(scene);

            Color background = new Color32(14, 18, 37, 255);
            Color primary = new Color32(247, 244, 255, 255);
            Color muted = new Color32(164, 169, 194, 255);
            Color twitch = new Color32(145, 70, 255, 255);
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
            scaler.referenceResolution = new Vector2(1080f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = Rect(
                "Login Screen",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            ImageRect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, background);

            RectTransform loginGroup = Rect(
                "Login Group",
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 620f));
            Label(
                "TWITCH CONNECTION",
                loginGroup,
                font,
                48,
                FontStyle.Bold,
                primary,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -58f),
                new Vector2(680f, 70f));
            Label(
                "Sign in with Twitch to connect the fishing game to your channel.",
                loginGroup,
                font,
                22,
                FontStyle.Normal,
                muted,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -142f),
                new Vector2(680f, 70f));
            Button loginButton = Button(
                "SIGN IN WITH TWITCH",
                loginGroup,
                font,
                twitch,
                primary,
                new Vector2(0f, 28f),
                new Vector2(650f, 82f));

            Text statusText = Label(
                "Sign in with Twitch to continue.",
                loginGroup,
                font,
                19,
                FontStyle.Normal,
                muted,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 52f),
                new Vector2(680f, 80f));

            RectTransform loadingGroup = Rect(
                "Loading Group",
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 200f));
            Label(
                "CHECKING TWITCH SESSION...",
                loadingGroup,
                font,
                28,
                FontStyle.Bold,
                primary,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            GameObject controllerObject = new GameObject(
                "Twitch Login Controller",
                typeof(TwitchAuthService),
                typeof(TwitchLoginController));
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

            loginGroup.gameObject.SetActive(false);
            loadingGroup.gameObject.SetActive(true);

            if (!EditorSceneManager.SaveScene(scene, LoginScenePath))
            {
                throw new IOException("Could not save Twitch login scene at " + LoginScenePath);
            }
            if (!keepLoginSceneOpen && hasRestorablePreviousScene)
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previousScene);
            }

            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Twitch login scene created: " + LoginScenePath);
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
                25,
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
