#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UiShowcase
{
    /// <summary>
    /// UiShowcase 编辑器装配工具：确保演示场景存在并回填全部素材引用（幂等）。
    /// 场景 UiShowcase.unity 已自带引用，本工具用于：重建场景 / 引用修复 / 素材验证。
    /// </summary>
    public static class UiShowcaseSetup
    {
        private const string ScenePath = "Assets/Scenes/UiShowcase.unity";
        private const string MenuRoot = "Tools/UI作品集/";

        private const string ChineseFontPath = "Assets/Arts/2DRPGRes/Front/字魂布丁体.ttf";
        private const string PixelFontPath = "Assets/Arts/2DRPGRes/Sprites/UI/Gixel.ttf";
        private const string BgmPath = "Assets/Arts/2DRPGRes/sound/BGM1.mp3";

        [MenuItem(MenuRoot + "装配并验证 UiShowcase 场景")]
        public static void ComposeUiShowcase()
        {
            if (!File.Exists(ScenePath))
            {
                CreateSceneFromScratch();
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            UiShowcaseBootstrap bootstrap = Object.FindFirstObjectByType<UiShowcaseBootstrap>();
            if (bootstrap == null)
            {
                GameObject go = new GameObject("UiShowcaseBootstrap");
                bootstrap = go.AddComponent<UiShowcaseBootstrap>();
            }

            AssignReferences(bootstrap);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("[UiShowcase] 装配完成：" + ScenePath + " 素材引用已回填。");
        }

        [MenuItem(MenuRoot + "仅验证素材引用")]
        public static void ValidateReferences()
        {
            if (Object.FindFirstObjectByType<UiShowcaseBootstrap>() == null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            UiShowcaseBootstrap bootstrap = Object.FindFirstObjectByType<UiShowcaseBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[UiShowcase] 未找到 UiShowcaseBootstrap，先执行「装配并验证」。");
                return;
            }
            int missing = 0;
            if (bootstrap.chineseFont == null) { missing++; Debug.LogWarning("[UiShowcase] 缺失：chineseFont"); }
            if (bootstrap.pixelFont == null) { missing++; Debug.LogWarning("[UiShowcase] 缺失：pixelFont"); }
            if (bootstrap.dungeonBg == null) { missing++; Debug.LogWarning("[UiShowcase] 缺失：dungeonBg"); }
            if (bootstrap.heroIdle == null) { missing++; Debug.LogWarning("[UiShowcase] 缺失：heroIdle"); }
            if (bootstrap.goldCoin == null) { missing++; Debug.LogWarning("[UiShowcase] 缺失：goldCoin"); }
            if (bootstrap.bgm == null) { missing++; Debug.LogWarning("[UiShowcase] 缺失：bgm"); }
            Debug.Log(missing == 0
                ? "[UiShowcase] 素材引用检查通过，全部就绪。"
                : "[UiShowcase] 素材引用缺失 " + missing + " 项，可执行「装配并验证」修复。");
        }

        private static void CreateSceneFromScratch()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Canvas + Scaler + Raycaster
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem + StandaloneInputModule（项目 activeInputHandler = Both，双模式可用）
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Bootstrap
            GameObject bootstrapGo = new GameObject("UiShowcaseBootstrap");
            UiShowcaseBootstrap bootstrap = bootstrapGo.AddComponent<UiShowcaseBootstrap>();

            SerializedObject so = new SerializedObject(bootstrap);
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        }

        private static void AssignReferences(UiShowcaseBootstrap bootstrap)
        {
            SerializedObject so = new SerializedObject(bootstrap);

            SetFont(so, "chineseFont", ChineseFontPath);
            SetFont(so, "pixelFont", PixelFontPath);

            SetSprite(so, "dungeonBg", "Assets/Arts/2DRPGRes/可交互吊灯资源包/背景.png", "背景_0");
            SetSprite(so, "stoneTile", "Assets/Arts/2DRPGRes/可交互吊灯资源包/石砖.png", "石砖_0");
            SetSprite(so, "chandelier", "Assets/Arts/2DRPGRes/可交互吊灯资源包/吊灯.png", "吊灯_0");
            SetSprite(so, "heroIdle", "Assets/Arts/2DRPGRes/Sprites/Player/Hero_Idle.png", "Hero_Idle_0");

            SetSprite(so, "frameWhite", "Assets/Arts/2DRPGRes/Sprites/UI/Border Empty White Outline.png");
            SetSprite(so, "framePlain", "Assets/Arts/2DRPGRes/Sprites/UI/Border Empty.png");
            SetSprite(so, "invBox", "Assets/Arts/2DRPGRes/Sprites/Inventory UI/InventoryBoxWhiteOutline.png");
            SetSprite(so, "uiBox", "Assets/Arts/2DRPGRes/Sprites/Inventory UI/UI_Box.png");
            SetSprite(so, "arrow", "Assets/Arts/2DRPGRes/Sprites/Inventory UI/Arrow.png");

            SetSprite(so, "goldCoin", "Assets/Arts/2DRPGRes/Sprites/UI/GoldCoin_WithOutline.png");
            SetSprite(so, "healthFill", "Assets/Arts/2DRPGRes/Sprites/UI/Health_Fill.png");
            SetSprite(so, "heartFull", "Assets/Arts/2DRPGRes/Sprites/UI/Heart Full.png");
            SetSprite(so, "heartEmpty", "Assets/Arts/2DRPGRes/Sprites/UI/Heart Empty.png");
            SetSprite(so, "staminaFull", "Assets/Arts/2DRPGRes/Sprites/UI/Stamina Full.png");
            SetSprite(so, "staminaEmpty", "Assets/Arts/2DRPGRes/Sprites/UI/Stamina Empty.png");
            SetSprite(so, "staminaGlobe", "Assets/Arts/2DRPGRes/Sprites/Misc/Stamina Globe.png");
            SetSprite(so, "heartPickup", "Assets/Arts/2DRPGRes/Sprites/Misc/Heart Pickup.png");
            SetSprite(so, "sword", "Assets/Arts/2DRPGRes/Sprites/Inventory UI/Inventory_Sword.png");
            SetSprite(so, "bow", "Assets/Arts/2DRPGRes/Sprites/Inventory UI/Inventory_Bow.png");
            SetSprite(so, "staff", "Assets/Arts/2DRPGRes/Sprites/Inventory UI/Inventory_Staff.png");
            SetSprite(so, "bulletin", "Assets/Arts/2DRPGRes/Sprites/Buildings/Bulletin Board.png");
            SetSprite(so, "sign", "Assets/Arts/2DRPGRes/Sprites/Buildings/Sign.png");
            SetSprite(so, "tree", "Assets/Arts/2DRPGRes/Sprites/Environment/Tree.png");
            SetSprite(so, "bush", "Assets/Arts/2DRPGRes/Sprites/Environment/Bush.png");
            SetSprite(so, "torchBase", "Assets/Arts/2DRPGRes/Sprites/Environment/TorchBase.png");
            SetSprite(so, "ray", "Assets/Arts/2DRPGRes/Sprites/Environment/Ray.png");
            SetSprite(so, "building1", "Assets/Arts/2DRPGRes/Sprites/Buildings/Building_1.png");
            SetSprite(so, "building1Base", "Assets/Arts/2DRPGRes/Sprites/Buildings/Building_1_Base.png");
            SetSprite(so, "building2", "Assets/Arts/2DRPGRes/Sprites/Buildings/Building_2.png");

            AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
            if (bgm != null)
            {
                so.FindProperty("bgm").objectReferenceValue = bgm;
            }
            else
            {
                Debug.LogWarning("[UiShowcase] 背景音乐缺失：" + BgmPath);
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                so.FindProperty("canvas").objectReferenceValue = canvas;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
        }

        private static void SetFont(SerializedObject so, string field, string path)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font != null)
            {
                so.FindProperty(field).objectReferenceValue = font;
            }
            else
            {
                Debug.LogWarning("[UiShowcase] 字体缺失：" + path);
            }
        }

        private static void SetSprite(SerializedObject so, string field, string texturePath, string subName = null)
        {
            Sprite sprite = null;
            if (string.IsNullOrEmpty(subName))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            }
            else
            {
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(texturePath);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite s && s.name == subName)
                    {
                        sprite = s;
                        break;
                    }
                }
            }
            if (sprite != null)
            {
                so.FindProperty(field).objectReferenceValue = sprite;
            }
            else
            {
                Debug.LogWarning("[UiShowcase] 图片缺失：" + texturePath + (subName == null ? "" : " / " + subName));
            }
        }
    }
}
#endif