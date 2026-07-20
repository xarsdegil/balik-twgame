using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Gorseller")]
    [SerializeField] private SpriteRenderer fishermanRenderer;
    [SerializeField] private Transform fishPoint;
    [SerializeField] private SpriteRenderer fishSpriteRenderer;
    [SerializeField] private Texture2D idleSheet;
    [SerializeField] private Texture2D successCatchSheet;
    [SerializeField] private Texture2D failCatchSheet;
    [SerializeField] private Texture2D fishAtlas;

    [Header("Balik Tutma")]
    [SerializeField, Range(0f, 1f)] private float successChance = 0.65f;
    [SerializeField, Min(0f)] private float biteDelay = 0.7f;
    [SerializeField, Min(0f)] private float resultDuration = 2.5f;

    [Header("Balik Parlamasi")]
    [SerializeField] private Color fishGlowColor = new Color(1f, 0.78f, 0.2f, 0.42f);
    [SerializeField, Min(1f)] private float fishGlowScale = 1.25f;
    [SerializeField, Min(0f)] private float fishGlowPulseSpeed = 4f;
    [SerializeField, Range(0f, 0.5f)] private float fishGlowPulseAmount = 0.12f;

    [Header("Kutlama Arka Plani")]
    [SerializeField] private Color celebrationColor = new Color(1f, 0.72f, 0.16f, 0.38f);
    [SerializeField, InspectorName("Işın Sayısı"), Range(6, 999)] private int celebrationRayCountSetting = 24;
    [SerializeField, Min(0f)] private float celebrationRotationSpeed = 24f;
    [SerializeField, InspectorName("Genel Boyut"), Min(0.25f)] private float celebrationSizeSetting = 1.05f;
    [SerializeField] private int celebrationSortingOrder = 2;

    private readonly List<Sprite> runtimeSprites = new List<Sprite>();
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
    private string status = "Balık tutmak için SPACE tuşuna bas";
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;

    private void Awake()
    {
        idleFrames = SliceStrip(idleSheet, IdleFrameCount);
        successCatchFrames = SliceStrip(successCatchSheet, CatchFrameCount);
        failCatchFrames = SliceStrip(failCatchSheet, CatchFrameCount);

        if (fishermanRenderer == null)
        {
            Debug.LogError("FishingController: Sahnedeki Fisherman SpriteRenderer atanmamış.", this);
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
            Debug.LogError("FishingController: FishPoint veya altındaki FishSprite SpriteRenderer bulunamadı.", this);
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
        if (!isFishing)
        {
            AnimateIdle();

            if (WasSpacePressed())
            {
                StartCoroutine(TryToCatchFish());
            }
        }
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

    private IEnumerator TryToCatchFish()
    {
        isFishing = true;
        fishSpriteRenderer.enabled = false;
        fishGlowRenderer.enabled = false;
        celebrationBackground.gameObject.SetActive(false);
        status = "Olta atıldı...";

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
            fishSpriteRenderer.sprite = CreateFishSprite(fishIndex);
            fishSpriteRenderer.enabled = true;
            fishGlowRenderer.sprite = fishSpriteRenderer.sprite;
            fishGlowRenderer.enabled = true;
            celebrationBackground.gameObject.SetActive(true);
            status = $"Yakaladın! {FishNames[fishIndex]}";
        }
        else
        {
            status = "Balık kaçtı! Tekrar dene.";
        }

        yield return AnimateResultForSeconds(resultDuration);

        fishSpriteRenderer.enabled = false;
        fishGlowRenderer.enabled = false;
        celebrationBackground.gameObject.SetActive(false);
        status = "Balık tutmak için SPACE tuşuna bas";
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
        EnsureGuiStyles();
        float width = Mathf.Min(460f, Screen.width - 32f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, 18f, width, 82f);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 30f), "BALIKCI", titleStyle);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 43f, panel.width - 32f, 28f), status, hintStyle);
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { textColor = Color.white },
            padding = new RectOffset(12, 12, 8, 8)
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.86f, 0.36f) }
        };
        hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            normal = { textColor = Color.white }
        };
    }

    private void OnDestroy()
    {
        for (int i = 0; i < runtimeSprites.Count; i++)
        {
            if (runtimeSprites[i] != null)
            {
                Destroy(runtimeSprites[i]);
            }
        }
    }
}
