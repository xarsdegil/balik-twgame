using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class FishingController : MonoBehaviour
{
    private const int FishermanCellSize = 67;
    private const int IdleFrameCount = 2;
    private const int CatchFrameCount = 6;
    private const int FishCellSize = 32;
    private const int FishColumns = 12;
    private const int FishRows = 12;
    private const float FishAtlasPixelsPerUnit = 384f;

    private static readonly string[] FishNames =
    {
        "Progenetica",
        "Clownfish",
        "Blue tang",
        "Yellow tang",
        "Gem tang",
        "Sailfin tang",
        "Angelfish",
        "Queen angelfish",
        "French angelfish",
        "Goldfish",
        "Fighting fish",
        "Catfish",
        "Porcupinefish",
        "Spotted pufferfish",
        "Ocean sunfish",
        "Mahi mahi",
        "Roule's goby",
        "Red piranha",
        "Lionfish",
        "Red scorpionfish",
        "Stonefish",
        "Flying fish",
        "Guppy",
        "Sailfin molly",
        "Greater weever",
        "Sockeye salmon",
        "Taimen",
        "Atlantic salmon",
        "Masu",
        "European angler",
        "Humpback anglerfish",
        "Hairy frogfish",
        "Common carp",
        "Tench",
        "Koi carp",
        "Barracuda",
        "Cardinal tetra",
        "Emperor tetra",
        "Zebrafish",
        "Petticoat tetra",
        "European perch",
        "Starry sturgeon",
        "Lake sturgeon",
        "Striped marlin",
        "Swordfish",
        "Garfish",
        "Sardine",
        "Atlantic herring",
        "Blackspot seabream",
        "Gilthead seabream",
        "Atlantic cod",
        "Hake",
        "Bluefin tuna",
        "Pike",
        "Common barbel",
        "Tiger barb",
        "Cherry barb",
        "Opah",
        "Blue discus",
        "Rainbow trout",
        "Ribbon eel",
        "Giant moray eel",
        "European conger",
        "Harlequin snake eel",
        "Oarfish",
        "African coelacanth",
        "Longnose gar",
        "Saddled bichir",
        "Senegal bichir",
        "Turbot",
        "European seabass",
        "European anchovy",
        "Humphead wrasse",
        "Common stingray",
        "Shortfin mako shark",
        "River lamprey",
        "Raccoon butterflyfish",
        "Atlantic trumpetfish",
        "Bartlett's anthias",
        "Fire goby",
        "Atlantic spadefish",
        "Blue acara",
        "Oscar",
        "Dwarf gourami",
        "Clown loach",
        "Arapaima",
        "Asian arowana",
        "Moorish idol",
        "Banggai cardinalfish",
        "Longhorn cowfish",
        "Hogfish",
        "Bluehead wrasse",
        "Lumpfish",
        "Boeseman's rainbowfish",
        "Hillstream loach",
        "Seahorse",
        "Pacific oyster",
        "Pearl oyster",
        "Hard clam",
        "Giant clam",
        "Mediterranean mussel",
        "Great scallop",
        "Spiny cockle",
        "Fried egg jellyfish",
        "Nomad jellyfish",
        "Portuguese man o' war",
        "Moon jellyfish",
        "Flame jellyfish",
        "Common octopus",
        "Flapjack octopus",
        "Atlantic giant squid",
        "European squid",
        "Cushion star",
        "Common starfish",
        "Sea sponge",
        "Sea urchin",
        "Red king crab",
        "Common hermit crab",
        "Blue crab",
        "Yeti crab",
        "Atlantic horseshoe crab",
        "Atlantic crayfish",
        "Common lobster",
        "Spiny lobster",
        "Shrimp",
        "Green sea turtle",
        "Axolotl",
        "Chambered nautilus",
        "Goose barnacle",
        "Blue sea dragon",
        "Sea butterfly",
        "Giant cuttlefish",
        "Sea cucumber",
        "Common sea slug",
        "Emerald sea slug",
        "Leaf sheep",
        "Sea bunny",
        "Giant isopod",
        "Queen conch",
        "Purple cone snail",
        "Fireworm",
        "Spoon worm",
        "Planaria",
        "Medicinal leech"
    };

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer fishermanRenderer;
    [SerializeField] private Transform fishPoint;
    [SerializeField] private SpriteRenderer fishSpriteRenderer;
    [SerializeField] private Texture2D idleSheet;
    [SerializeField] private Texture2D successCatchSheet;
    [SerializeField] private Texture2D failCatchSheet;
    [SerializeField] private Texture2D fishAtlas;

    [Header("Fishing")]
    [SerializeField, Range(0f, 1f)] private float successChance = 0.65f;
    [SerializeField, Min(0f)] private float biteDelay = 0.7f;
    [SerializeField, Min(0f)] private float resultDuration = 2.5f;

    [Header("Fish Glow")]
    [SerializeField] private Color fishGlowColor = new Color(1f, 0.78f, 0.2f, 0.42f);
    [SerializeField, Min(1f)] private float fishGlowScale = 1.25f;
    [SerializeField, Min(0f)] private float fishGlowPulseSpeed = 4f;
    [SerializeField, Range(0f, 0.5f)] private float fishGlowPulseAmount = 0.12f;

    [Header("Celebration Background")]
    [SerializeField] private Color celebrationColor = new Color(1f, 0.72f, 0.16f, 0.38f);
    [SerializeField, InspectorName("Ray Count"), Range(6, 999)] private int celebrationRayCountSetting = 24;
    [SerializeField, Min(0f)] private float celebrationRotationSpeed = 24f;
    [SerializeField, InspectorName("Overall Size"), Min(0.25f)] private float celebrationSizeSetting = 1.05f;
    [SerializeField] private int celebrationSortingOrder = 2;

    private readonly List<Sprite> runtimeSprites = new List<Sprite>();
    private readonly Queue<FishingRequest> fishingQueue = new Queue<FishingRequest>();
    private Sprite[] idleFrames;
    private Sprite[] successCatchFrames;
    private Sprite[] failCatchFrames;
    private SpriteRenderer fishGlowRenderer;
    private Transform fishGlowTransform;
    private Transform celebrationBackground;
    private SpriteRenderer[] celebrationRays;
    private bool isFishing;
    private float fishPixelsPerUnit = FishAtlasPixelsPerUnit;
    private float idleTimer;
    private int idleFrame;
    private bool settingsOpen;
    private float settingsButtonVisibility;
    private bool pointerInsideWindow;
    private bool applicationFocused = true;
    private int settingsTab;
    private Vector2 settingsScrollPosition;
    private Vector2 gameSettingsScrollPosition;
    private Camera gameCamera;
    private Color defaultGameBackgroundColor;
    private Color gameBackgroundColor;
    private Texture2D colorWheelTexture;
    private float playerCooldownSeconds;
    private string fishCommand;
    private string fishCountCommand;
    private string countMessage;
    private string notReadyMessage;
    private string caughtMessage;
    private string escapedMessage;
    private string queueFullMessage;
    private GUIStyle settingsTitleStyle;
    private GUIStyle settingsLabelStyle;
    private GUIStyle settingsValueStyle;
    private GUIStyle settingsActionStyle;
    private GUIStyle settingsTextFieldStyle;
    private GUIStyle settingsHintStyle;

    private void Awake()
    {
        using IDisposable diagnostics = RuntimeDiagnostics.Measure("FishingController.Awake");
        gameCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        defaultGameBackgroundColor = gameCamera != null
            ? gameCamera.backgroundColor
            : new Color(0.06561402f, 0.06074225f, 0.122641504f, 0f);
        LoadSettings();
        ApplyBackgroundColor();
        pointerInsideWindow = IsPointerInsideWindow();
        idleFrames = SliceStrip(idleSheet, IdleFrameCount);
        successCatchFrames = SliceStrip(successCatchSheet, CatchFrameCount);
        failCatchFrames = SliceStrip(failCatchSheet, CatchFrameCount);

        if (fishermanRenderer == null)
        {
            Debug.LogError("FishingController: The Fisherman SpriteRenderer is not assigned.", this);
            enabled = false;
            return;
        }

        fishermanRenderer.sprite = idleFrames.Length > 0 ? idleFrames[0] : null;

        if (fishPoint == null)
        {
            GameObject fishPointObject = FindSceneObject("FishPoint");
            fishPoint = fishPointObject != null ? fishPointObject.transform : null;
        }

        if (fishSpriteRenderer == null && fishPoint != null)
        {
            Transform fishSprite = fishPoint.Find("FishSprite") ?? fishPoint.Find("fishsprite");
            if (fishSprite != null)
            {
                fishSpriteRenderer = fishSprite.GetComponent<SpriteRenderer>();
            }
        }

        if (fishSpriteRenderer == null)
        {
            GameObject fishSpriteObject = FindSceneObject("FishSprite");
            fishSpriteRenderer = fishSpriteObject != null
                ? fishSpriteObject.GetComponent<SpriteRenderer>()
                : null;
        }

        if (fishPoint == null || fishSpriteRenderer == null)
        {
            Debug.LogError("FishingController: FishPoint or its FishSprite child was not found.", this);
            enabled = false;
            return;
        }

        if (fishSpriteRenderer.sprite != null)
        {
            fishPixelsPerUnit = fishSpriteRenderer.sprite.pixelsPerUnit;
        }

        fishSpriteRenderer.enabled = false;
        SetupFishGlow();
        SetupCelebrationBackground();
    }

    private void Update()
    {
        UpdateSettingsOverlay();

        if (!isFishing)
        {
            AnimateIdle();

            if (fishingQueue.Count > 0)
            {
                StartCoroutine(TryToCatchFish(fishingQueue.Dequeue()));
            }
            else if (WasSpacePressed())
            {
                StartCoroutine(TryToCatchFish(null));
            }
        }
    }

    public bool QueueFishingAttempt(
        string userId,
        string displayName,
        Action<bool, string, int> completed)
    {
        if (fishingQueue.Count >= 50)
        {
            return false;
        }

        fishingQueue.Enqueue(new FishingRequest(userId, displayName, completed));
        return true;
    }

    private void AnimateIdle()
    {
        if (idleFrames.Length == 0)
        {
            return;
        }

        idleTimer += Time.deltaTime;
        if (idleTimer < 0.22f)
        {
            return;
        }

        idleTimer = 0f;
        idleFrame = (idleFrame + 1) % idleFrames.Length;
        fishermanRenderer.sprite = idleFrames[idleFrame];
    }

    private IEnumerator TryToCatchFish(FishingRequest request)
    {
        isFishing = true;
        fishSpriteRenderer.enabled = false;
        fishGlowRenderer.enabled = false;
        celebrationBackground.gameObject.SetActive(false);
        bool caught = Random.value <= successChance && fishAtlas != null;
        Sprite[] catchFrames = caught ? successCatchFrames : failCatchFrames;

        yield return AnimateIdleForSeconds(biteDelay);

        for (int i = 0; i < catchFrames.Length; i++)
        {
            fishermanRenderer.sprite = catchFrames[i];
            yield return new WaitForSeconds(0.12f);
        }

        ResetIdleAnimation();

        if (caught)
        {
            int fishIndex = Random.Range(0, FishColumns * FishRows);
            string fishName = FishNames[fishIndex];
            fishSpriteRenderer.sprite = CreateFishSprite(fishIndex);
            fishSpriteRenderer.enabled = true;
            fishGlowRenderer.sprite = fishSpriteRenderer.sprite;
            fishGlowRenderer.enabled = true;
            celebrationBackground.gameObject.SetActive(true);
            int total = request != null
                ? FishCollectionStore.RecordCatch(request.userId, request.displayName, fishName)
                : 0;
            request?.completed?.Invoke(true, fishName, total);
        }
        else
        {
            request?.completed?.Invoke(false, string.Empty, 0);
        }

        yield return AnimateResultForSeconds(resultDuration);

        fishSpriteRenderer.enabled = false;
        fishGlowRenderer.enabled = false;
        celebrationBackground.gameObject.SetActive(false);
        isFishing = false;
    }

    private IEnumerator AnimateIdleForSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            AnimateIdle();
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator AnimateResultForSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            AnimateIdle();

            if (fishGlowRenderer.enabled)
            {
                float pulse = Mathf.Sin(elapsed * fishGlowPulseSpeed) * 0.5f + 0.5f;
                float scale = fishGlowScale * (1f + pulse * fishGlowPulseAmount);
                fishGlowTransform.localScale = Vector3.one * scale;

                Color color = fishGlowColor;
                color.a *= 0.65f + pulse * 0.35f;
                fishGlowRenderer.color = color;
            }

            if (celebrationBackground.gameObject.activeSelf)
            {
                float rotation = elapsed * celebrationRotationSpeed;
                celebrationBackground.localRotation = Quaternion.Euler(0f, 0f, rotation);

                float backgroundPulse = 1f + Mathf.Sin(elapsed * 3.2f) * 0.06f;
                celebrationBackground.localScale = Vector3.one * backgroundPulse;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetupFishGlow()
    {
        fishGlowTransform = fishSpriteRenderer.transform.Find("Fish Glow");
        if (fishGlowTransform == null)
        {
            GameObject glowObject = new GameObject("Fish Glow");
            fishGlowTransform = glowObject.transform;
            fishGlowTransform.SetParent(fishSpriteRenderer.transform, false);
        }

        fishGlowTransform.localPosition = Vector3.zero;
        fishGlowTransform.localRotation = Quaternion.identity;
        fishGlowTransform.localScale = Vector3.one * fishGlowScale;

        fishGlowRenderer = fishGlowTransform.GetComponent<SpriteRenderer>();
        if (fishGlowRenderer == null)
        {
            fishGlowRenderer = fishGlowTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        fishGlowRenderer.sharedMaterial = fishSpriteRenderer.sharedMaterial;
        fishGlowRenderer.sortingLayerID = fishSpriteRenderer.sortingLayerID;
        fishGlowRenderer.sortingOrder = celebrationSortingOrder;
        fishGlowRenderer.color = fishGlowColor;
        fishGlowRenderer.enabled = false;
    }

    private void SetupCelebrationBackground()
    {
        using IDisposable diagnostics = RuntimeDiagnostics.Measure("FishingController.SetupCelebrationBackground");
        celebrationBackground = fishSpriteRenderer.transform.Find("Fish Celebration Background");
        if (celebrationBackground == null)
        {
            GameObject backgroundObject = new GameObject("Fish Celebration Background");
            celebrationBackground = backgroundObject.transform;
            celebrationBackground.SetParent(fishSpriteRenderer.transform, false);
        }

        celebrationBackground.localPosition = Vector3.zero;
        celebrationBackground.localRotation = Quaternion.identity;
        celebrationBackground.localScale = Vector3.one;

        Sprite raySprite = CreateRuntimeSprite(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            1f);

        celebrationRays = new SpriteRenderer[celebrationRayCountSetting];
        float fishDiameter = fishSpriteRenderer.sprite != null
            ? Mathf.Max(fishSpriteRenderer.sprite.bounds.size.x, fishSpriteRenderer.sprite.bounds.size.y)
            : FishCellSize / FishAtlasPixelsPerUnit;
        float baseRayLength = fishDiameter * celebrationSizeSetting;
        System.Random rayRandom = new System.Random(20260718);
        int existingRayCount = celebrationBackground.childCount;

        for (int i = 0; i < celebrationRayCountSetting; i++)
        {
            GameObject rayObject;
            if (i < existingRayCount)
            {
                rayObject = celebrationBackground.GetChild(i).gameObject;
                rayObject.name = $"Celebration Ray {i + 1:00}";
                rayObject.SetActive(true);
            }
            else
            {
                rayObject = new GameObject($"Celebration Ray {i + 1:00}");
            }

            Transform rayTransform = rayObject.transform;
            rayTransform.SetParent(celebrationBackground, false);

            float angle = 360f * i / celebrationRayCountSetting;
            float lengthMultiplier = Mathf.Lerp(0.7f, 1.3f, (float)rayRandom.NextDouble());
            float widthMultiplier = Mathf.Lerp(0.65f, 1.35f, (float)rayRandom.NextDouble());
            float rayLength = baseRayLength * lengthMultiplier;
            float rayWidth = baseRayLength * 0.09f * widthMultiplier;
            rayTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            rayTransform.localPosition = Quaternion.Euler(0f, 0f, angle)
                * new Vector3(0f, rayLength * 0.46f, 0f);
            rayTransform.localScale = new Vector3(rayWidth, rayLength, 1f);

            SpriteRenderer rayRenderer = rayObject.GetComponent<SpriteRenderer>();
            if (rayRenderer == null)
            {
                rayRenderer = rayObject.AddComponent<SpriteRenderer>();
            }

            rayRenderer.sprite = raySprite;
            rayRenderer.sharedMaterial = fishSpriteRenderer.sharedMaterial;
            rayRenderer.sortingLayerID = fishSpriteRenderer.sortingLayerID;
            rayRenderer.sortingOrder = celebrationSortingOrder;

            Color rayColor = celebrationColor;
            rayColor.a *= i % 2 == 0 ? 1f : 0.58f;
            rayRenderer.color = rayColor;
            celebrationRays[i] = rayRenderer;
        }

        for (int i = celebrationRayCountSetting; i < existingRayCount; i++)
        {
            celebrationBackground.GetChild(i).gameObject.SetActive(false);
        }

        celebrationBackground.gameObject.SetActive(false);
    }

    private void ResetIdleAnimation()
    {
        idleFrame = 0;
        idleTimer = 0f;
        fishermanRenderer.sprite = idleFrames.Length > 0 ? idleFrames[0] : null;
    }

    private Sprite[] SliceStrip(Texture2D sheet, int frameCount)
    {
        if (sheet == null)
        {
            return new Sprite[0];
        }

        Sprite[] frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = CreateRuntimeSprite(
                sheet,
                new Rect(i * FishermanCellSize, 0, FishermanCellSize, FishermanCellSize),
                FishermanCellSize);
        }

        return frames;
    }

    private Sprite CreateFishSprite(int index)
    {
        int column = index % FishColumns;
        int rowFromTop = index / FishColumns;
        int y = (FishRows - 1 - rowFromTop) * FishCellSize;
        return CreateRuntimeSprite(
            fishAtlas,
            new Rect(column * FishCellSize, y, FishCellSize, FishCellSize),
            fishPixelsPerUnit);
    }

    private static GameObject FindSceneObject(string expectedName)
    {
        string normalizedExpectedName = expectedName.Replace(" ", string.Empty);
        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject candidate = sceneObjects[i];
            if (!candidate.scene.IsValid())
            {
                continue;
            }

            string normalizedCandidateName = candidate.name.Replace(" ", string.Empty);
            if (string.Equals(
                normalizedCandidateName,
                normalizedExpectedName,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private Sprite CreateRuntimeSprite(Texture2D texture, Rect rect, float pixelsPerUnit)
    {
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        runtimeSprites.Add(sprite);
        return sprite;
    }

    private static bool WasSpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void OnGUI()
    {
        TrackPointerWindowState();
        EnsureSettingsStyles();

        if (settingsButtonVisibility <= 0.001f && !settingsOpen)
        {
            return;
        }

        Color previousGuiColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, settingsOpen ? 1f : settingsButtonVisibility);

        Rect buttonRect = new Rect(Screen.width - 60f, 16f, 44f, 44f);
        DrawSolidRect(buttonRect, new Color(0.055f, 0.071f, 0.145f, 0.92f));
        if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
        {
            settingsOpen = !settingsOpen;
            if (!settingsOpen)
            {
                SaveSettings();
            }
        }

        DrawSettingsIcon(buttonRect);
        GUI.color = previousGuiColor;

        if (settingsOpen)
        {
            DrawSettingsPanel();
        }
    }

    private void UpdateSettingsOverlay()
    {
        float target = (applicationFocused && pointerInsideWindow) || settingsOpen ? 1f : 0f;
        settingsButtonVisibility = Mathf.MoveTowards(
            settingsButtonVisibility,
            target,
            Time.unscaledDeltaTime * 5f);

#if ENABLE_INPUT_SYSTEM
        if (settingsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            settingsOpen = false;
            SaveSettings();
        }
#else
        if (settingsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            settingsOpen = false;
            SaveSettings();
        }
#endif
    }

    private void TrackPointerWindowState()
    {
        Event current = Event.current;
        if (current == null)
        {
            return;
        }

        if (current.type == EventType.MouseLeaveWindow)
        {
            pointerInsideWindow = false;
        }
        else if (current.type == EventType.MouseEnterWindow || current.isMouse)
        {
            Vector2 pointer = current.mousePosition;
            pointerInsideWindow = pointer.x >= 0f && pointer.x <= Screen.width &&
                                  pointer.y >= 0f && pointer.y <= Screen.height;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        applicationFocused = hasFocus;
        if (!hasFocus)
        {
            pointerInsideWindow = false;
        }
        else
        {
            pointerInsideWindow = IsPointerInsideWindow();
        }
    }

    private static bool IsPointerInsideWindow()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return false;
        }

        Vector2 pointer = Mouse.current.position.ReadValue();
#else
        Vector2 pointer = Input.mousePosition;
#endif
        return pointer.x >= 0f && pointer.x <= Screen.width &&
               pointer.y >= 0f && pointer.y <= Screen.height;
    }

    private void DrawSettingsPanel()
    {
        float width = Mathf.Min(520f, Screen.width - 32f);
        float height = Mathf.Max(360f, Mathf.Min(640f, Screen.height - 88f));
        Rect panel = new Rect(Screen.width - width - 16f, 72f, width, height);
        DrawSolidRect(
            new Rect(panel.x + 5f, panel.y + 7f, panel.width, panel.height),
            new Color(0f, 0f, 0f, 0.28f));
        DrawSolidRect(panel, new Color(0.055f, 0.071f, 0.145f, 0.97f));

        float innerX = panel.x + 24f;
        float innerWidth = panel.width - 48f;
        GUI.Label(new Rect(innerX, panel.y + 18f, innerWidth, 34f), "SETTINGS", settingsTitleStyle);

        float tabY = panel.y + 56f;
        Rect gameTabRect = new Rect(innerX, tabY, 92f, 32f);
        Rect chatTabRect = new Rect(innerX + 100f, tabY, 92f, 32f);
        DrawSettingsTab(gameTabRect, "GAME", 0);
        DrawSettingsTab(chatTabRect, "CHAT", 1);

        bool previousChanged = GUI.changed;
        GUI.changed = false;
        Rect contentRect = new Rect(innerX, panel.y + 104f, innerWidth, panel.height - 176f);
        if (settingsTab == 0)
        {
            DrawGameSettings(contentRect);
        }
        else
        {
            DrawChatSettings(contentRect);
        }

        bool settingsChanged = GUI.changed;
        GUI.changed = previousChanged || settingsChanged;

        Rect resetRect = new Rect(innerX, panel.yMax - 54f, 118f, 34f);
        Rect doneRect = new Rect(panel.xMax - 142f, panel.yMax - 54f, 118f, 34f);
        if (GUI.Button(resetRect, "RESET", settingsActionStyle))
        {
            ResetCurrentSettingsTab();
            SaveSettings();
        }

        if (GUI.Button(doneRect, "DONE", settingsActionStyle))
        {
            settingsOpen = false;
            SaveSettings();
        }

        if (settingsChanged)
        {
            SaveSettings();
        }
    }

    private void DrawSettingsTab(Rect rect, string label, int tab)
    {
        if (settingsTab == tab)
        {
            DrawSolidRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), new Color(0.57f, 0.28f, 1f));
        }

        if (GUI.Button(rect, label, settingsActionStyle))
        {
            settingsTab = tab;
        }
    }

    private void DrawGameSettings(Rect contentRect)
    {
        const float contentHeight = 558f;
        gameSettingsScrollPosition = GUI.BeginScrollView(
            contentRect,
            gameSettingsScrollPosition,
            new Rect(0f, 0f, contentRect.width - 18f, contentHeight));

        float width = contentRect.width - 24f;
        float y = 0f;
        successChance = DrawSliderRow("Catch chance", successChance, 0f, 1f,
            Mathf.RoundToInt(successChance * 100f) + "%", 0f, width, y);
        y += 62f;
        biteDelay = DrawSliderRow("Bite delay", biteDelay, 0f, 3f,
            biteDelay.ToString("0.0") + "s", 0f, width, y);
        y += 62f;
        resultDuration = DrawSliderRow("Result duration", resultDuration, 0.5f, 6f,
            resultDuration.ToString("0.0") + "s", 0f, width, y);
        y += 62f;
        fishGlowScale = DrawSliderRow("Catch effect size", fishGlowScale, 1f, 2f,
            fishGlowScale.ToString("0.00") + "x", 0f, width, y);
        y += 70f;

        GUI.Label(new Rect(0f, y, width - 150f, 24f), "Background color", settingsLabelStyle);
        Color opaquePreview = new Color(gameBackgroundColor.r, gameBackgroundColor.g, gameBackgroundColor.b, 1f);
        GUI.Label(new Rect(width - 144f, y, 94f, 24f),
            "#" + ColorUtility.ToHtmlStringRGB(opaquePreview), settingsValueStyle);
        Rect previewRect = new Rect(width - 42f, y + 2f, 42f, 20f);
        DrawSolidRect(previewRect, opaquePreview);
        y += 34f;

        const float wheelSize = 176f;
        Rect wheelRect = new Rect((width - wheelSize) * 0.5f, y, wheelSize, wheelSize);
        DrawColorWheel(wheelRect);
        y += wheelSize + 18f;

        Color.RGBToHSV(gameBackgroundColor, out float hue, out float saturation, out float brightness);
        brightness = DrawSliderRow(
            "Brightness",
            brightness,
            0f,
            1f,
            Mathf.RoundToInt(brightness * 100f) + "%",
            0f,
            width,
            y);
        Color selectedColor = Color.HSVToRGB(hue, saturation, brightness);
        gameBackgroundColor = new Color(
            selectedColor.r,
            selectedColor.g,
            selectedColor.b,
            defaultGameBackgroundColor.a);
        ApplyBackgroundColor();

        GUI.EndScrollView();
    }

    private void DrawColorWheel(Rect wheelRect)
    {
        EnsureColorWheelTexture();
        GUI.DrawTexture(wheelRect, colorWheelTexture, ScaleMode.StretchToFill, true);

        Color.RGBToHSV(gameBackgroundColor, out float hue, out float saturation, out float brightness);
        int controlId = GUIUtility.GetControlID(FocusType.Passive, wheelRect);
        Event current = Event.current;
        EventType eventType = current.GetTypeForControl(controlId);
        if (eventType == EventType.MouseDown && current.button == 0 && wheelRect.Contains(current.mousePosition))
        {
            GUIUtility.hotControl = controlId;
            SetColorFromWheelPointer(wheelRect, current.mousePosition, brightness);
            current.Use();
        }
        else if (eventType == EventType.MouseDrag && GUIUtility.hotControl == controlId)
        {
            SetColorFromWheelPointer(wheelRect, current.mousePosition, brightness);
            current.Use();
        }
        else if (eventType == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            GUIUtility.hotControl = 0;
            current.Use();
        }

        Color.RGBToHSV(gameBackgroundColor, out hue, out saturation, out brightness);
        float angle = hue * Mathf.PI * 2f;
        float radius = wheelRect.width * 0.5f;
        Vector2 center = wheelRect.center;
        Vector2 handle = center + new Vector2(
            Mathf.Cos(angle) * saturation * radius,
            -Mathf.Sin(angle) * saturation * radius);
        DrawSolidRect(new Rect(handle.x - 6f, handle.y - 6f, 12f, 12f), new Color(0.02f, 0.02f, 0.03f));
        DrawSolidRect(new Rect(handle.x - 4f, handle.y - 4f, 8f, 8f), Color.white);
        DrawSolidRect(new Rect(handle.x - 2f, handle.y - 2f, 4f, 4f),
            new Color(gameBackgroundColor.r, gameBackgroundColor.g, gameBackgroundColor.b, 1f));
    }

    private void SetColorFromWheelPointer(Rect wheelRect, Vector2 pointer, float brightness)
    {
        Vector2 center = wheelRect.center;
        Vector2 offset = pointer - center;
        float radius = wheelRect.width * 0.5f;
        float saturation = Mathf.Clamp01(offset.magnitude / radius);
        float hue = Mathf.Repeat(Mathf.Atan2(-offset.y, offset.x) / (Mathf.PI * 2f), 1f);
        Color selectedColor = Color.HSVToRGB(hue, saturation, brightness);
        gameBackgroundColor = new Color(
            selectedColor.r,
            selectedColor.g,
            selectedColor.b,
            defaultGameBackgroundColor.a);
        GUI.changed = true;
        ApplyBackgroundColor();
    }

    private void EnsureColorWheelTexture()
    {
        if (colorWheelTexture != null)
        {
            return;
        }

        using IDisposable diagnostics = RuntimeDiagnostics.Measure("FishingController.CreateColorWheelTexture");

        const int size = 192;
        float radius = (size - 1) * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - radius) / radius;
                float dy = (y - radius) / radius;
                float saturation = Mathf.Sqrt(dx * dx + dy * dy);
                if (saturation > 1f)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float hue = Mathf.Repeat(Mathf.Atan2(dy, dx) / (Mathf.PI * 2f), 1f);
                Color color = Color.HSVToRGB(hue, saturation, 1f);
                color.a = Mathf.Clamp01((1f - saturation) * 48f);
                pixels[y * size + x] = color;
            }
        }

        colorWheelTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Color Wheel",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        colorWheelTexture.SetPixels(pixels);
        colorWheelTexture.Apply(false, true);
    }

    private void DrawChatSettings(Rect contentRect)
    {
        const float contentHeight = 574f;
        settingsScrollPosition = GUI.BeginScrollView(
            contentRect,
            settingsScrollPosition,
            new Rect(0f, 0f, contentRect.width - 18f, contentHeight));

        float width = contentRect.width - 24f;
        float y = 0f;
        playerCooldownSeconds = DrawSliderRow(
            "Per-player cooldown",
            playerCooldownSeconds,
            0f,
            60f,
            playerCooldownSeconds.ToString("0.0") + "s",
            0f,
            width,
            y);
        y += 66f;
        fishCommand = DrawTextFieldRow("Fish command", fishCommand, 0f, width, y);
        y += 58f;
        fishCountCommand = DrawTextFieldRow("Collection command", fishCountCommand, 0f, width, y);
        y += 66f;
        GUI.Label(new Rect(0f, y, width, 36f), "Message variables: {user}, {fish}, {total}", settingsHintStyle);
        y += 38f;
        countMessage = DrawTextFieldRow("Collection response", countMessage, 0f, width, y);
        y += 66f;
        notReadyMessage = DrawTextFieldRow("Game unavailable", notReadyMessage, 0f, width, y);
        y += 66f;
        caughtMessage = DrawTextFieldRow("Catch success", caughtMessage, 0f, width, y);
        y += 66f;
        escapedMessage = DrawTextFieldRow("Catch failed", escapedMessage, 0f, width, y);
        y += 66f;
        queueFullMessage = DrawTextFieldRow("Queue full", queueFullMessage, 0f, width, y);

        GUI.EndScrollView();
    }

    private string DrawTextFieldRow(string label, string value, float x, float width, float y)
    {
        GUI.Label(new Rect(x, y, width, 22f), label, settingsLabelStyle);
        return GUI.TextField(new Rect(x, y + 25f, width, 30f), value ?? string.Empty, 180, settingsTextFieldStyle);
    }

    private void ResetCurrentSettingsTab()
    {
        if (settingsTab == 0)
        {
            successChance = 0.65f;
            biteDelay = 0.7f;
            resultDuration = 2.5f;
            fishGlowScale = 1.25f;
            gameBackgroundColor = defaultGameBackgroundColor;
            ApplyBackgroundColor();
            return;
        }

        playerCooldownSeconds = FishingChatSettings.DefaultPlayerCooldownSeconds;
        fishCommand = FishingChatSettings.DefaultFishCommand;
        fishCountCommand = FishingChatSettings.DefaultFishCountCommand;
        countMessage = FishingChatSettings.DefaultCountMessage;
        notReadyMessage = FishingChatSettings.DefaultNotReadyMessage;
        caughtMessage = FishingChatSettings.DefaultCaughtMessage;
        escapedMessage = FishingChatSettings.DefaultEscapedMessage;
        queueFullMessage = FishingChatSettings.DefaultQueueFullMessage;
    }

    private float DrawSliderRow(
        string label,
        float value,
        float minimum,
        float maximum,
        string formattedValue,
        float x,
        float width,
        float y)
    {
        GUI.Label(new Rect(x, y, width - 72f, 24f), label, settingsLabelStyle);
        GUI.Label(new Rect(x + width - 72f, y, 72f, 24f), formattedValue, settingsValueStyle);
        return GUI.HorizontalSlider(new Rect(x, y + 31f, width, 18f), value, minimum, maximum);
    }

    private static void DrawSettingsIcon(Rect rect)
    {
        Color line = new Color(0.94f, 0.92f, 1f, 1f);
        float x = rect.x + 12f;
        float y = rect.y + 13f;
        DrawSolidRect(new Rect(x, y, 20f, 2f), line);
        DrawSolidRect(new Rect(x + 5f, y - 2f, 4f, 6f), line);
        DrawSolidRect(new Rect(x, y + 8f, 20f, 2f), line);
        DrawSolidRect(new Rect(x + 13f, y + 6f, 4f, 6f), line);
        DrawSolidRect(new Rect(x, y + 16f, 20f, 2f), line);
        DrawSolidRect(new Rect(x + 8f, y + 14f, 4f, 6f), line);
    }

    private static void DrawSolidRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        color.a *= previous.a;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void EnsureSettingsStyles()
    {
        if (settingsTitleStyle != null)
        {
            return;
        }

        settingsTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.94f, 0.92f, 1f) }
        };
        settingsLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            normal = { textColor = new Color(0.78f, 0.79f, 0.88f) }
        };
        settingsValueStyle = new GUIStyle(settingsLabelStyle)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.57f, 0.28f, 1f) }
        };
        settingsActionStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.94f, 0.92f, 1f) }
        };
        settingsTextFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 13,
            padding = new RectOffset(10, 10, 6, 5),
            normal = { textColor = new Color(0.94f, 0.92f, 1f) }
        };
        settingsHintStyle = new GUIStyle(settingsLabelStyle)
        {
            fontSize = 12,
            wordWrap = true,
            normal = { textColor = new Color(0.57f, 0.58f, 0.7f) }
        };
    }

    private void LoadSettings()
    {
        successChance = PlayerPrefs.GetFloat("Fishing.SuccessChance", successChance);
        biteDelay = PlayerPrefs.GetFloat("Fishing.BiteDelay", biteDelay);
        resultDuration = PlayerPrefs.GetFloat("Fishing.ResultDuration", resultDuration);
        fishGlowScale = PlayerPrefs.GetFloat("Fishing.GlowScale", fishGlowScale);
        gameBackgroundColor = new Color(
            Mathf.Clamp01(PlayerPrefs.GetFloat("Fishing.BackgroundRed", defaultGameBackgroundColor.r)),
            Mathf.Clamp01(PlayerPrefs.GetFloat("Fishing.BackgroundGreen", defaultGameBackgroundColor.g)),
            Mathf.Clamp01(PlayerPrefs.GetFloat("Fishing.BackgroundBlue", defaultGameBackgroundColor.b)),
            defaultGameBackgroundColor.a);
        playerCooldownSeconds = FishingChatSettings.PlayerCooldownSeconds;
        fishCommand = FishingChatSettings.FishCommand;
        fishCountCommand = FishingChatSettings.FishCountCommand;
        countMessage = FishingChatSettings.CountMessage;
        notReadyMessage = FishingChatSettings.NotReadyMessage;
        caughtMessage = FishingChatSettings.CaughtMessage;
        escapedMessage = FishingChatSettings.EscapedMessage;
        queueFullMessage = FishingChatSettings.QueueFullMessage;
    }

    private void SaveSettings()
    {
        using IDisposable diagnostics = RuntimeDiagnostics.Measure("FishingController.SaveSettings");
        PlayerPrefs.SetFloat("Fishing.SuccessChance", successChance);
        PlayerPrefs.SetFloat("Fishing.BiteDelay", biteDelay);
        PlayerPrefs.SetFloat("Fishing.ResultDuration", resultDuration);
        PlayerPrefs.SetFloat("Fishing.GlowScale", fishGlowScale);
        PlayerPrefs.SetFloat("Fishing.BackgroundRed", gameBackgroundColor.r);
        PlayerPrefs.SetFloat("Fishing.BackgroundGreen", gameBackgroundColor.g);
        PlayerPrefs.SetFloat("Fishing.BackgroundBlue", gameBackgroundColor.b);
        FishingChatSettings.Save(
            playerCooldownSeconds,
            fishCommand,
            fishCountCommand,
            countMessage,
            notReadyMessage,
            caughtMessage,
            escapedMessage,
            queueFullMessage);
        PlayerPrefs.Save();
    }

    private void ApplyBackgroundColor()
    {
        if (gameCamera != null)
        {
            gameCamera.backgroundColor = gameBackgroundColor;
        }
    }

    private void OnDestroy()
    {
        if (colorWheelTexture != null)
        {
            Destroy(colorWheelTexture);
        }

        for (int i = 0; i < runtimeSprites.Count; i++)
        {
            if (runtimeSprites[i] != null)
            {
                Destroy(runtimeSprites[i]);
            }
        }
    }

    private sealed class FishingRequest
    {
        public FishingRequest(string userId, string displayName, Action<bool, string, int> completed)
        {
            this.userId = userId;
            this.displayName = string.IsNullOrWhiteSpace(displayName) ? "Viewer" : displayName;
            this.completed = completed;
        }

        public readonly string userId;
        public readonly string displayName;
        public readonly Action<bool, string, int> completed;
    }
}
