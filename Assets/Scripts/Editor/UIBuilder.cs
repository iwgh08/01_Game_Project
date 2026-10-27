using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static BuilderKit;

// 게임 화면 UI(HUD, 자막, 문서, 소지품, 자물쇠, 일시정지, 암전)와 타이틀 화면 UI를 만든다
public class GameUIRefs
{
    public HUD hud;
    public SubtitleUI subtitles;
    public DocumentViewer documents;
    public InventoryScreen inventory;
    public CombinationLockUI combinationLock;
    public PauseMenu pause;
    public CinematicOverlay cinematic;
}

public static class UIBuilder
{
    static TMP_FontAsset font, handFont;
    static Material shadowMaterial;

    static readonly Vector2 Center = new(0.5f, 0.5f);
    static readonly Color Ink = new(0.16f, 0.13f, 0.1f);
    static readonly Color TextColor = new(0.9f, 0.9f, 0.88f);
    static readonly Color Muted = new(0.6f, 0.6f, 0.58f);

    public static GameUIRefs BuildGameUI(TMP_FontAsset gothic, TMP_FontAsset pen, Inventory inventory)
    {
        Setup(gothic, pen);
        var canvas = CreateCanvas("UI", null);
        var refs = new GameUIRefs
        {
            hud = BuildHUD(canvas),
            documents = BuildDocumentViewer(canvas),
            inventory = BuildInventory(canvas, inventory),
            combinationLock = BuildLockUI(canvas),
            pause = BuildPause(canvas),
            subtitles = BuildSubtitles(canvas),
            cinematic = BuildCinematic(canvas)
        };
        CreateEventSystem();
        return refs;
    }

    public static void BuildMainMenuUI(TMP_FontAsset gothic, TMP_FontAsset pen, Camera camera, AudioSource ambience)
    {
        Setup(gothic, pen);
        var canvas = CreateCanvas("MenuUI", camera);
        Image(Stretch("Background", canvas), new Color(0.015f, 0.016f, 0.02f));

        var title = Text("Title", canvas, "방과후 방송", 150, TextAlignmentOptions.Center, new Color(0.86f, 0.85f, 0.8f), Center, Center, new Vector2(0f, 190f), new Vector2(1400f, 220f), handFont);
        Text("Subtitle", canvas, "철거를 앞둔 학교, 20년 전의 약속", 32, TextAlignmentOptions.Center, Muted, Center, Center, new Vector2(0f, 70f), new Vector2(1200f, 50f));
        var start = MakeButton("Start", canvas, "시작하기", new Vector2(0f, -110f), new Vector2(380f, 72f));
        var quit = MakeButton("Quit", canvas, "끝내기", new Vector2(0f, -200f), new Vector2(380f, 72f));
        Text("Controls", canvas, "WASD 이동 · 마우스 시점 · E 상호작용 · F 손전등 · Tab 소지품 · Esc 일시정지\n<size=85%>헤드폰을 쓰고 불을 끄고 플레이하는 것을 권합니다</size>",
            22, TextAlignmentOptions.Center, new Color(0.45f, 0.45f, 0.44f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1600f, 80f));

        var faderRect = Stretch("Fader", canvas);
        Image(faderRect, Color.black);
        var fader = faderRect.gameObject.AddComponent<CanvasGroup>();
        fader.alpha = 1f;

        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        Set(menu, "startButton", start);
        Set(menu, "quitButton", quit);
        Set(menu, "title", title);
        Set(menu, "fader", fader);
        Set(menu, "ambience", ambience);
        CreateEventSystem();
    }

    static void Setup(TMP_FontAsset gothic, TMP_FontAsset pen)
    {
        font = gothic;
        handFont = pen;
        string path = ArtFolder + "/Materials/UI_TextShadow.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
        shadowMaterial = new Material(gothic.material) { name = "UI_TextShadow" };
        shadowMaterial.EnableKeyword("UNDERLAY_ON");
        shadowMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
        shadowMaterial.SetFloat("_UnderlayOffsetX", 0.4f);
        shadowMaterial.SetFloat("_UnderlayOffsetY", -0.4f);
        shadowMaterial.SetFloat("_UnderlayDilate", 0.4f);
        shadowMaterial.SetFloat("_UnderlaySoftness", 0.6f);
        AssetDatabase.CreateAsset(shadowMaterial, path);
    }

    // ───────── 게임 화면 ─────────

