using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 参考图风格的游戏主页 UI：深色科幻背景、白色信息卡、青蓝色功能入口和顶部资源栏。
/// 组件会在包含 Canvas 的场景中自动创建，也可以手动挂到任意 Canvas 进行定制。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameHomeUI : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.055f, 0.075f, 0.085f, 0.96f);
    private static readonly Color InkSoft = new Color(0.10f, 0.13f, 0.14f, 0.90f);
    private static readonly Color Paper = new Color(0.92f, 0.93f, 0.89f, 0.96f);
    private static readonly Color PaperMuted = new Color(0.72f, 0.75f, 0.73f, 0.94f);
    private static readonly Color Cyan = new Color(0.04f, 0.63f, 0.79f, 0.98f);
    private static readonly Color CyanDark = new Color(0.02f, 0.30f, 0.40f, 0.96f);
    private static readonly Color Orange = new Color(0.96f, 0.31f, 0.08f, 0.98f);
    private static readonly Color Yellow = new Color(0.97f, 0.80f, 0.12f, 1f);
    private static Sprite solidSprite;

    [SerializeField] private bool createOnAwake = true;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite heroSprite;

    private RectTransform root;
    private Text noticeText;
    private Text dateText;
    private readonly List<Button> navigationButtons = new List<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnSceneLoad()
    {
        // UiShowcase 演示场景自带页面栈，跳过自动安装，避免双 UI 冲突。
        var bootstrap = Object.FindAnyObjectByType<MonoBehaviour>();
        if (bootstrap != null && bootstrap.GetType().Name == "UiShowcaseBootstrap")
        {
            return;
        }


        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null || Object.FindAnyObjectByType<GameHomeUI>() != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("GameHomeUI");
        Canvas overlay = rootObject.AddComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 100;
        rootObject.AddComponent<GraphicRaycaster>();
        GameHomeUI ui = rootObject.AddComponent<GameHomeUI>();
        ui.createOnAwake = true;
    }

    private void Awake()
    {
        if (createOnAwake)
        {
            Build();
        }
    }

    public void Build()
    {
        if (root != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }
        root = rect;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;

        BuildBackground();
        BuildTopBar();
        BuildLeftEvents();
        BuildHeroStage();
        BuildRightPanels();
        BuildBottomPanels();
        BuildCursorHint();
    }

    private void BuildBackground()
    {
        Image background = CreateImage("Background", root, new Color(0.035f, 0.055f, 0.065f, 1f));
        Stretch(background.rectTransform);
        background.raycastTarget = false;

        if (backgroundSprite != null)
        {
            background.sprite = backgroundSprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = Color.white;
        }

        Image vignette = CreateImage("Vignette", root, new Color(0.01f, 0.02f, 0.025f, 0.30f));
        Stretch(vignette.rectTransform);
        vignette.raycastTarget = false;

        for (int i = 0; i < 8; i++)
        {
            Image stripe = CreateImage("DiagonalStripe_" + i, root,
                new Color(0.04f, 0.18f, 0.21f, 0.12f));
            SetRect(stripe.rectTransform, 850f + i * 180f, -180f, 150f, 1500f);
            stripe.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            stripe.raycastTarget = false;
        }

        Text watermark = CreateText("WATERMARK", root, "THE OCEAN / TERMINAL", 18, PaperMuted);
        SetRect(watermark.rectTransform, 1450f, 1010f, 360f, 28f);
        watermark.alignment = TextAnchor.MiddleRight;
        watermark.raycastTarget = false;
    }

    private void BuildTopBar()
    {
        Image top = CreateImage("TopBar", root, new Color(0.02f, 0.03f, 0.035f, 0.88f));
        SetRect(top.rectTransform, 0f, 1008f, 1920f, 72f);

        Button settings = CreateButton("Settings", root, "⚙", InkSoft, Paper, 32);
        SetRect(settings.GetComponent<RectTransform>(), 24f, 1020f, 54f, 48f);
        settings.onClick.AddListener(() => ShowNotice("系统设置：UI 缩放、音量和输入绑定"));

        Button mail = CreateButton("Mail", root, "✉", InkSoft, Paper, 27);
        SetRect(mail.GetComponent<RectTransform>(), 88f, 1020f, 54f, 48f);
        mail.onClick.AddListener(() => ShowNotice("暂无未读通讯"));

        Button calendar = CreateButton("Calendar", root, "▣", InkSoft, Paper, 28);
        SetRect(calendar.GetComponent<RectTransform>(), 152f, 1020f, 54f, 48f);
        calendar.onClick.AddListener(() => ShowNotice("日程：明日 09:00 进行探索任务"));

        Image identity = CreateImage("Identity", root, Cyan, true);
        SetRect(identity.rectTransform, 260f, 1018f, 38f, 52f);
        Text identityText = CreateText("IdentityText", identity.rectTransform, "◆", 22, Color.white);
        Stretch(identityText.rectTransform);
        identityText.alignment = TextAnchor.MiddleCenter;

        Text welcome = CreateText("Welcome", root, "欢迎回到终端", 22, Paper);
        SetRect(welcome.rectTransform, 312f, 1020f, 210f, 46f);
        welcome.fontStyle = FontStyle.Bold;

        dateText = CreateText("Date", root, "2024/08/05 23:53", 20, Paper);
        SetRect(dateText.rectTransform, 1120f, 1020f, 220f, 46f);
        dateText.alignment = TextAnchor.MiddleRight;

        CreateResource(root, "▣", "129771", Cyan, 1360f);
        CreateResource(root, "◆", "9120", new Color(0.90f, 0.22f, 0.42f), 1520f);
        CreateResource(root, "⬡", "46", Yellow, 1695f);
    }

    private void BuildLeftEvents()
    {
        CreateEventCard("Event_1", root, "沉沙赫日", "签到活动", 38f, 842f, Orange);
        CreateEventCard("Event_2", root, "大巴扎", "闲逛", 38f, 766f, new Color(0.56f, 0.26f, 0.72f));

        Button expand = CreateButton("ExpandEvents", root, "⌄", Ink, Paper, 24);
        SetRect(expand.GetComponent<RectTransform>(), 42f, 724f, 86f, 38f);
        expand.onClick.AddListener(() => ShowNotice("活动列表已展开"));
    }

    private void BuildHeroStage()
    {
        Image stage = CreateImage("HeroStage", root, new Color(0.08f, 0.13f, 0.15f, 0.42f));
        SetRect(stage.rectTransform, 215f, 180f, 620f, 770f);

        Image glow = CreateImage("HeroGlow", stage.rectTransform, new Color(0.02f, 0.60f, 0.78f, 0.18f));
        SetRect(glow.rectTransform, 105f, 105f, 410f, 560f);
        glow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
        glow.raycastTarget = false;

        Sprite resolvedHeroSprite = heroSprite != null ? heroSprite : FindSceneHeroSprite();
        if (resolvedHeroSprite != null)
        {
            Image hero = CreateImage("HeroArtwork", stage.rectTransform, Color.white, true);
            SetRect(hero.rectTransform, 60f, 60f, 500f, 650f);
            hero.sprite = resolvedHeroSprite;
            hero.preserveAspect = true;
        }
        else
        {
            Text heroPlaceholder = CreateText("HeroPlaceholder", stage.rectTransform, "HERO\nTERMINAL", 54, Paper);
            SetRect(heroPlaceholder.rectTransform, 70f, 250f, 480f, 210f);
            heroPlaceholder.alignment = TextAnchor.MiddleCenter;
            heroPlaceholder.fontStyle = FontStyle.Bold;
            heroPlaceholder.color = new Color(0.92f, 0.95f, 0.92f, 0.82f);
        }

        Image levelRing = CreateImage("LevelRing", root, new Color(0.02f, 0.03f, 0.035f, 0.92f));
        SetRect(levelRing.rectTransform, 36f, 270f, 160f, 160f);
        Outline outline = levelRing.gameObject.AddComponent<Outline>();
        outline.effectColor = Yellow;
        outline.effectDistance = new Vector2(3f, -3f);
        Text level = CreateText("Level", levelRing.rectTransform, "17\nLV", 36, Paper);
        Stretch(level.rectTransform);
        level.alignment = TextAnchor.MiddleCenter;
        level.fontStyle = FontStyle.Bold;

        Text playerName = CreateText("PlayerName", root, "暗术", 30, Paper);
        SetRect(playerName.rectTransform, 42f, 228f, 150f, 42f);
        playerName.fontStyle = FontStyle.Bold;
        Text playerId = CreateText("PlayerId", root, "ID: 832211822", 17, PaperMuted);
        SetRect(playerId.rectTransform, 42f, 198f, 190f, 28f);

        Button eye = CreateButton("HideName", root, "◉", InkSoft, Paper, 20);
        SetRect(eye.GetComponent<RectTransform>(), 210f, 286f, 46f, 42f);
        eye.onClick.AddListener(() => ShowNotice("角色信息显示已切换"));
    }

    private void BuildRightPanels()
    {
        CreateLargePanel("Terminal", root, "终端", "+683", "理智 / 102", 872f, 744f, 470f, 220f, Paper, Ink);
        CreateLargePanel("Team", root, "编队", "当前编队", "队伍管理", 872f, 530f, 226f, 190f, Paper, Ink);
        CreateLargePanel("Members", root, "干员", "角色管理", "干员档案", 1116f, 530f, 226f, 190f, Paper, Ink);

        Image recruitment = CreateImage("Recruitment", root, CyanDark);
        SetRect(recruitment.rectTransform, 872f, 292f, 470f, 208f);
        Text recruitTitle = CreateText("RecruitTitle", recruitment.rectTransform, "招募", 32, Color.white);
        SetRect(recruitTitle.rectTransform, 28f, 132f, 410f, 48f);
        recruitTitle.fontStyle = FontStyle.Bold;
        Button publicRecruit = CreateButton("PublicRecruit", recruitment.rectTransform, "公开招募", Cyan, Color.white, 22);
        SetRect(publicRecruit.GetComponent<RectTransform>(), 24f, 45f, 200f, 66f);
        publicRecruit.onClick.AddListener(() => ShowNotice("公开招募功能已打开"));
        Button visitRecruit = CreateButton("VisitRecruit", recruitment.rectTransform, "干员寻访", Cyan, Color.white, 22);
        SetRect(visitRecruit.GetComponent<RectTransform>(), 246f, 45f, 200f, 66f);
        visitRecruit.onClick.AddListener(() => ShowNotice("干员寻访功能已打开"));

        CreateLargePanel("Tasks", root, "任务", "每日任务", "查看任务进度", 1360f, 530f, 230f, 190f, Paper, Ink);
        CreateLargePanel("Base", root, "基建", "BETA", "设施管理", 1608f, 530f, 230f, 190f, Paper, Ink);
        CreateLargePanel("Storage", root, "仓库", "", "物品与资源", 1608f, 292f, 230f, 190f, InkSoft, Paper);

        Button alert = CreateButton("Alert", root, "!  1", Orange, Color.white, 23);
        SetRect(alert.GetComponent<RectTransform>(), 1510f, 670f, 84f, 46f);
        alert.onClick.AddListener(() => ShowNotice("有 1 条待处理通知"));
    }

    private void BuildBottomPanels()
    {
        CreateSmallPanel("Friends", root, "好友", "社交", 270f, 50f, 230f, 112f);
        CreateSmallPanel("Archive", root, "档案", "角色资料", 515f, 50f, 230f, 112f);
        CreateSmallPanel("Mission", root, "任务", "探索进度", 1360f, 50f, 230f, 112f);
        CreateSmallPanel("Depot", root, "仓库", "容量 12 / 120", 1608f, 50f, 230f, 112f);

        Image status = CreateImage("StatusBar", root, Ink, true);
        SetRect(status.rectTransform, 790f, 62f, 500f, 88f);
        Text statusLabel = CreateText("StatusLabel", status.rectTransform, "当前状态", 17, PaperMuted);
        SetRect(statusLabel.rectTransform, 20f, 43f, 180f, 28f);
        noticeText = CreateText("Notice", status.rectTransform, "终端已连接 · 点击功能卡片查看详情", 18, Paper);
        SetRect(noticeText.rectTransform, 20f, 12f, 460f, 34f);
    }

    private void BuildCursorHint()
    {
        Text hint = CreateText("CursorHint", root, "MENU / HOME", 14, PaperMuted);
        SetRect(hint.rectTransform, 28f, 24f, 150f, 24f);
        hint.raycastTarget = false;
    }

    private void CreateResource(RectTransform parent, string icon, string value, Color color, float x)
    {
        Text iconText = CreateText("ResourceIcon_" + value, parent, icon, 24, color);
        SetRect(iconText.rectTransform, x, 1018f, 34f, 48f);
        iconText.alignment = TextAnchor.MiddleCenter;

        Text valueText = CreateText("ResourceValue_" + value, parent, value + "  +", 22, Paper);
        SetRect(valueText.rectTransform, x + 36f, 1018f, 120f, 48f);
        valueText.fontStyle = FontStyle.Bold;
    }

    private void CreateEventCard(string objectName, RectTransform parent, string title, string subtitle,
        float x, float y, Color color)
    {
        Button button = CreateButton(objectName, parent, title + "\n" + subtitle, color, Color.white, 18);
        SetRect(button.GetComponent<RectTransform>(), x, y, 180f, 68f);
        button.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        button.onClick.AddListener(() => ShowNotice(title + "：" + subtitle));
        navigationButtons.Add(button);
    }

    private void CreateLargePanel(string objectName, RectTransform parent, string title, string value,
        string subtitle, float x, float y, float width, float height, Color color, Color textColor)
    {
        Button button = CreateButton(objectName, parent, string.Empty, color, textColor, 20);
        SetRect(button.GetComponent<RectTransform>(), x, y, width, height);
        Text titleText = CreateText(objectName + "Title", button.transform as RectTransform, title, 32, textColor);
        SetRect(titleText.rectTransform, 24f, height - 62f, width - 48f, 42f);
        titleText.fontStyle = FontStyle.Bold;
        Text valueText = CreateText(objectName + "Value", button.transform as RectTransform, value, 25, textColor);
        SetRect(valueText.rectTransform, 24f, height * 0.42f, width - 48f, 42f);
        Text subtitleText = CreateText(objectName + "Subtitle", button.transform as RectTransform, subtitle, 16, textColor);
        SetRect(subtitleText.rectTransform, 24f, 18f, width - 48f, 28f);
        button.onClick.AddListener(() => ShowNotice(title + "：" + (string.IsNullOrEmpty(value) ? subtitle : value)));
        navigationButtons.Add(button);
    }

    private void CreateSmallPanel(string objectName, RectTransform parent, string title, string subtitle,
        float x, float y, float width, float height)
    {
        Button button = CreateButton(objectName, parent, string.Empty, InkSoft, Paper, 18);
        SetRect(button.GetComponent<RectTransform>(), x, y, width, height);
        Text titleText = CreateText(objectName + "Title", button.transform as RectTransform, title, 25, Paper);
        SetRect(titleText.rectTransform, 18f, 48f, width - 36f, 36f);
        titleText.fontStyle = FontStyle.Bold;
        Text subtitleText = CreateText(objectName + "Subtitle", button.transform as RectTransform, subtitle, 15, PaperMuted);
        SetRect(subtitleText.rectTransform, 18f, 17f, width - 36f, 26f);
        button.onClick.AddListener(() => ShowNotice(title + "：" + subtitle));
        navigationButtons.Add(button);
    }

    private Sprite FindSceneHeroSprite()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return null;
        }

        SpriteRenderer renderer = player.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
        {
            return solidSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "GameHomeUI_SolidTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        solidSprite.name = "GameHomeUI_SolidSprite";
        solidSprite.hideFlags = HideFlags.HideAndDontSave;
        return solidSprite;
    }

    private Image CreateImage(string objectName, RectTransform parent, Color color, bool raycastTarget = false)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.sprite = GetSolidSprite();
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private Text CreateText(string objectName, RectTransform parent, string content, int size, Color color)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        child.transform.SetParent(parent, false);
        Text text = child.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string objectName, RectTransform parent, string label, Color background,
        Color textColor, int fontSize)
    {
        Image image = CreateImage(objectName, parent, background, true);
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(background, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            Text text = CreateText(objectName + "Label", image.rectTransform, label, fontSize, textColor);
            Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
        }

        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.38f);
        outline.effectDistance = new Vector2(2f, -2f);
        return button;
    }

    private void ShowNotice(string message)
    {
        if (noticeText != null)
        {
            noticeText.text = message;
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }
}
