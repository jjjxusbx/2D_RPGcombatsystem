using System;
using UnityEngine;
using UnityEngine.UI;

namespace UiShowcase
{
    /// <summary>
    /// 界面内容层：Loading / 主界面 / 信息页 / 关卡选择 + 活动弹窗 + 关卡详情弹窗。
    /// 页面只依赖 UiPageStack / UiDialogStack / UiShowcaseAssets，不持有全局单例。
    /// </summary>

    // ---------------------------------------------------------------- Loading 页

    public sealed class LoadingPage : IPage
    {
        public string PageName { get { return "Loading"; } }
        public RectTransform Root { get; private set; }

        private readonly UiShowcaseAssets _assets;
        private readonly Action _onLoadingDone;
        private ProgressBar _progress;
        private Text _statusText;
        private Text _tipText;
        private float _elapsed;

        private static readonly string[] Tips =
        {
            "提示：深渊每层都有隐藏宝箱，留意墙角的微光。",
            "提示：合理携带火炬可以降低遭遇怪物的概率。",
            "提示：死亡不会丢失已解锁楼层，放心探索吧。",
        };

        public LoadingPage(UiShowcaseAssets assets, RectTransform layer, Action onLoadingDone)
        {
            _assets = assets;
            _onLoadingDone = onLoadingDone;
            Root = UiFactory.CreateRect("Page_Loading", layer);
            UiFactory.Stretch(Root);
            Build();
            Root.gameObject.SetActive(false);
        }

        private void Build()
        {
            Image bg = UiFactory.CreatePanel("Bg", Root, UiTheme.Night, false);
            UiFactory.Stretch(bg.rectTransform);
            if (_assets.DungeonBg != null)
            {
                bg.sprite = _assets.DungeonBg;
                bg.color = new Color(1f, 1f, 1f, 0.22f);
            }

            Image vignette = UiFactory.CreatePanel("Vignette", Root, new Color(0f, 0f, 0f, 0.42f), false);
            UiFactory.Stretch(vignette.rectTransform);

            if (_assets.Chandelier != null)
            {
                Image chandelier = UiFactory.CreateSpritePanel("Chandelier", Root, _assets.Chandelier,
                    new Color(1f, 0.9f, 0.7f, 0.5f), true);
                UiFactory.SetRect(chandelier.rectTransform, 0f, 0f, 220f, 220f);
                chandelier.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                chandelier.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                chandelier.rectTransform.pivot = new Vector2(0.5f, 1f);
                chandelier.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            }

            Text title = UiFactory.CreateText("Title", Root, "地 牢 远 征", 100, UiTheme.Gold);
            UiFactory.SetRect(title.rectTransform, 0f, 0f, 1100f, 130f);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 210f);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;

            Text subtitle = UiFactory.CreateText("Subtitle", Root, "DUNGEON  EXPEDITION", 30, UiTheme.PaperMuted);
            UiFactory.SetRect(subtitle.rectTransform, 0f, 0f, 800f, 44f);
            subtitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 140f);
            subtitle.alignment = TextAnchor.MiddleCenter;

            Text loading = UiFactory.CreateText("Loading", Root, "正在加载深渊档案…", 26, UiTheme.Paper);
            UiFactory.SetRect(loading.rectTransform, 0f, 0f, 700f, 40f);
            loading.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            loading.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            loading.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            loading.rectTransform.anchoredPosition = new Vector2(0f, 48f);
            loading.alignment = TextAnchor.MiddleCenter;

            _statusText = UiFactory.CreateText("Status", Root, "初始化…", 22, UiTheme.PaperMuted);
            UiFactory.SetRect(_statusText.rectTransform, 0f, 0f, 700f, 34f);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _statusText.rectTransform.anchoredPosition = new Vector2(0f, -96f);
            _statusText.alignment = TextAnchor.MiddleCenter;

            _progress = ProgressBar.Create("Progress", Root, _assets.FramePlain, _assets.HealthFill,
                new Color(0.16f, 0.15f, 0.22f, 0.95f), UiTheme.Gold, 30f, out RectTransform progressRoot);
            UiFactory.SetRect(progressRoot, 0f, 0f, 760f, 30f);
            progressRoot.anchorMin = new Vector2(0.5f, 0.5f);
            progressRoot.anchorMax = new Vector2(0.5f, 0.5f);
            progressRoot.pivot = new Vector2(0.5f, 0.5f);
            progressRoot.anchoredPosition = new Vector2(0f, -40f);

            _tipText = UiFactory.CreateText("Tip", Root, Tips[0], 22, UiTheme.PaperMuted);
            UiFactory.SetRect(_tipText.rectTransform, 0f, 0f, 1200f, 34f);
            _tipText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _tipText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _tipText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _tipText.rectTransform.anchoredPosition = new Vector2(0f, 70f);
            _tipText.alignment = TextAnchor.MiddleCenter;