    static HUD BuildHUD(Transform canvas)
    {
        var root = Stretch("HUD", canvas);
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var crosshair = Image(Rect("Crosshair", root, Center, Center, Vector2.zero, new Vector2(6f, 6f)), new Color(1f, 1f, 1f, 0.4f),
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
        var prompt = Text("Prompt", root, "", 28, TextAlignmentOptions.Center, TextColor, Center, Center, new Vector2(0f, -64f), new Vector2(1000f, 50f));
        prompt.fontSharedMaterial = shadowMaterial;

        var hintRect = Rect("Hint", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(1000f, 120f), new Vector2(0f, 1f));
        var hintGroup = hintRect.gameObject.AddComponent<CanvasGroup>();
        var hint = Text("Text", hintRect, "", 24, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.83f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hint.fontSharedMaterial = shadowMaterial;

        var hud = root.gameObject.AddComponent<HUD>();
        Set(hud, "group", group);
        Set(hud, "crosshair", crosshair);
        Set(hud, "promptText", prompt);
        Set(hud, "hintGroup", hintGroup);
        Set(hud, "hintText", hint);
        return hud;
    }

    static SubtitleUI BuildSubtitles(Transform canvas)
    {
        var rect = Rect("Subtitles", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(1500f, 160f));
        var group = rect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        var text = Text("Line", rect, "", 34, TextAlignmentOptions.Bottom, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.fontSharedMaterial = shadowMaterial;

        var subtitles = rect.gameObject.AddComponent<SubtitleUI>();
        Set(subtitles, "text", text);
        Set(subtitles, "group", group);
        return subtitles;
    }

    static DocumentViewer BuildDocumentViewer(Transform canvas)
    {
        var holder = Stretch("DocumentViewer", canvas);
        var root = Stretch("Root", holder);
        Image(root, new Color(0f, 0f, 0f, 0.85f));

        // 화면 아래쪽은 자막 자리로 비워 둔다
        var paperRect = Rect("Paper", root, Center, Center, new Vector2(0f, 70f), new Vector2(900f, 840f));
        var paper = Image(paperRect, new Color(0.92f, 0.89f, 0.8f));
        var title = Text("Title", paperRect, "", 40, TextAlignmentOptions.Center, Ink, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -65f), new Vector2(-120f, 70f));
        var body = Text("Body", paperRect, "", 30, TextAlignmentOptions.TopLeft, Ink, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        body.rectTransform.offsetMin = new Vector2(80f, 90f);
        body.rectTransform.offsetMax = new Vector2(-80f, -125f);
        body.lineSpacing = 12f;
        var page = Text("Page", paperRect, "", 22, TextAlignmentOptions.Center, new Color(0.4f, 0.35f, 0.3f), Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 45f), new Vector2(-100f, 40f));

        var viewer = holder.gameObject.AddComponent<DocumentViewer>();
        Set(viewer, "root", root.gameObject);
        Set(viewer, "paper", paper);
        Set(viewer, "titleText", title);
        Set(viewer, "bodyText", body);
        Set(viewer, "pageText", page);
        Set(viewer, "printFont", font);
        Set(viewer, "handwrittenFont", handFont);
        return viewer;
    }

    static InventoryScreen BuildInventory(Transform canvas, Inventory inventory)
    {
        var holder = Stretch("Inventory", canvas);
        var root = Stretch("Root", holder);
        Image(root, new Color(0.02f, 0.02f, 0.03f, 0.92f));

        Text("Title", root, "소지품", 48, TextAlignmentOptions.Left, TextColor, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
        Text("ItemsHeader", root, "가진 물건", 30, TextAlignmentOptions.Left, Muted, new Vector2(0.08f, 0.82f), new Vector2(0.46f, 0.87f), Vector2.zero, Vector2.zero);
        Text("DocsHeader", root, "읽은 기록", 30, TextAlignmentOptions.Left, Muted, new Vector2(0.54f, 0.82f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero);

        var itemList = Rect("Items", root, new Vector2(0.08f, 0.14f), new Vector2(0.46f, 0.8f), Vector2.zero, Vector2.zero);
        AddVerticalLayout(itemList, 22f);
        var itemTemplate = Text("ItemTemplate", itemList, "물건", 28, TextAlignmentOptions.TopLeft, TextColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var docList = Rect("Documents", root, new Vector2(0.54f, 0.14f), new Vector2(0.92f, 0.8f), Vector2.zero, Vector2.zero);
        AddVerticalLayout(docList, 10f);
        var docTemplate = MakeButton("DocumentTemplate", docList, "문서", Vector2.zero, new Vector2(0f, 60f));
        docTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        var docLabel = docTemplate.GetComponentInChildren<TextMeshProUGUI>();
        docLabel.alignment = TextAlignmentOptions.Left;
        docLabel.margin = new Vector4(24f, 0f, 24f, 0f);
        docLabel.fontSize = 26;

        Text("Footer", root, "[Tab] 닫기      ·      기록을 누르면 다시 읽을 수 있다", 24, TextAlignmentOptions.Center, Muted,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 60f), new Vector2(0f, 40f));

        var screen = holder.gameObject.AddComponent<InventoryScreen>();
        Set(screen, "root", root.gameObject);
        Set(screen, "inventory", inventory);
        Set(screen, "itemList", itemList);
        Set(screen, "itemTemplate", itemTemplate);
        Set(screen, "documentList", docList);
        Set(screen, "documentTemplate", docTemplate);
        return screen;
    }

    static CombinationLockUI BuildLockUI(Transform canvas)
    {
        var holder = Stretch("CombinationLock", canvas);
        var root = Stretch("Root", holder);
        Image(root, new Color(0f, 0f, 0f, 0.8f));
        var panel = Rect("Panel", root, Center, Center, Vector2.zero, new Vector2(640f, 480f));
        Image(panel, new Color(0.1f, 0.1f, 0.11f, 0.97f));
        Text("Title", panel, "번호 자물쇠", 38, TextAlignmentOptions.Center, TextColor, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -50f), new Vector2(0f, 60f));

        var digits = new TMP_Text[4];
        var ups = new Button[4];
        var downs = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float x = -180f + i * 120f;
            ups[i] = MakeButton($"Up{i}", panel, "▲", new Vector2(x, 105f), new Vector2(90f, 60f));
            var bg = Rect($"Digit{i}", panel, Center, Center, new Vector2(x, 0f), new Vector2(96f, 120f));
            Image(bg, new Color(0.04f, 0.04f, 0.045f));
            digits[i] = Text("Value", bg, "0", 76, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.75f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            downs[i] = MakeButton($"Down{i}", panel, "▼", new Vector2(x, -105f), new Vector2(90f, 60f));
        }
        Text("Footer", panel, "숫자 네 개를 맞추면 열린다      [Esc] 그만두기", 22, TextAlignmentOptions.Center, Muted,
            Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 40f), new Vector2(0f, 40f));

        var ui = holder.gameObject.AddComponent<CombinationLockUI>();
        Set(ui, "root", root.gameObject);
        Set(ui, "digitTexts", digits);
        Set(ui, "upButtons", ups);
        Set(ui, "downButtons", downs);
        return ui;
    }

    static PauseMenu BuildPause(Transform canvas)
    {
        var holder = Stretch("PauseMenu", canvas);
        var root = Stretch("Root", holder);
        Image(root, new Color(0f, 0f, 0f, 0.78f));
        Text("Title", root, "일시정지", 60, TextAlignmentOptions.Center, TextColor, Center, Center, new Vector2(0f, 220f), new Vector2(800f, 90f));
        var resume = MakeButton("Resume", root, "계속하기", new Vector2(0f, 90f), new Vector2(380f, 72f));
        var label = Text("SensitivityLabel", root, "마우스 감도  1.0", 28, TextAlignmentOptions.Center, Muted, Center, Center, new Vector2(0f, -10f), new Vector2(600f, 40f));
        var slider = MakeSlider("Sensitivity", root, new Vector2(0f, -60f), new Vector2(420f, 24f));
        var menu = MakeButton("MainMenu", root, "메인 메뉴로", new Vector2(0f, -170f), new Vector2(380f, 72f));

        var pause = holder.gameObject.AddComponent<PauseMenu>();
        Set(pause, "root", root.gameObject);
        Set(pause, "resumeButton", resume);
        Set(pause, "menuButton", menu);
        Set(pause, "sensitivitySlider", slider);
        Set(pause, "sensitivityLabel", label);
        return pause;
    }

    static CinematicOverlay BuildCinematic(Transform canvas)
    {
        var holder = Stretch("Cinematic", canvas);
        var blackRect = Stretch("Black", holder);
        Image(blackRect, Color.black);
        var black = blackRect.gameObject.AddComponent<CanvasGroup>();
        black.alpha = 1f;
        black.blocksRaycasts = false;

        var textRect = Stretch("Text", holder);
        var textGroup = textRect.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;
        textGroup.blocksRaycasts = false;
        var text = Text("Message", textRect, "", 44, TextAlignmentOptions.Center, new Color(0.88f, 0.88f, 0.86f), Center, Center, Vector2.zero, new Vector2(1400f, 600f));
        text.lineSpacing = 20f;

        var overlay = holder.gameObject.AddComponent<CinematicOverlay>();
        Set(overlay, "black", black);
        Set(overlay, "textGroup", textGroup);
        Set(overlay, "text", text);
        return overlay;
    }

    // ───────── 도구 ─────────

    static Transform CreateCanvas(string name, Camera camera)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        if (camera != null)
        {
            // 후처리(필름 그레인·비네트)가 타이틀 화면에도 걸리도록 카메라 공간에 그린다
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }
        else canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? Center;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent) => Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    static Image Image(RectTransform rect, Color color, Sprite sprite = null)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta, TMP_FontAsset fontAsset = null)
    {
        var rt = Rect(name, parent, anchorMin, anchorMax, position, sizeDelta);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = fontAsset != null ? fontAsset : font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        UsedText.Append(text);
        return tmp;
    }

    static TMP_DefaultControls.Resources ControlResources() => new()
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
    };

    static Button MakeButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        var go = TMP_DefaultControls.CreateButton(ControlResources());
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.24f, 0.95f);
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.7f, 0.7f, 0.7f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.45f, 0.45f, 0.45f);
        colors.selectedColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;

        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        text.text = label;
        text.font = font;
        text.fontSize = 30;
        text.color = TextColor;
        UsedText.Append(label);
        return button;
    }

    static Slider MakeSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        });
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go.GetComponent<Slider>();
    }

    static void AddVerticalLayout(RectTransform rect, float spacing)
    {
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }
}
