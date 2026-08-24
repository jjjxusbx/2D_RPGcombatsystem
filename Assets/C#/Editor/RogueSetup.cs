#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Roguelike.Run;
using Roguelike.Flow;
using Roguelike.UI;
using Roguelike.Reward;

/// <summary>
/// v0.1.5 局会话一键装配：RogueSystem（RunManager + 房间流 + 碎片掉落）与祭坛/结算 UGUI 面板。
/// 幂等设计：重复执行不会叠加组件/面板。UI 通过代码创建（不手写场景 YAML）。
/// </summary>
public static class RogueSetup
{
    private const string MenuRoot = "Tools/局会话/";

    [MenuItem(MenuRoot + "装配 Rogue Demo（房间流/掉落/祭坛/结算）")]
    public static void SetupRogueDemo()
    {
        GameObject system = GameObject.Find("RogueSystem");
        if (system == null) system = new GameObject("RogueSystem");

        RunManager rm = system.GetComponent<RunManager>();
        if (rm == null) rm = system.AddComponent<RunManager>();

        RogueRoomFlowController flow = system.GetComponent<RogueRoomFlowController>();
        if (flow == null) flow = system.AddComponent<RogueRoomFlowController>();

        FragmentSpawner spawner = system.GetComponent<FragmentSpawner>();
        if (spawner == null) spawner = system.AddComponent<FragmentSpawner>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        ChaState playerState = player != null ? player.GetComponentInChildren<ChaState>() : null;
        if (playerState == null)
        {
            Debug.LogWarning("[装配] 未找到带 Player 标签的 ChaState，请先装配玩家战斗管线。");
        }

        GameObject enemiesRootGo = GameObject.Find("Enemies");
        Transform enemiesTransform = enemiesRootGo != null ? enemiesRootGo.transform : system.transform;

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();

        SerializedObject soFlow = new SerializedObject(flow);
        soFlow.FindProperty("runManager").objectReferenceValue = rm;
        if (playerState != null) soFlow.FindProperty("playerState").objectReferenceValue = playerState;
        if (enemiesTransform != null) soFlow.FindProperty("enemiesRoot").objectReferenceValue = enemiesTransform;
        soFlow.FindProperty("autoStartOnEnable").boolValue = true;
        soFlow.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soSpawner = new SerializedObject(spawner);
        soSpawner.FindProperty("runManager").objectReferenceValue = rm;
        if (enemiesTransform != null) soSpawner.FindProperty("enemiesRoot").objectReferenceValue = enemiesTransform;
        soSpawner.ApplyModifiedPropertiesWithoutUndo();

        BuildAltarPanel(canvas.transform, rm, flow);
        BuildSettlementPanel(canvas.transform, rm, flow);

        EditorUtility.SetDirty(system);
        AssetDatabase.SaveAssets();
        Debug.Log("[装配] Rogue Demo 装配完成：RogueSystem + 房间流 + 碎片掉落 + 祭坛/结算面板。" +
                  "进入 Play Mode 查看 [Diagnostics] 日志。", system);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas;

        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void BuildAltarPanel(Transform canvas, RunManager rm, RogueRoomFlowController flow)
    {
        GameObject panel = CreatePanel(canvas, "AltarPanel", 720f, 260f);
        panel.SetActive(false);
        AltarChoiceUI altar = panel.AddComponent<AltarChoiceUI>();

        const float w = 200f;
        const float h = 120f;
        var buttons = new Button[3];
        var labels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 240f;
            buttons[i] = CreateButton(panel.transform, "Option" + (i + 1), new Vector2(x, 0f),
                new Vector2(w, h), "选项 " + (i + 1), out labels[i]);
        }

        SerializedObject so = new SerializedObject(altar);
        so.FindProperty("runManager").objectReferenceValue = rm;
        so.FindProperty("flow").objectReferenceValue = flow;
        SetObjectArray(so, "choiceButtons", buttons);
        SetObjectArray(so, "choiceLabels", labels);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildSettlementPanel(Transform canvas, RunManager rm, RogueRoomFlowController flow)
    {
        GameObject panel = CreatePanel(canvas, "SettlementPanel", 720f, 320f);
        panel.SetActive(false);
        SettlementUI settlement = panel.AddComponent<SettlementUI>();

        Text result = CreateText(panel.transform, "Result", "冒险失败", new Vector2(0f, 120f), new Vector2(600f, 40f), 32);
        Text fragment = CreateText(panel.transform, "Fragments", "本局碎片：0", new Vector2(0f, 60f), new Vector2(600f, 30f), 24);
        Text kill = CreateText(panel.transform, "Kills", "击杀数：0", new Vector2(0f, 20f), new Vector2(600f, 30f), 24);
        Text crystal = CreateText(panel.transform, "Crystals", "累计晶核：0", new Vector2(0f, -20f), new Vector2(600f, 30f), 24);

        Button restart = CreateButton(panel.transform, "Restart", new Vector2(-140f, -90f),
            new Vector2(220f, 60f), "再开一局", out _);
        Button camp = CreateButton(panel.transform, "Camp", new Vector2(140f, -90f),
            new Vector2(220f, 60f), "回营地", out _);

        SerializedObject so = new SerializedObject(settlement);
        so.FindProperty("runManager").objectReferenceValue = rm;
        so.FindProperty("flow").objectReferenceValue = flow;
        so.FindProperty("resultText").objectReferenceValue = result;
        so.FindProperty("fragmentText").objectReferenceValue = fragment;
        so.FindProperty("killText").objectReferenceValue = kill;
        so.FindProperty("crystalText").objectReferenceValue = crystal;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("campButton").objectReferenceValue = camp;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePanel(Transform parent, string name, float w, float h)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);

        Image img = panel.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        return panel;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size,
        string label, out Text labelText)
    {
        GameObject btn = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        RectTransform brt = btn.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = pos;
        brt.sizeDelta = size;

        Image img = btn.GetComponent<Image>();
        img.color = new Color(0.25f, 0.28f, 0.4f, 1f);

        Text text = CreateText(btn.transform, "Label", label, Vector2.zero, Vector2.zero, 22, true);
        labelText = text;
        return btn.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size,
        int fontSize, bool stretch = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();

        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = fontSize;
        Font font = null;
        try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (font == null)
        {
            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        text.font = font;
        return text;
    }

    private static void SetObjectArray(SerializedObject so, string field, Object[] items)
    {
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
#endif
