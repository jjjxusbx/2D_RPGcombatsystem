using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UiShowcase
{
    /// <summary>
    /// UiShowcase 组合根：显式装配页面栈、弹窗栈与全部界面，不依赖全局单例。
    /// Awake 用代码构建完整 UI 层级；Start 输出装配自检日志。
    ///
    /// 场景结构约定（见 Assets/Scenes/UiShowcase.unity）：
    ///   - Canvas（Screen Space - Overlay，CanvasScaler 1920x1080）+ GraphicRaycaster
    ///   - EventSystem（StandaloneInputModule，项目 activeInputHandler=Both 双模式可用）
    ///   - UiShowcaseBootstrap（本组件，持有素材引用）
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiShowcaseBootstrap : MonoBehaviour
    {
        [Header("字体（中文必配，否则回退 Arial 并告警）")]
        public Font chineseFont;
        public Font pixelFont;

        [Header("场景与装饰")]
        public Sprite dungeonBg;      // 可交互吊灯资源包/背景_0
        public Sprite stoneTile;      // 可交互吊灯资源包/石砖_0
        public Sprite chandelier;     // 可交互吊灯资源包/吊灯_0
        public Sprite building1;
        public Sprite building1Base;
        public Sprite building2;

        [Header("UI 框架图")]
        public Sprite frameWhite;
        public Sprite framePlain;
        public Sprite invBox;
        public Sprite uiBox;
        public Sprite arrow;

        [Header("图标")]
        public Sprite heroIdle;
        public Sprite goldCoin;
        public Sprite healthFill;
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Sprite staminaFull;
        public Sprite staminaEmpty;
        public Sprite staminaGlobe;
        public Sprite heartPickup;
        public Sprite sword;
        public Sprite bow;
        public Sprite staff;
        public Sprite bulletin;
        public Sprite sign;
        public Sprite tree;
        public Sprite bush;
        public Sprite torchBase;
        public Sprite ray;

        [Header("音频（可选）")]
        public AudioClip bgm;
        public bool musicEnabled = true;

        [Header("场景装配（由场景提供）")]
        public Canvas canvas;

        private UiPageStack _pages;
        private UiDialogStack _dialogs;
        private UiShowcaseAssets _assets;
        private LoadingPage _loading;
        private MainMenuPage _mainMenu;
        private bool _loadingDone;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }
            if (canvas == null)
            {
                UiLog.Error("未找到 Canvas，UI 装配中止。请在场景中放置 Canvas。");
                enabled = false;
                return;
            }

            _assets = BuildAssets();

            // 注入字体：中文缺失会回退 Arial 并告警
            UiFactory.DefaultFont = _assets.ChineseFont;
            UiFactory.PixelFont = _assets.PixelFont;
            if (UiFactory.DefaultFont == null)
            {
                UiLog.Warn("中文字体缺失（chineseFont 未配置），已回退内置 Arial，中文可能显示为方块。建议运行 Tools/UI作品集/装配并验证 自动配置 字魂布丁体.ttf。");
            }

            // 两层：页面层在下，弹窗层在上
            RectTransform pageLayer = UiFactory.CreateRect("PageLayer", canvas.transform);
            UiFactory.Stretch(pageLayer);

            RectTransform dialogLayer = UiFactory.CreateRect("DialogLayer", canvas.transform);
            UiFactory.Stretch(dialogLayer);
            _dialogs = new UiDialogStack { Layer = dialogLayer };

            // 页面栈：Loading 为根页
            _pages = new UiPageStack();
            _loading = new LoadingPage(_assets, pageLayer, OnLoadingDone);
            _mainMenu = new MainMenuPage(_assets, _pages, _dialogs, pageLayer);

            _pages.Push(_loading);

            // 主界面导航接线：主界面 -> 信息页 / 关卡选择 / 活动弹窗
            _mainMenu.BindNavigation(
                onLevels: () => OpenLevelSelect(),
                onInfo: () => OpenInfo(),
                onEvent: () => OpenEventDialog(),
                onBackpack: () => ShowToast("背包", "背包系统为演示占位，将在后续版本接入存档。"),
                onSettings: () => ShowToast("设置", "设置界面为演示占位：音量、画质、语言。"));

            UiLog.Info("UI 装配完成：页面栈 + 弹窗栈 + 6 界面已就绪。");
        }

        private void Start()
        {
            if (musicEnabled && bgm != null)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = bgm;
                source.loop = true;
                source.volume = 0.35f;
                source.Play();
            }
            else if (musicEnabled && bgm == null)
            {
                UiLog.Warn("背景音乐缺失（bgm 未配置），已跳过。可运行编辑器工具自动配置 BGM1.mp3。");
            }

            ReportAssembly();
        }

        private void Update()
        {
            if (!_loadingDone && _loading != null)
            {
                _loading.TickLoading();
            }

            // Esc：优先关弹窗，其次返回上一页
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_dialogs != null && _dialogs.Count > 0)
                {
                    _dialogs.CloseTop();
                }
                else if (_pages != null)
                {
                    _pages.Pop();
                }
            }
        }

        private void OnLoadingDone()
        {
            if (_loadingDone)
            {
                return;
            }
            _loadingDone = true;
            _pages.Replace(_mainMenu);
        }

        private void OpenLevelSelect()
        {
            LevelSelectPage levelSelect = new LevelSelectPage(_assets, _pages, _dialogs, GetPageLayer());
            _pages.Push(levelSelect);
        }

        private void OpenInfo()
        {
            InfoPage info = new InfoPage(_assets, _pages, _dialogs, GetPageLayer());
            _pages.Push(info);
        }

        private void OpenEventDialog()
        {
            EventDialog dialog = new EventDialog(_dialogs, _assets, _dialogs.Layer);
            _dialogs.Open(dialog);
        }

        private void ShowToast(string title, string message)
        {
            ToastDialog toast = new ToastDialog(_dialogs, _assets, _dialogs.Layer, title, message);
            _dialogs.Open(toast);
        }

        private RectTransform GetPageLayer()
        {
            RectTransform layer = _loading.Root.parent as RectTransform;
            return layer;
        }

        private UiShowcaseAssets BuildAssets()
        {
            return new UiShowcaseAssets
            {
                ChineseFont = chineseFont,
                PixelFont = pixelFont,
                DungeonBg = dungeonBg,
                StoneTile = stoneTile,
                Chandelier = chandelier,
                HeroIdle = heroIdle,
                FrameWhite = frameWhite,
                FramePlain = framePlain,
                GoldCoin = goldCoin,
                HealthFill = healthFill,
                HeartFull = heartFull,
                HeartEmpty = heartEmpty,
                StaminaFull = staminaFull,
                StaminaEmpty = staminaEmpty,
                StaminaGlobe = staminaGlobe,
                HeartPickup = heartPickup,
                InvBox = invBox,
                UiBox = uiBox,
                Sword = sword,
                Bow = bow,
                Staff = staff,
                Arrow = arrow,
                Building1 = building1,
                Building1Base = building1Base,
                Building1Roof = null,
                Building2 = building2,
                Bulletin = bulletin,
                Sign = sign,
                Tree = tree,
                Bush = bush,
                TorchBase = torchBase,
                Ray = ray,
                Bgm = bgm,
            };
        }

        private void ReportAssembly()
        {
            int missing = 0;
            if (chineseFont == null)
            {
                missing++;
                UiLog.Warn("图片/字体缺失检查：chineseFont 未配置。");
            }
            if (dungeonBg == null) { missing++; UiLog.Warn("[素材缺失] dungeonBg（背景_0）未配置，背景使用纯色。"); }
            if (stoneTile == null) { missing++; UiLog.Warn("[素材缺失] stoneTile（石砖_0）未配置，关卡页背景使用纯色。"); }
            if (chandelier == null) { missing++; UiLog.Warn("[素材缺失] chandelier（吊灯_0）未配置，Loading 装饰省略。"); }
            if (heroIdle == null) { missing++; UiLog.Warn("[素材缺失] heroIdle（Hero_Idle_0）未配置，头像/立绘省略。"); }
            if (frameWhite == null) { missing++; UiLog.Warn("[素材缺失] frameWhite 未配置，弹窗边框省略。"); }
            if (goldCoin == null) { missing++; UiLog.Warn("[素材缺失] goldCoin 未配置，金币图标省略。"); }
            if (healthFill == null) { missing++; UiLog.Warn("[素材缺失] healthFill 未配置，进度条填充使用纯色。"); }

            UiLog.Info("装配自检完成：关键素材缺失 " + missing + " 项，详细缺失项见上方 Warn 日志。");
        }
    }
}