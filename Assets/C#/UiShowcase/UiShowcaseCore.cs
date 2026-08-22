using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UiShowcase
{
    /// <summary>
    /// UiShowcase 统一日志：资源缺失等失败情况必须可见，不静默。
    /// </summary>
    public static class UiLog
    {
        public const string Tag = "[UiShowcase]";

        public static void Info(string message)
        {
            Debug.Log(Tag + " " + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Tag + " " + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Tag + " " + message);
        }
    }

    /// <summary>页面契约：UI 框架层与界面内容分离的最小接口。</summary>
    public interface IPage
    {
        string PageName { get; }
        RectTransform Root { get; }
        void OnEnter();
        void OnExit();
    }

    /// <summary>弹窗契约：弹窗独立于页面栈，由弹窗栈统一管理。</summary>
    public interface IDialog
    {
        RectTransform Root { get; }
        void OnOpen();
        void Close();
    }

    /// <summary>
    /// 轻量页面栈：Push 压入并隐藏当前页，Pop 返回上一页，Replace 整体替换。
    /// 不做多余抽象——只解决"一次显示一个页面 + 返回"这一件事。
    /// </summary>
    public sealed class UiPageStack
    {
        private readonly List<IPage> _stack = new List<IPage>();

        public IPage Current
        {
            get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; }
        }

        public int Count
        {
            get { return _stack.Count; }
        }

        public void Push(IPage page)
        {
            if (page == null)
            {
                UiLog.Warn("Push 收到空页面，已忽略。");
                return;
            }
            if (Current != null)
            {
                Current.OnExit();
                Current.Root.gameObject.SetActive(false);
            }
            _stack.Add(page);
            page.Root.gameObject.SetActive(true);
            page.OnEnter();
            UiLog.Info("页面压栈 -> " + page.PageName);
        }

        public void Replace(IPage page)
        {
            if (page == null)
            {
                UiLog.Warn("Replace 收到空页面，已忽略。");
                return;
            }
            for (int i = 0; i < _stack.Count; i++)
            {
                _stack[i].OnExit();
                _stack[i].Root.gameObject.SetActive(false);
            }
            _stack.Clear();
            _stack.Add(page);
            page.Root.gameObject.SetActive(true);
            page.OnEnter();
            UiLog.Info("页面整体替换 -> " + page.PageName);
        }

        /// <summary>弹出栈顶并显示上一页。仅剩一页时返回 false（不弹根页）。</summary>
        public bool Pop()
        {
            if (_stack.Count <= 1)
            {
                return false;
            }
            IPage top = _stack[_stack.Count - 1];
            top.OnExit();
            top.Root.gameObject.SetActive(false);
            _stack.RemoveAt(_stack.Count - 1);
            IPage now = Current;
            now.Root.gameObject.SetActive(true);
            now.OnEnter();
            UiLog.Info("页面返回 -> " + now.PageName);
            return true;
        }
    }

    /// <summary>
    /// 弹窗栈：独立于页面栈。Open 压入，CloseTop 关闭栈顶，ClearAll 全关。
    /// </summary>
    public sealed class UiDialogStack
    {
        private readonly List<IDialog> _stack = new List<IDialog>();

        /// <summary>弹窗挂载层：所有弹窗都挂在同一个层上（比页面层晚渲染）。由 Bootstrap 装配。</summary>
        public RectTransform Layer { get; set; }

        public int Count
        {
            get { return _stack.Count; }
        }

        public void Open(IDialog dialog)
        {
            if (dialog == null)
            {
                UiLog.Warn("Open 收到空弹窗，已忽略。");
                return;
            }
            _stack.Add(dialog);
            dialog.Root.gameObject.SetActive(true);
            dialog.OnOpen();
            UiLog.Info("弹窗打开 -> " + dialog.Root.name);
        }

        public void CloseTop()
        {
            if (_stack.Count == 0)
            {
                return;
            }
            IDialog top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            top.Root.gameObject.SetActive(false);
            top.Close();
            UiLog.Info("弹窗关闭 -> " + top.Root.name);
        }

        public void ClearAll()
        {
            while (_stack.Count > 0)
            {
                CloseTop();
            }
        }
    }

    /// <summary>主题色板：暗色地牢 + 金色点缀的统一色调。</summary>
    public static class UiTheme
    {
        public static readonly Color Night = new Color(0.045f, 0.045f, 0.08f, 1f);
        public static readonly Color NightSoft = new Color(0.085f, 0.082f, 0.14f, 0.96f);
        public static readonly Color Stone = new Color(0.20f, 0.18f, 0.25f, 0.98f);
        public static readonly Color StoneLight = new Color(0.34f, 0.30f, 0.38f, 1f);
        public static readonly Color Gold = new Color(0.96f, 0.79f, 0.35f, 1f);
        public static readonly Color GoldDeep = new Color(0.58f, 0.40f, 0.12f, 1f);
        public static readonly Color Flame = new Color(0.95f, 0.42f, 0.22f, 1f);
        public static readonly Color Frost = new Color(0.42f, 0.78f, 0.92f, 1f);
        public static readonly Color Paper = new Color(0.93f, 0.91f, 0.85f, 1f);
        public static readonly Color PaperMuted = new Color(0.62f, 0.60f, 0.58f, 1f);
        public static readonly Color Ink = new Color(0.12f, 0.10f, 0.15f, 1f);
        public static readonly Color Locked = new Color(0.15f, 0.15f, 0.19f, 0.94f);
        public static readonly Color Success = new Color(0.47f, 0.80f, 0.46f, 1f);
        public static readonly Color Mask = new Color(0f, 0f, 0f, 0.62f);
    }

    /// <summary>
    /// UI 构建工厂：所有界面都用这套小工具构建，保证统一的字体/排版/九宫格行为。
    /// 字体由 Bootstrap 在装配时注入，缺失时回退内置 Arial 并告警。
    /// </summary>
    public static class UiFactory
    {
        public static Font DefaultFont = null;
        public static Font PixelFont = null;

        public static Font ResolveFont()
        {
            if (DefaultFont != null)
            {
                return DefaultFont;
            }
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>创建一个空 RectTransform 挂点。</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>创建纯色面板（Image）。</summary>
        public static Image CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>创建带 Sprite 的面板。</summary>
        public static Image CreateSpritePanel(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
        {
            Image image = CreatePanel(name, parent, color, false);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            return image;
        }

        /// <summary>创建文本。</summary>
        public static Text CreateText(string name, Transform parent, string content, int size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = content;
            text.font = ResolveFont();
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>创建按钮（纯色底 + 可选图标 + 文案）。</summary>
        public static Button CreateButton(string name, Transform parent, string label, int fontSize,
            Color background, Color foreground, bool bold, Sprite icon = null, float iconSize = 0f)
        {
            Image image = CreatePanel(name, parent, background, true);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            RectTransform rt = image.rectTransform;
            if (!string.IsNullOrEmpty(label))
            {
                Text text = CreateText("Label", rt, label, fontSize, foreground);
                Stretch((RectTransform)text.transform);
                text.alignment = TextAnchor.MiddleCenter;
                text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            }
            return button;
        }

        /// <summary>铺满父节点。</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>以左下角为基准设置尺寸与位置（与 GameHomeUI 相同的排版约定）。</summary>
        public static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        /// <summary>创建可滚动的视口（用于列表/弹窗内滚动内容）。</summary>
        public static ScrollRect CreateScrollRect(string name, Transform parent, Color viewportColor,
            out RectTransform content)
        {
            RectTransform root = CreateRect(name, parent);
            Stretch(root);

            Image viewport = CreatePanel("Viewport", root, viewportColor, false);
            Stretch(viewport.rectTransform);
            RectTransform viewportRt = viewport.rectTransform;

            // 遮罩：限制内容显示范围
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            Image contentImage = CreatePanel("Content", viewportRt, new Color(1f, 1f, 1f, 0f), false);
            content = contentImage.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return scroll;
        }
    }

    /// <summary>
    /// Bootstrap 装配的资源集合：由 UiShowcaseBootstrap 从序列化字段注入，
    /// 页面与弹窗只依赖这份资源表，不直接持有 AssetDatabase 引用。
    /// </summary>
    public sealed class UiShowcaseAssets
    {
        public Font ChineseFont;
        public Font PixelFont;
        public Sprite DungeonBg;
        public Sprite StoneTile;
        public Sprite Chandelier;
        public Sprite HeroIdle;
        public Sprite FrameWhite;
        public Sprite FramePlain;
        public Sprite GoldCoin;
        public Sprite HealthFill;
        public Sprite HeartFull;
        public Sprite HeartEmpty;
        public Sprite StaminaFull;
        public Sprite StaminaEmpty;
        public Sprite StaminaGlobe;
        public Sprite HeartPickup;
        public Sprite InvBox;
        public Sprite UiBox;
        public Sprite Sword;
        public Sprite Bow;
        public Sprite Staff;
        public Sprite Arrow;
        public Sprite Building1;
        public Sprite Building1Base;
        public Sprite Building1Roof;
        public Sprite Building2;
        public Sprite Bulletin;
        public Sprite Sign;
        public Sprite Tree;
        public Sprite Bush;
        public Sprite TorchBase;
        public Sprite Ray;
        public AudioClip Bgm;
    }
}