            Text version = UiFactory.CreateText("Version", Root, "v0.1.0 演示版", 18, UiTheme.PaperMuted);
            UiFactory.SetRect(version.rectTransform, 24f, 20f, 240f, 28f);
        }

        public void OnEnter()
        {
            _elapsed = 0f;
            _progress.SetProgress(0f);
            _statusText.text = "初始化…";
        }

        public void OnExit() { }

        public void TickLoading()
        {
            if (_elapsed >= 1f)
            {
                return;
            }
            _elapsed += Time.deltaTime / 3.2f;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(_elapsed), 2.2f); // easeOutQuad 风格
            _progress.SetProgress(eased);

            float p = eased * 100f;
            if (p < 25f)
            {
                _statusText.text = "读取地牢地图…";
            }
            else if (p < 50f)
            {
                _statusText.text = "召唤勇士小队…";
            }
            else if (p < 80f)
            {
                _statusText.text = "淬炼武器与符石…";
            }
            else
            {
                _statusText.text = "点燃深渊入口的火炬…";
            }

            int tipIndex = Mathf.FloorToInt(Time.time * 0.7f) % Tips.Length;
            _tipText.text = Tips[tipIndex];

            if (eased >= 1f)
            {
                _statusText.text = "加载完成";
                if (_onLoadingDone != null)
                {
                    _onLoadingDone();
                }
            }
        }
    }

    // ---------------------------------------------------------------- 主界面

    public sealed class MainMenuPage : IPage
    {
        public string PageName { get { return "MainMenu"; } }
        public RectTransform Root { get; private set; }

        private readonly UiShowcaseAssets _assets;
        private readonly UiPageStack _pages;
        private readonly UiDialogStack _dialogs;

        public MainMenuPage(UiShowcaseAssets assets, UiPageStack pages, UiDialogStack dialogs, RectTransform layer)
        {
            _assets = assets;
            _pages = pages;
            _dialogs = dialogs;
            Root = UiFactory.CreateRect("Page_MainMenu", layer);
            UiFactory.Stretch(Root);
            Build();
            Root.gameObject.SetActive(false);
        }

        private void Build()
        {
            // 分层背景：底色 + 地牢场景 + 暗角 + 底部建筑剪影
            Image bg = UiFactory.CreatePanel("Bg", Root, UiTheme.Night, false);
            UiFactory.Stretch(bg.rectTransform);
            if (_assets.DungeonBg != null)
            {
                bg.sprite = _assets.DungeonBg;
                bg.color = new Color(1f, 1f, 1f, 0.30f);
            }

            Image vignette = UiFactory.CreatePanel("Vignette", Root, new Color(0f, 0f, 0f, 0.38f), false);
            UiFactory.Stretch(vignette.rectTransform);

            if (_assets.Building1Base != null)
            {
                Image silhouette = UiFactory.CreateSpritePanel("Silhouette_L", Root, _assets.Building1Base,
                    new Color(0.05f, 0.05f, 0.10f, 0.85f), true);
                UiFactory.SetRect(silhouette.rectTransform, -60f, 0f, 420f, 260f);
                silhouette.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                silhouette.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                silhouette.rectTransform.pivot = new Vector2(0f, 0f);
                silhouette.rectTransform.anchoredPosition = new Vector2(-480f, 0f);
            }
            if (_assets.Building2 != null)
            {
                Image silhouette = UiFactory.CreateSpritePanel("Silhouette_R", Root, _assets.Building2,
                    new Color(0.05f, 0.05f, 0.10f, 0.85f), true);
                UiFactory.SetRect(silhouette.rectTransform, 0f, 0f, 420f, 300f);
                silhouette.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                silhouette.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                silhouette.rectTransform.pivot = new Vector2(1f, 0f);
                silhouette.rectTransform.anchoredPosition = new Vector2(470f, 0f);
            }

            BuildBanner();
            BuildPlayerBar();
            BuildNavRow();

            Text version = UiFactory.CreateText("Version", Root, "v0.1.0 演示版", 18, UiTheme.PaperMuted);
            UiFactory.SetRect(version.rectTransform, 24f, 16f, 240f, 26f);
        }

        private void BuildBanner()
        {
            Image banner = UiFactory.CreatePanel("Banner", Root, new Color(0.12f, 0.10f, 0.18f, 0.92f), true);
            UiFactory.SetRect(banner.rectTransform, 0f, 0f, 1240f, 250f);
            banner.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            banner.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            banner.rectTransform.pivot = new Vector2(0.5f, 1f);
            banner.rectTransform.anchoredPosition = new Vector2(0f, -26f);
            if (_assets.FrameWhite != null)
            {
                banner.sprite = _assets.FrameWhite;
                banner.color = new Color(1f, 1f, 1f, 1f);
            }

            Image hero = UiFactory.CreateSpritePanel("HeroArt", banner.rectTransform, _assets.HeroIdle,
                Color.white, true);
            UiFactory.SetRect(hero.rectTransform, 0f, 0f, 260f, 230f);
            hero.rectTransform.anchorMin = new Vector2(1f, 0f);
            hero.rectTransform.anchorMax = new Vector2(1f, 0f);
            hero.rectTransform.pivot = new Vector2(1f, 0f);
            hero.rectTransform.anchoredPosition = new Vector2(-20f, 8f);

            Text title = UiFactory.CreateText("BannerTitle", banner.rectTransform, "黑暗裂隙 · 深渊远征", 48, UiTheme.Gold);
            UiFactory.SetRect(title.rectTransform, 0f, 0f, 880f, 64f);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(40f, -38f);
            title.fontStyle = FontStyle.Bold;

            Text subtitle = UiFactory.CreateText("BannerSub", banner.rectTransform, "地牢勇士集结，向深渊更深处进发！", 24, UiTheme.Paper);
            UiFactory.SetRect(subtitle.rectTransform, 0f, 0f, 820f, 40f);
            subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(0f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(40f, -120f);

            Button cta = UiFactory.CreateButton("BannerCta", banner.rectTransform, "立即出发 →", 26, UiTheme.GoldDeep, UiTheme.Paper, true);
            UiFactory.SetRect(cta.GetComponent<RectTransform>(), 0f, 0f, 220f, 64f);
            cta.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            cta.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0f);
            cta.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
            cta.GetComponent<RectTransform>().anchoredPosition = new Vector2(40f, 34f);
            // 导航行为由 BindNavigation 注入（避免构造期循环依赖）
        }

        private void BuildPlayerBar()
        {
            Image bar = UiFactory.CreatePanel("PlayerBar", Root, new Color(0.09f, 0.08f, 0.14f, 0.94f), false);
            UiFactory.SetRect(bar.rectTransform, 0f, 0f, 1240f, 118f);
            bar.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            bar.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.anchoredPosition = new Vector2(0f, -296f);
            if (_assets.InvBox != null)
            {
                bar.sprite = _assets.InvBox;
                bar.color = new Color(1f, 1f, 1f, 0.92f);
            }

            Image avatar = UiFactory.CreateSpritePanel("Avatar", bar.rectTransform, _assets.HeroIdle, Color.white, true);
            UiFactory.SetRect(avatar.rectTransform, 24f, 22f, 84f, 84f);

            Text name = UiFactory.CreateText("Name", bar.rectTransform, "旅人 · 阿莱克", 28, UiTheme.Paper);
            UiFactory.SetRect(name.rectTransform, 130f, 74f, 320f, 38f);
            name.fontStyle = FontStyle.Bold;

            Text level = UiFactory.CreateText("Level", bar.rectTransform, "Lv.12  战力 1,280", 20, UiTheme.Frost);
            UiFactory.SetRect(level.rectTransform, 130f, 28f, 320f, 30f);

            AddResourceIcon(bar.rectTransform, _assets.GoldCoin, "12,480", 520f);
            AddResourceIcon(bar.rectTransform, _assets.StaminaGlobe, "320", 760f);
            AddResourceIcon(bar.rectTransform, _assets.HeartFull, "240 / 240", 1000f);
        }

        private void AddResourceIcon(RectTransform parent, Sprite icon, string value, float x)
        {
            if (icon != null)
            {
                Image iconImage = UiFactory.CreateSpritePanel("ResIcon", parent, icon, Color.white, true);
                UiFactory.SetRect(iconImage.rectTransform, x, 42f, 40f, 40f);
            }
            Text valueText = UiFactory.CreateText("ResValue", parent, value, 24, UiTheme.Paper);
            UiFactory.SetRect(valueText.rectTransform, x + 48f, 44f, 200f, 36f);
            valueText.fontStyle = FontStyle.Bold;
        }

        private void BuildNavRow()
        {
            RectTransform row = UiFactory.CreateRect("NavRow", Root);
            UiFactory.SetRect(row, 0f, 0f, 1100f, 132f);
            row.anchorMin = new Vector2(0.5f, 0f);
            row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, 60f);

            float w = 200f, gap = 24f, h = 132f;
            BuildNavButton(row, "关卡", "深入地牢", _assets.Sword, UiTheme.GoldDeep, 0f, w, h);
            BuildNavButton(row, "公告", "消息中心", _assets.Bulletin, new Color(0.24f, 0.22f, 0.32f, 1f), 1 * (w + gap), w, h);
            BuildNavButton(row, "活动", "限时开启", _assets.HeartPickup, UiTheme.Flame, 2 * (w + gap), w, h);
            BuildNavButton(row, "背包", "随身物品", _assets.InvBox, new Color(0.24f, 0.22f, 0.32f, 1f), 3 * (w + gap), w, h);
            BuildNavButton(row, "设置", "系统选项", null, new Color(0.24f, 0.22f, 0.32f, 1f), 4 * (w + gap), w, h);
        }

        private void BuildNavButton(RectTransform parent, string title, string sub, Sprite icon, Color color,
            float x, float w, float h)
        {
            Button button = UiFactory.CreateButton("Nav_" + title, parent, string.Empty, 0, color, UiTheme.Paper, false);
            UiFactory.SetRect(button.GetComponent<RectTransform>(), x, 0f, w, h);
            RectTransform rt = button.GetComponent<RectTransform>();

            if (icon != null)
            {
                Image iconImage = UiFactory.CreateSpritePanel("Icon", rt, icon, UiTheme.Gold, true);
                UiFactory.SetRect(iconImage.rectTransform, w * 0.5f - 24f, 84f, 48f, 48f);
            }

            Text titleText = UiFactory.CreateText("Title", rt, title, 28, UiTheme.Paper);
            UiFactory.SetRect(titleText.rectTransform, 0f, 40f, w, 38f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            Text subText = UiFactory.CreateText("Sub", rt, sub, 17, UiTheme.PaperMuted);
            UiFactory.SetRect(subText.rectTransform, 0f, 12f, w, 26f);
            subText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>由 Bootstrap 注入导航行为（避免构造期循环依赖）。</summary>
        public void BindNavigation(Action onLevels, Action onInfo, Action onEvent, Action onBackpack, Action onSettings)
        {
            Button levels = Root.Find("NavRow/Nav_关卡").GetComponent<Button>();
            levels.onClick.AddListener(() => { if (onLevels != null) onLevels(); });

            Button info = Root.Find("NavRow/Nav_公告").GetComponent<Button>();
            info.onClick.AddListener(() => { if (onInfo != null) onInfo(); });

            Button eventBtn = Root.Find("NavRow/Nav_活动").GetComponent<Button>();
            eventBtn.onClick.AddListener(() => { if (onEvent != null) onEvent(); });

            Button backpack = Root.Find("NavRow/Nav_背包").GetComponent<Button>();
            backpack.onClick.AddListener(() => { if (onBackpack != null) onBackpack(); });

            Button settings = Root.Find("NavRow/Nav_设置").GetComponent<Button>();
            settings.onClick.AddListener(() => { if (onSettings != null) onSettings(); });

            Button cta = Root.Find("Banner/BannerCta").GetComponent<Button>();
            cta.onClick.RemoveAllListeners();
            cta.onClick.AddListener(() => { if (onLevels != null) onLevels(); });
        }

        public void OnEnter() { }

        public void OnExit() { }
    }

    // ---------------------------------------------------------------- 信息页

    public sealed class InfoPage : IPage
    {
        public string PageName { get { return "Info"; } }
        public RectTransform Root { get; private set; }

        private readonly UiShowcaseAssets _assets;
        private readonly UiPageStack _pages;
        private readonly UiDialogStack _dialogs;
        private RectTransform _listContent;
        private ScrollRect _scroll;

        private static readonly string[][] TabTitles =
        {
            new[] { "地牢版本 0.1 上线公告", "每日维护时间调整通知", "深渊排行榜功能预告" },
            new[] { "v0.1.0 更新日志", "v0.1.1 修复说明", "平衡性调整：第二层怪物" },
            new[] { "来自系统的新邮件", "深渊领主的挑战书", "勇士工会的邀请函" },
        };

        private static readonly string[][] TabTimes =
        {
            new[] { "08-15 20:00", "08-14 10:30", "08-12 18:45" },
            new[] { "08-16 09:00", "08-13 16:20", "08-10 11:05" },
            new[] { "08-16 07:40", "08-15 22:15", "08-14 14:50" },
        };

        private static readonly string[][] TabSummaries =
        {
            new[] { "地牢远征首个演示版本已上线，共 3 层可探索。", "服务器每周三 04:00-05:00 例行维护。", "排行榜系统正在制作中，敬请期待。" },
            new[] { "新增 Loading 进度表现与主界面 Banner。", "修复了第二层房间解锁状态不刷新问题。", "降低腐化墓室怪物攻击频率。" },
            new[] { "欢迎加入地牢远征，初始装备已发放。", "深渊领主邀请你挑战本周 BOSS 房间。", "工会系统开放预约。" },
        };

        public InfoPage(UiShowcaseAssets assets, UiPageStack pages, UiDialogStack dialogs, RectTransform layer)
        {
            _assets = assets;
            _pages = pages;
            _dialogs = dialogs;
            Root = UiFactory.CreateRect("Page_Info", layer);
            UiFactory.Stretch(Root);
            Build();
            Root.gameObject.SetActive(false);
        }

        private void Build()
        {
            Image bg = UiFactory.CreatePanel("Bg", Root, UiTheme.Night, false);
            UiFactory.Stretch(bg.rectTransform);

            // 顶栏：返回 + 标题
            Button back = UiFactory.CreateButton("Back", Root, "←  返回", 24, UiTheme.Stone, UiTheme.Paper, true);
            UiFactory.SetRect(back.GetComponent<RectTransform>(), 28f, 998f, 150f, 54f);
            back.onClick.AddListener(() => _pages.Pop());

            Text title = UiFactory.CreateText("Title", Root, "消息中心", 40, UiTheme.Gold);
            UiFactory.SetRect(title.rectTransform, 200f, 1000f, 300f, 52f);
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;

            // 标签页
            TabBar tabBar = new TabBar(UiTheme.Gold, UiTheme.Stone);
            tabBar.Build("TabBar", Root, new[] { "公告", "版本更新", "邮件" }, 620f, 56f);
            RectTransform tabRt = Root.Find("TabBar") as RectTransform;
            UiFactory.SetRect(tabRt, 180f, 920f, 620f, 56f);
            tabBar.OnTabSelected += RenderList;

            // 列表区域
            _scroll = UiFactory.CreateScrollRect("ListScroll", Root, new Color(0f, 0f, 0f, 0f), out RectTransform content);
            RectTransform scrollRt = _scroll.GetComponent<RectTransform>();
            UiFactory.SetRect(scrollRt, 160f, 130f, 1600f, 760f);
            _listContent = content;
            _listContent.sizeDelta = new Vector2(0f, 0f);

            RenderList(0);
        }

        private void RenderList(int tabIndex)
        {
            if (_listContent == null)
            {
                return;
            }
            // 清空旧行
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_listContent.GetChild(i).gameObject);
            }

            string[] titles = TabTitles[tabIndex];
            string[] times = TabTimes[tabIndex];
            string[] summaries = TabSummaries[tabIndex];
            float rowHeight = 120f;
            float rowWidth = 1560f;
            float gap = 16f;

            _listContent.sizeDelta = new Vector2(0f, titles.Length * (rowHeight + gap) + gap);

            for (int i = 0; i < titles.Length; i++)
            {
                int index = i;
                Button row = ListRow.Create("Row_" + i, _listContent, titles[i], times[i], summaries[i],
                    rowWidth, rowHeight, _assets.FramePlain);
                UiFactory.SetRect(row.GetComponent<RectTransform>(), 0f,
                    (_listContent.sizeDelta.y - rowHeight - gap) - i * (rowHeight + gap), rowWidth, rowHeight);
                row.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
                row.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
                row.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
                row.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f,
                    -gap - i * (rowHeight + gap));
                row.onClick.AddListener(() => OpenDetail(tabIndex, index));
            }
        }

        private void OpenDetail(int tabIndex, int index)
        {
            string title = TabTitles[tabIndex][index];
            string time = TabTimes[tabIndex][index];
            string summary = TabSummaries[tabIndex][index];
            string detail = title + "\n" + time + "\n\n" + summary + "\n\n（这是" + title + "的详细内容，用于展示信息页的排版与交互。作品集中可在此扩展富文本、图片与跳转链接。）";
            ToastDialog dialog = new ToastDialog(_dialogs, _assets, _dialogs.Layer, title, detail);
            _dialogs.Open(dialog);
        }

        public void OnEnter() { }

        public void OnExit() { }
    }

    // ---------------------------------------------------------------- 关卡选择页

    public sealed class LevelSelectPage : IPage
    {
        public string PageName { get { return "LevelSelect"; } }
        public RectTransform Root { get; private set; }

        private readonly UiShowcaseAssets _assets;
        private readonly UiPageStack _pages;
        private readonly UiDialogStack _dialogs;

        /// <summary>房间信息（关卡详情弹窗需要访问）。</summary>
        public struct RoomInfo
        {
            public string Name;
            public string Tag;
            public bool Unlocked;
            public bool Completed;
            public int Stars;
            public string Detail;
        }

        public LevelSelectPage(UiShowcaseAssets assets, UiPageStack pages, UiDialogStack dialogs, RectTransform layer)
        {
            _assets = assets;
            _pages = pages;
            _dialogs = dialogs;
            Root = UiFactory.CreateRect("Page_LevelSelect", layer);
            UiFactory.Stretch(Root);
            Build();
            Root.gameObject.SetActive(false);
        }

        private void Build()
        {
            Image bg = UiFactory.CreatePanel("Bg", Root, UiTheme.Night, false);
            UiFactory.Stretch(bg.rectTransform);
            if (_assets.StoneTile != null)
            {
                bg.sprite = _assets.StoneTile;
                bg.color = new Color(1f, 1f, 1f, 0.16f);
            }

            Button back = UiFactory.CreateButton("Back", Root, "←  返回", 24, UiTheme.Stone, UiTheme.Paper, true);
            UiFactory.SetRect(back.GetComponent<RectTransform>(), 28f, 998f, 150f, 54f);
            back.onClick.AddListener(() => _pages.Pop());

            Text title = UiFactory.CreateText("Title", Root, "选择关卡", 40, UiTheme.Gold);
            UiFactory.SetRect(title.rectTransform, 200f, 1000f, 300f, 52f);
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;

            Text guide = UiFactory.CreateText("Guide", Root, "地牢共 3 层 · 通关当前层全部房间可解锁下一层", 22, UiTheme.Frost);
            UiFactory.SetRect(guide.rectTransform, 200f, 950f, 900f, 34f);

            // 楼层布局（纵向三层，每层一行房间）
            float sectionHeight = 240f;
            float startY = 890f;
            string[] floorNames = { "第 1 层 · 幽暗走廊", "第 2 层 · 腐化墓室", "第 3 层 · 深渊核心" };
            string[] floorDescs = { "难度 ★", "难度 ★★", "难度 ★★★" };

            RoomInfo[][] floors = new RoomInfo[3][];
            floors[0] = new[]
            {
                new RoomInfo { Name = "1-1 碎石大厅", Tag = "普通", Unlocked = true, Completed = true, Stars = 1, Detail = "基础教学房间，敌人较弱，掉落金币与药水。" },
                new RoomInfo { Name = "1-2 火把回廊", Tag = "精英", Unlocked = true, Completed = true, Stars = 2, Detail = "出现精英骷髅兵，注意躲避投掷火把。" },
                new RoomInfo { Name = "1-3 铁闸密室", Tag = "BOSS", Unlocked = true, Completed = false, Stars = 3, Detail = "本层最终 BOSS：铁甲骷髅王，掉落稀有装备。" },
            };
            floors[1] = new[]
            {
                new RoomInfo { Name = "2-1 腐化墓室", Tag = "普通", Unlocked = true, Completed = false, Stars = 2, Detail = "腐化气息弥漫，建议携带解毒药剂。" },
                new RoomInfo { Name = "2-2 白骨长廊", Tag = "精英", Unlocked = false, Completed = false, Stars = 2, Detail = "通关 2-1 后解锁。" },
                new RoomInfo { Name = "2-3 瘟疫祭坛", Tag = "BOSS", Unlocked = false, Completed = false, Stars = 3, Detail = "通关 2-2 后解锁。" },
            };
            floors[2] = new[]
            {
                new RoomInfo { Name = "3-1 深渊之门", Tag = "普通", Unlocked = false, Completed = false, Stars = 3, Detail = "通关第二层全部房间后解锁。" },
                new RoomInfo { Name = "3-2 混沌核心", Tag = "精英", Unlocked = false, Completed = false, Stars = 3, Detail = "通关 3-1 后解锁。" },
                new RoomInfo { Name = "3-3 深渊领主", Tag = "最终BOSS", Unlocked = false, Completed = false, Stars = 3, Detail = "深渊的终焉。通关 3-2 后解锁。" },
            };

            for (int f = 0; f < 3; f++)
            {
                BuildFloor(floors[f], floorNames[f], floorDescs[f], startY - f * sectionHeight, sectionHeight);
            }
        }

        private void BuildFloor(RoomInfo[] rooms, string floorName, string floorDesc, float y, float height)
        {
            RectTransform section = UiFactory.CreateRect("Floor_" + floorName.Substring(1, 1), Root);
            UiFactory.SetRect(section, 160f, y, 1600f, height);

            Text nameText = UiFactory.CreateText("FloorName", section, floorName + "    " + floorDesc, 26, UiTheme.Paper);
            UiFactory.SetRect(nameText.rectTransform, 0f, height - 44f, 1000f, 38f);
            nameText.fontStyle = FontStyle.Bold;

            float roomW = 480f, roomH = 150f, gap = 40f;
            for (int i = 0; i < rooms.Length; i++)
            {
                BuildRoom(section, rooms[i], i * (roomW + gap), 0f, roomW, roomH);
            }
        }

        private void BuildRoom(RectTransform parent, RoomInfo room, float x, float y, float w, float h)
        {
            Color bgColor = room.Unlocked
                ? (room.Completed ? new Color(0.30f, 0.42f, 0.30f, 0.95f) : UiTheme.StoneLight)
                : UiTheme.Locked;
            Button button = UiFactory.CreateButton("Room_" + room.Name, parent, string.Empty, 0, bgColor, UiTheme.Paper, false);
            UiFactory.SetRect(button.GetComponent<RectTransform>(), x, y, w, h);
            RectTransform rt = button.GetComponent<RectTransform>();

            Text nameText = UiFactory.CreateText("RoomName", rt, room.Name, 26, UiTheme.Paper);
            UiFactory.SetRect(nameText.rectTransform, 20f, h - 44f, w - 40f, 36f);
            nameText.fontStyle = FontStyle.Bold;
            if (!room.Unlocked)
            {
                nameText.color = UiTheme.PaperMuted;
            }

            Text tagText = UiFactory.CreateText("RoomTag", rt, room.Tag, 18,
                room.Tag == "BOSS" || room.Tag == "最终BOSS" ? UiTheme.Flame : UiTheme.Frost);
            UiFactory.SetRect(tagText.rectTransform, 20f, h - 78f, w - 40f, 28f);

            if (room.Completed)
            {
                Text done = UiFactory.CreateText("Done", rt, "✓ 已完成", 22, UiTheme.Success);
                UiFactory.SetRect(done.rectTransform, 20f, 18f, 160f, 30f);
            }
            else if (room.Unlocked)
            {
                Text stars = UiFactory.CreateText("Stars", rt, Stars(room.Stars), 20, UiTheme.Gold);
                UiFactory.SetRect(stars.rectTransform, w - 140f, 20f, 120f, 28f);
                stars.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                Text locked = UiFactory.CreateText("Locked", rt, "🔒 未解锁", 20, UiTheme.PaperMuted);
                UiFactory.SetRect(locked.rectTransform, w - 150f, 20f, 130f, 28f);
                locked.alignment = TextAnchor.MiddleCenter;
            }

            button.onClick.AddListener(() => OnRoomClicked(room));
        }

        private static string Stars(int count)
        {
            string result = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                result += i < count ? "★" : "☆";
            }
            return result;
        }

        /// <summary>供关卡详情弹窗复用：星级字符串。</summary>
        public static string StarsText(int count)
        {
            return Stars(count);
        }

        private void OnRoomClicked(RoomInfo room)
        {
            if (!room.Unlocked)
            {
                ToastDialog locked = new ToastDialog(_dialogs, _assets, _dialogs.Layer,
                    "房间未解锁", room.Detail + "\n\n通关上一层对应房间即可解锁。");
                _dialogs.Open(locked);
                return;
            }
            if (room.Completed)
            {
                ToastDialog done = new ToastDialog(_dialogs, _assets, _dialogs.Layer,
                    "已完成", "该房间已通关，可重复挑战刷取材料。");
                _dialogs.Open(done);
                return;
            }
            LevelDetailDialog detail = new LevelDetailDialog(_dialogs, _assets, _dialogs.Layer, room);
            _dialogs.Open(detail);
        }

        public void OnEnter() { }

        public void OnExit() { }
    }

    // ---------------------------------------------------------------- 活动弹窗

    public sealed class EventDialog : ModalDialog
    {
        private readonly Button _claimButton;
        private readonly Text _statusText;
        private bool _claimed;

        public EventDialog(UiDialogStack dialogStack, UiShowcaseAssets assets, RectTransform layer)
            : base(dialogStack, assets, layer, "Dialog_Event", 940f, 700f)
        {
            Text title = UiFactory.CreateText("Title", Content, "限时活动 · 深渊试炼", 36, UiTheme.Gold);
            UiFactory.SetRect(title.rectTransform, 0f, 0f, 700f, 50f);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -28f);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;

            Text subtitle = UiFactory.CreateText("Subtitle", Content, "活动时间：08-16 ~ 08-30 · 活动期间挑战任意楼层均有额外掉落", 20, UiTheme.PaperMuted);
            UiFactory.SetRect(subtitle.rectTransform, 0f, 0f, 840f, 30f);
            subtitle.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -88f);
            subtitle.alignment = TextAnchor.MiddleCenter;

            // 滚动奖励列表（通用性验证：内容超过可视高度时可滚动）
            ScrollRect scroll = UiFactory.CreateScrollRect("RewardScroll", Content, new Color(0f, 0f, 0f, 0f), out RectTransform rewardContent);
            RectTransform scrollRt = scroll.GetComponent<RectTransform>();
            UiFactory.SetRect(scrollRt, 60f, 250f, 820f, 300f);
            scrollRt.anchorMin = new Vector2(0.5f, 1f);
            scrollRt.anchorMax = new Vector2(0.5f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 1f);
            scrollRt.anchoredPosition = new Vector2(0f, -128f);

            string[] names = { "金币", "生命药水", "体力宝石", "白银短剑", "风之弓", "贤者法杖" };
            Sprite[] icons = { assets.GoldCoin, assets.HeartPickup, assets.StaminaGlobe, assets.Sword, assets.Bow, assets.Staff };
            string[] counts = { "x 1,200", "x 3", "x 60", "x 1", "x 1", "x 1" };

            float rowH = 78f, gap = 10f;
            rewardContent.sizeDelta = new Vector2(0f, names.Length * (rowH + gap) + gap);

            for (int i = 0; i < names.Length; i++)
            {
                Image row = UiFactory.CreatePanel("Reward_" + i, rewardContent, new Color(0.14f, 0.13f, 0.20f, 0.9f), false);
                row.rectTransform.anchorMin = new Vector2(0f, 1f);
                row.rectTransform.anchorMax = new Vector2(0f, 1f);
                row.rectTransform.pivot = new Vector2(0f, 1f);
                row.rectTransform.anchoredPosition = new Vector2(0f, -gap - i * (rowH + gap));
                row.rectTransform.sizeDelta = new Vector2(800f, rowH);

                if (icons[i] != null)
                {
                    Image icon = UiFactory.CreateSpritePanel("Icon", row.rectTransform, icons[i], Color.white, true);
                    UiFactory.SetRect(icon.rectTransform, 18f, 16f, 46f, 46f);
                }
                Text nameText = UiFactory.CreateText("Name", row.rectTransform, names[i], 24, UiTheme.Paper);
                UiFactory.SetRect(nameText.rectTransform, 84f, 24f, 280f, 32f);

                Text countText = UiFactory.CreateText("Count", row.rectTransform, counts[i], 22, UiTheme.Gold);
                UiFactory.SetRect(countText.rectTransform, 600f, 24f, 180f, 32f);
                countText.alignment = TextAnchor.MiddleRight;
            }

            // 底部操作区
            _statusText = UiFactory.CreateText("Status", Content, "完成任意楼层挑战即可领取", 20, UiTheme.PaperMuted);
            UiFactory.SetRect(_statusText.rectTransform, 0f, 0f, 820f, 30f);
            _statusText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _statusText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _statusText.rectTransform.anchoredPosition = new Vector2(0f, 116f);
            _statusText.alignment = TextAnchor.MiddleCenter;

            _claimButton = UiFactory.CreateButton("Claim", Content, "领取奖励", 26, UiTheme.GoldDeep, UiTheme.Paper, true);
            UiFactory.SetRect(_claimButton.GetComponent<RectTransform>(), 0f, 0f, 260f, 66f);
            _claimButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            _claimButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            _claimButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            _claimButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-140f, 32f);
            _claimButton.onClick.AddListener(() => Claim());

            Button join = UiFactory.CreateButton("Join", Content, "去参与", 26, UiTheme.Flame, Color.white, true);
            UiFactory.SetRect(join.GetComponent<RectTransform>(), 0f, 0f, 260f, 66f);
            join.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            join.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            join.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            join.GetComponent<RectTransform>().anchoredPosition = new Vector2(140f, 32f);
            join.onClick.AddListener(() =>
            {
                _statusText.text = "已前往关卡选择，选择任意楼层即可参与活动！";
                _statusText.color = UiTheme.Frost;
            });
        }

        private void Claim()
        {
            if (_claimed)
            {
                return;
            }
            _claimed = true;
            _statusText.text = "奖励已发放至背包！  ✓";
            _statusText.color = UiTheme.Success;
            Image bg = _claimButton.GetComponent<Image>();
            bg.color = new Color(0.30f, 0.30f, 0.34f, 1f);
        }

        public override void OnOpen()
        {
            _statusText.text = "完成任意楼层挑战即可领取";
            _statusText.color = UiTheme.PaperMuted;
        }
    }

    // ---------------------------------------------------------------- 关卡详情弹窗

    public sealed class LevelDetailDialog : ModalDialog
    {
        public LevelDetailDialog(UiDialogStack dialogStack, UiShowcaseAssets assets, RectTransform layer, LevelSelectPage.RoomInfo room)
            : base(dialogStack, assets, layer, "Dialog_LevelDetail", 800f, 560f)
        {
            Text title = UiFactory.CreateText("Title", Content, room.Name, 36, UiTheme.Gold);
            UiFactory.SetRect(title.rectTransform, 0f, 0f, 680f, 50f);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;

            Text tag = UiFactory.CreateText("Tag", Content, "房间类型：" + room.Tag + "    难度：" + LevelSelectPage.StarsText(room.Stars), 22, UiTheme.Frost);
            UiFactory.SetRect(tag.rectTransform, 0f, 0f, 680f, 32f);
            tag.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            tag.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tag.rectTransform.pivot = new Vector2(0.5f, 1f);
            tag.rectTransform.anchoredPosition = new Vector2(0f, -96f);
            tag.alignment = TextAnchor.MiddleCenter;

            Text detail = UiFactory.CreateText("Detail", Content, room.Detail, 24, UiTheme.Paper);
            UiFactory.SetRect(detail.rectTransform, 60f, 250f, 680f, 60f);
            detail.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            detail.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            detail.rectTransform.pivot = new Vector2(0.5f, 1f);
            detail.rectTransform.anchoredPosition = new Vector2(0f, -160f);
            detail.alignment = TextAnchor.MiddleCenter;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Overflow;

            Text rewardLabel = UiFactory.CreateText("RewardLabel", Content, "通关奖励", 24, UiTheme.Paper);
            UiFactory.SetRect(rewardLabel.rectTransform, 80f, 180f, 200f, 32f);

            float iconX = 80f;
            AddRewardIcon(assets.GoldCoin, iconX, "x 120"); iconX += 150f;
            AddRewardIcon(assets.HeartPickup, iconX, "x 2"); iconX += 150f;
            AddRewardIcon(assets.Sword, iconX, "概率掉落"); iconX += 150f;

            Text enemy = UiFactory.CreateText("Enemy", Content, "敌方：骷髅弓手 x3 · 腐化史莱姆 x2", 20, UiTheme.PaperMuted);
            UiFactory.SetRect(enemy.rectTransform, 80f, 120f, 600f, 28f);

            Button confirm = UiFactory.CreateButton("Confirm", Content, "确认进入", 26, UiTheme.GoldDeep, UiTheme.Paper, true);
            UiFactory.SetRect(confirm.GetComponent<RectTransform>(), 0f, 0f, 240f, 60f);
            confirm.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            confirm.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            confirm.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            confirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130f, 28f);
            confirm.onClick.AddListener(() =>
            {
                RequestClose();
                ToastDialog toast = new ToastDialog(dialogStack, assets, layer, "进入关卡", room.Name + " 已加入挑战队列（战斗系统为演示占位）。");
                dialogStack.Open(toast);
            });

            Button cancel = UiFactory.CreateButton("Cancel", Content, "再想想", 24, UiTheme.Stone, UiTheme.Paper, true);
            UiFactory.SetRect(cancel.GetComponent<RectTransform>(), 0f, 0f, 200f, 60f);
            cancel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            cancel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            cancel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            cancel.GetComponent<RectTransform>().anchoredPosition = new Vector2(150f, 28f);
            cancel.onClick.AddListener(() => RequestClose());
        }

        private void AddRewardIcon(Sprite icon, float x, string count)
        {
            if (icon == null)
            {
                return;
            }
            Image iconImage = UiFactory.CreateSpritePanel("RewardIcon", Content, icon, Color.white, true);
            UiFactory.SetRect(iconImage.rectTransform, x, 96f, 52f, 52f);
            Text countText = UiFactory.CreateText("RewardCount", Content, count, 18, UiTheme.Paper);
            UiFactory.SetRect(countText.rectTransform, x - 10f, 70f, 72f, 26f);
            countText.alignment = TextAnchor.MiddleCenter;
        }
    }
}