using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UiShowcase
{
    /// <summary>进度条组件：底槽 + 填充条 + 可选百分比文字。</summary>
    public sealed class ProgressBar
    {
        public Image Fill { get; private set; }
        public Text Label { get; private set; }
        private readonly RectTransform _root;

        public ProgressBar(RectTransform root)
        {
            _root = root;
        }

        public static ProgressBar Create(string name, Transform parent, Sprite trackSprite, Sprite fillSprite,
            Color trackColor, Color fillColor, float height, out RectTransform root)
        {
            root = UiFactory.CreateRect(name, parent);
            UiFactory.SetRect(root, 0f, 0f, 600f, height);

            Image track = UiFactory.CreatePanel("Track", root, trackColor, false);
            UiFactory.Stretch(track.rectTransform);
            if (trackSprite != null)
            {
                track.sprite = trackSprite;
                track.type = Image.Type.Simple;
                track.preserveAspect = false;
            }

            RectTransform fillWrap = UiFactory.CreateRect("FillWrap", root);
            fillWrap.anchorMin = new Vector2(0f, 0f);
            fillWrap.anchorMax = new Vector2(1f, 1f);
            fillWrap.offsetMin = new Vector2(2f, 2f);
            fillWrap.offsetMax = new Vector2(-2f, -2f);

            Image fill = UiFactory.CreatePanel("Fill", fillWrap, fillColor, false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            if (fillSprite != null)
            {
                fill.sprite = fillSprite;
                fill.type = Image.Type.Filled;
            }
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            Text label = UiFactory.CreateText("ValueLabel", root, "0%", 18, UiTheme.Paper);
            UiFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;

            return new ProgressBar(root) { Fill = fill, Label = label };
        }

        public void SetProgress(float amount01)
        {
            float clamped = Mathf.Clamp01(amount01);
            if (Fill != null)
            {
                Fill.fillAmount = clamped;
            }
            if (Label != null)
            {
                Label.text = Mathf.RoundToInt(clamped * 100f) + "%";
            }
        }
    }

    /// <summary>标签页组件：一行 Tab，切换时高亮当前项并回调。</summary>
    public sealed class TabBar
    {
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<Color> _normalColors = new List<Color>();
        private readonly Color _activeColor;
        private readonly Color _inactiveColor;
        private int _selected = -1;

        public event Action<int> OnTabSelected;

        public TabBar(Color activeColor, Color inactiveColor)
        {
            _activeColor = activeColor;
            _inactiveColor = inactiveColor;
        }

        public RectTransform Build(string name, Transform parent, string[] tabs, float width, float height)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            UiFactory.SetRect(root, 0f, 0f, width, height);

            float gap = 8f;
            float tabWidth = (width - gap * (tabs.Length - 1)) / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                Button button = UiFactory.CreateButton("Tab_" + tabs[i], root, tabs[i], 24,
                    _inactiveColor, UiTheme.Paper, true);
                UiFactory.SetRect(button.GetComponent<RectTransform>(), i * (tabWidth + gap), 0f, tabWidth, height);
                _buttons.Add(button);
                _normalColors.Add(_inactiveColor);
                button.onClick.AddListener(() => Select(index));
            }

            if (tabs.Length > 0)
            {
                Select(0);
            }
            return root;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _buttons.Count || index == _selected)
            {
                if (index >= 0 && index < _buttons.Count && index == _selected)
                {
                    return;
                }
                if (index < 0 || index >= _buttons.Count)
                {
                    return;
                }
            }
            _selected = index;
            for (int i = 0; i < _buttons.Count; i++)
            {
                Image img = _buttons[i].GetComponent<Image>();
                img.color = i == _selected ? _activeColor : _normalColors[i];
                Text label = _buttons[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.color = i == _selected ? UiTheme.Ink : UiTheme.Paper;
                }
            }
            if (OnTabSelected != null)
            {
                OnTabSelected(index);
            }
        }

        public int Selected
        {
            get { return _selected; }
        }
    }

    /// <summary>信息列表行：标题 + 时间 + 摘要，整行可点击。</summary>
    public static class ListRow
    {
        public static Button Create(string name, Transform parent, string title, string time, string summary,
            float width, float height, Sprite frameSprite)
        {
            Button button = UiFactory.CreateButton(name, parent, string.Empty, 0, UiTheme.NightSoft, UiTheme.Paper, false);
            UiFactory.SetRect(button.GetComponent<RectTransform>(), 0f, 0f, width, height);

            Image bg = button.GetComponent<Image>();
            if (frameSprite != null)
            {
                bg.sprite = frameSprite;
                bg.color = new Color(1f, 1f, 1f, 0.92f);
            }

            RectTransform rt = button.GetComponent<RectTransform>();
            Text titleText = UiFactory.CreateText("Title", rt, title, 26, UiTheme.Paper);
            UiFactory.SetRect(titleText.rectTransform, 24f, height - 52f, width - 48f, 34f);
            titleText.fontStyle = FontStyle.Bold;

            Text timeText = UiFactory.CreateText("Time", rt, time, 16, UiTheme.PaperMuted);
            UiFactory.SetRect(timeText.rectTransform, 24f, height - 40f, width * 0.5f, 24f);
            timeText.alignment = TextAnchor.MiddleRight;
            timeText.rectTransform.anchorMin = new Vector2(0f, 0f);
            timeText.rectTransform.anchorMax = new Vector2(1f, 1f);
            timeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            timeText.rectTransform.anchoredPosition = new Vector2(-20f, 0f);

            Text summaryText = UiFactory.CreateText("Summary", rt, summary, 19, UiTheme.PaperMuted);
            UiFactory.SetRect(summaryText.rectTransform, 24f, 18f, width - 120f, 30f);

            Text arrowText = UiFactory.CreateText("Arrow", rt, ">", 26, UiTheme.Gold);
            UiFactory.SetRect(arrowText.rectTransform, width - 44f, 0f, 30f, height);
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.raycastTarget = true;

            return button;
        }
    }

    /// <summary>
    /// 模态弹窗基类：遮罩 + 居中面板 + 关闭按钮 + 遮罩点击关闭。
    /// 弹窗内容在 Content 下构建，Content 内可再放滚动区域。
    /// </summary>
    public class ModalDialog : IDialog
    {
        public RectTransform Root { get; private set; }
        protected RectTransform Content;
        protected UiDialogStack DialogStack;
        protected UiShowcaseAssets Assets;

        private readonly Button _closeButton;
        private readonly Image _mask;

        protected ModalDialog(UiDialogStack dialogStack, UiShowcaseAssets assets, RectTransform layer,
            string dialogName, float panelWidth, float panelHeight)
        {
            DialogStack = dialogStack;
            Assets = assets;

            Root = UiFactory.CreateRect(dialogName, layer);
            UiFactory.Stretch(Root);
            Root.gameObject.SetActive(false);

            _mask = UiFactory.CreatePanel("Mask", Root, UiTheme.Mask, true);
            UiFactory.Stretch(_mask.rectTransform);
            Button maskButton = _mask.gameObject.AddComponent<Button>();
            maskButton.transition = Selectable.Transition.None;
            maskButton.onClick.AddListener(() => RequestClose());

            Image panel = UiFactory.CreatePanel("Panel", Root, UiTheme.NightSoft, true);
            UiFactory.SetRect(panel.rectTransform, 0f, 0f, panelWidth, panelHeight);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchoredPosition = Vector2.zero;
            if (Assets.FrameWhite != null)
            {
                panel.sprite = Assets.FrameWhite;
                panel.color = new Color(1f, 1f, 1f, 1f);
            }

            Content = UiFactory.CreateRect("Content", panel.rectTransform);
            UiFactory.Stretch(Content);

            // 关闭按钮（右上角）
            _closeButton = UiFactory.CreateButton("CloseButton", panel.rectTransform, "✕", 28,
                new Color(0.30f, 0.26f, 0.38f, 1f), UiTheme.Paper, true);
            RectTransform closeRt = _closeButton.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.sizeDelta = new Vector2(44f, 44f);
            closeRt.anchoredPosition = new Vector2(-26f, -26f);
            _closeButton.onClick.AddListener(() => RequestClose());
        }

        /// <summary>弹窗自身请求关闭：关闭整层栈顶（自己）。</summary>
        protected void RequestClose()
        {
            if (DialogStack != null)
            {
                DialogStack.CloseTop();
            }
        }

        public virtual void OnOpen() { }

        public virtual void Close() { }
    }

    /// <summary>简单提示弹窗（标题 + 文案 + 确定）。</summary>
    public sealed class ToastDialog : ModalDialog
    {
        public ToastDialog(UiDialogStack dialogStack, UiShowcaseAssets assets, RectTransform layer,
            string title, string message)
            : base(dialogStack, assets, layer, "ToastDialog", 640f, 360f)
        {
            Text titleText = UiFactory.CreateText("Title", Content, title, 34, UiTheme.Gold);
            UiFactory.SetRect(titleText.rectTransform, 40f, 268f, 560f, 48f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            Text messageText = UiFactory.CreateText("Message", Content, message, 24, UiTheme.Paper);
            UiFactory.SetRect(messageText.rectTransform, 60f, 140f, 520f, 90f);
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.verticalOverflow = VerticalWrapMode.Overflow;

            Button ok = UiFactory.CreateButton("OK", Content, "确 定", 26, UiTheme.GoldDeep, UiTheme.Paper, true);
            UiFactory.SetRect(ok.GetComponent<RectTransform>(), 250f, 36f, 180f, 56f);
            ok.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            ok.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            ok.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            ok.onClick.AddListener(() => RequestClose());
        }
    }
}