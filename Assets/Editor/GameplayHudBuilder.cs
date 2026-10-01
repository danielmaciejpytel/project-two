using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Rebuilds the gameplay HUD of MainScene to the approved mockup (Assets/Sprites/GameplayHud). The existing objects keep their names and
// components (the game code and the tests use them): they are restyled, moved to the layout of the mockup and extended with the new elements.
// Safe to run again: objects that exist are reused. Menu: Tools > Gameplay HUD > Build.
public static class GameplayHudBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string SpriteRoot = "Assets/Sprites/GameplayHud/";

    private static readonly Color Hot = new Color32(0xFF, 0x1B, 0x47, 0xFF);
    private static readonly Color Cold = new Color32(0x14, 0xC8, 0xD8, 0xFF);
    private static readonly Color Accent = new Color32(0xD7, 0x2E, 0x66, 0xFF);
    private static readonly Color Muted = new Color32(0xA3, 0xA3, 0xA3, 0xFF);
    private static readonly Color Ink = new Color32(0x11, 0x11, 0x11, 0xFF);

    private static TMP_FontAsset _font;
    private static readonly List<string> Log = new List<string>();

    [MenuItem("Tools/Gameplay HUD/Build")]
    public static string Build()
    {
        Log.Clear();
        BuildPrefabs();
        BuildScene();
        AssetDatabase.SaveAssets();
        return string.Join("\n", Log);
    }

    private static Sprite Spr(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + path + ".png");
        if (sprite == null) Log.Add("MISSING SPRITE " + path);
        return sprite;
    }

    // ---------------------------------------------------------------- small builders

    private static GameObject Child(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform Rt(GameObject go) => (RectTransform)go.transform;

    // The component, added when the object has none (GetComponent can give a fake null in the editor, so no ?? here).
    private static T Get<T>(GameObject go) where T : Component => go.TryGetComponent(out T component) ? component : go.AddComponent<T>();

    // A descendant by name, wherever it is under the root (objects that were moved by an earlier run are found too).
    private static GameObject FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child.gameObject;
            GameObject found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Image SetImage(GameObject go, Sprite sprite, Color color, bool raycast = false, bool preserveAspect = false)
    {
        Image image = Get<Image>(go);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = color;
        image.raycastTarget = raycast;
        image.enabled = true;
        return image;
    }

    private static TMP_Text SetText(GameObject go, string text, float size, Color color, TextAlignmentOptions alignment, bool upper = true, bool wrap = false)
    {
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = upper ? FontStyles.UpperCase : FontStyles.Normal;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        // The text of the game font sits a few pixels lower in its line than in the mockup: a bottom margin lifts centred text.
        float lift = size >= 48.0f ? 5.0f : size >= 32.0f ? 4.0f : 2.0f;
        bool centred = tmp.verticalAlignment != VerticalAlignmentOptions.Top && tmp.verticalAlignment != VerticalAlignmentOptions.Bottom;
        tmp.margin = centred ? new Vector4(0.0f, 0.0f, 0.0f, 2.0f * lift) : Vector4.zero;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Line height in pixels for a text of this size (TextMeshPro takes it as a percentage change of the natural one).
    private static void LineHeight(TMP_Text text, float pixels)
    {
        float natural = text.font.faceInfo.lineHeight * text.fontSize / text.font.faceInfo.pointSize;
        text.lineSpacing = (pixels - natural) / (0.01f * text.fontSize);
    }

    private static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Log.Add("NO FIELD " + target.GetType().Name + "." + field); return; }
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Log.Add("NO FIELD " + target.GetType().Name + "." + field); return; }
        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string field, Color value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Log.Add("NO FIELD " + target.GetType().Name + "." + field); return; }
        property.colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The key of a LocalizedText: the English text that is translated to the language of the game.
    private static void SetKey(GameObject go, string key)
    {
        SerializedObject so = new SerializedObject(Get<LocalizedText>(go));
        so.FindProperty("_key").stringValue = key;
        so.FindProperty("_fitButtonWidth").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HorizontalLayoutGroup Row(GameObject go, float spacing)
    {
        HorizontalLayoutGroup group = Get<HorizontalLayoutGroup>(go);
        group.spacing = spacing;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        group.padding = new RectOffset();
        return group;
    }

    private static GameObject Find(string path)
    {
        string[] parts = path.Split('/');
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != parts[0]) continue;
            Transform current = root.transform;
            for (int i = 1; i < parts.Length && current != null; i++) current = current.Find(parts[i]);
            if (current != null) return current.gameObject;
        }
        Log.Add("NOT FOUND " + path);
        return null;
    }

    // ---------------------------------------------------------------- prefabs

    private static void BuildPrefabs()
    {
        foreach (string path in new[] { "Assets/Prefabs/CommanderHealthPointRed.prefab", "Assets/Prefabs/CommanderHealthPointBlue.prefab" })
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Image image = root.GetComponent<Image>();
            image.sprite = Spr("Bars/PipCommanderOn");
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            RectTransform rect = Rt(root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(14.0f, 14.0f);
            rect.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        BuildUnitButtonPrefab();
        BuildTilePrefab();
    }

    // The deployment zone (where a called unit can stand) is gray and pulses (TileController), it was yellow.
    private static void BuildTilePrefab()
    {
        const string path = "Assets/Prefabs/TilePrefab.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        SetColor(root.GetComponent<TileController>(), "_deploymentZoneColor", new Color(0.62f, 0.62f, 0.62f, 0.95f));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Log.Add("tile prefab ok");
    }

    // The card of the team panel: a transparent button over the frame (with the card, the killed mark and the badge), the name and the health points.
    private static void BuildUnitButtonPrefab()
    {
        const string path = "Assets/Prefabs/UnitButton.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        _font = root.transform.Find("UnitNameText").GetComponent<TMP_Text>().font;
        RectTransform rootRect = Rt(root);
        HudLayout.PlaceIn(rootRect, 0, 0, 90, 150);
        rootRect.localScale = Vector3.one;
        SetImage(root, null, Color.clear, true);

        GameObject frame = Child(root.transform, "Frame");
        HudLayout.PlaceIn(Rt(frame), 0, 0, 90, 114);
        Image frameImage = SetImage(frame, Spr("Slots/SlotFrame"), Color.white);
        GameObject card = Child(frame.transform, "Card");
        HudLayout.PlaceIn(Rt(card), 8, 5, 74, 104);
        Image cardImage = SetImage(card, null, Color.white, false, true);

        GameObject killed = root.transform.Find("KilledImage") != null ? root.transform.Find("KilledImage").gameObject : root.transform.Find("Frame/KilledImage").gameObject;
        killed.transform.SetParent(frame.transform, false);
        Stretch(Rt(killed));
        // The cross over the card of a killed unit.
        Image killedImage = SetImage(killed, Spr("Slots/SlotCrossOut"), Color.white);
        killed.transform.SetAsLastSibling();

        GameObject badge = Child(frame.transform, "Badge");
        HudLayout.PlaceIn(Rt(badge), 40, -2, 52, 22);
        Image badgeImage = SetImage(badge, Spr("Elements/BadgeAccent"), Color.white);
        GameObject badgeLabel = Child(badge.transform, "Label");
        Stretch(Rt(badgeLabel));
        TMP_Text badgeText = SetText(badgeLabel, "NEW", 16, Color.white, TextAlignmentOptions.Center);
        badge.SetActive(false);

        GameObject name = root.transform.Find("UnitNameText").gameObject;
        HudLayout.PlaceIn(Rt(name), -5, 120, 100, 18);
        TMP_Text nameText = SetText(name, "", 16, Color.white, TextAlignmentOptions.Center);

        GameObject pips = Child(root.transform, "HealthPoints");
        HudLayout.PlaceIn(Rt(pips), 0, 143, 90, 5);

        ButtonUnitController controller = root.GetComponent<ButtonUnitController>();
        SetField(controller, "_frame", Rt(frame));
        SetField(controller, "_frameImage", frameImage);
        SetField(controller, "_cardImage", cardImage);
        SetField(controller, "_killedImage", killedImage);
        SetField(controller, "_unitText", nameText);
        SetField(controller, "_badgeImage", badgeImage);
        SetField(controller, "_badgeText", badgeText);
        SetField(controller, "_pipRow", Rt(pips));
        SetField(controller, "_frameSprite", Spr("Slots/SlotFrame"));
        SetField(controller, "_reserveSprite", Spr("Slots/SlotFrameReserve"));
        SetField(controller, "_selectedRedSprite", Spr("Slots/SlotFrameSelectedRed"));
        SetField(controller, "_selectedBlueSprite", Spr("Slots/SlotFrameSelectedBlue"));
        SetField(controller, "_badgeNewSprite", Spr("Elements/BadgeAccent"));
        SetField(controller, "_badgeMovedSprite", Spr("Elements/BadgeMuted"));
        SetField(controller, "_pipRedSprite", Spr("Slots/PipSmallRed"));
        SetField(controller, "_pipBlueSprite", Spr("Slots/PipSmallBlue"));
        SetField(controller, "_pipLostSprite", Spr("Slots/PipSmallOff"));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Log.Add("prefabs ok");
    }

    // ---------------------------------------------------------------- scene

    private static void BuildScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject redText = Find("Canvas/GameplayPanel/Layout/CornerTopLeft/RedBarImage/RedCommanderText");
        _font = redText.GetComponent<TMP_Text>().font;

        ZeroPanelOffset();
        CommanderBars();
        Banner();
        Hint();
        Buttons();
        BattleLog();
        InfoPanel();
        TeamPanel();
        DamageCallout();
        EndGame();
        SettingsPanels();
        WireUi();

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log.Add("scene saved");
        BuildMenuScene();
    }

    // The instructions in the menu: the text on the left, aligned to the left, in a scrolling view with a white scrollbar on its right.
    private static void BuildMenuScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity", OpenSceneMode.Single);
        GameObject menuPanel = Find("Canvas/MainMenuPanel");
        GameObject text = FindDeep(menuPanel.transform, "Instrukcja") ?? FindDeep(menuPanel.transform, "InstructionText");
        TMP_Text instruction = text.GetComponent<TMP_Text>();
        _font = instruction.font;

        GameObject panel = Child(menuPanel.transform, "InstructionPanel");
        HudLayout.Place(Rt(panel), 30, 320, 860, 480);
        SetImage(panel, null, Color.clear, true);
        GameObject viewport = Child(panel.transform, "Viewport");
        Stretch(Rt(viewport), 30, 0, 28, 0);
        Get<RectMask2D>(viewport);

        text.name = "InstructionText";
        text.SetActive(true);
        text.transform.SetParent(viewport.transform, false);
        RectTransform textRect = Rt(text);
        textRect.anchorMin = new Vector2(0.0f, 1.0f);
        textRect.anchorMax = new Vector2(1.0f, 1.0f);
        textRect.pivot = new Vector2(0.5f, 1.0f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(0.0f, 0.0f);
        instruction.alignment = TextAlignmentOptions.TopLeft;
        instruction.textWrappingMode = TextWrappingModes.Normal;
        instruction.enableAutoSizing = false;
        instruction.fontSize = 24.0f;
        instruction.lineSpacing = 8.0f;
        instruction.raycastTarget = false;
        ContentSizeFitter fitter = Get<ContentSizeFitter>(text);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetKey(text, ReadInstructionKey());

        // The scrollbar: a thin track in the middle of its rect and a handle across it, like the sliders of the settings but white.
        GameObject bar = Child(panel.transform, "Scrollbar");
        RectTransform barRect = Rt(bar);
        barRect.anchorMin = new Vector2(1.0f, 0.0f);
        barRect.anchorMax = new Vector2(1.0f, 1.0f);
        barRect.pivot = new Vector2(1.0f, 0.5f);
        barRect.sizeDelta = new Vector2(16.0f, 0.0f);
        barRect.anchoredPosition = Vector2.zero;
        SetImage(bar, null, Color.clear, true);
        GameObject track = Child(bar.transform, "Track");
        RectTransform trackRect = Rt(track);
        trackRect.anchorMin = new Vector2(0.5f, 0.0f);
        trackRect.anchorMax = new Vector2(0.5f, 1.0f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(8.0f, 0.0f);
        trackRect.anchoredPosition = Vector2.zero;
        SetImage(track, Spr("Menu/ScrollbarTrack"), Color.white);
        GameObject slide = Child(bar.transform, "Sliding Area");
        Stretch(Rt(slide));
        GameObject handle = Child(slide.transform, "Handle");
        Stretch(Rt(handle));
        Image handleImage = SetImage(handle, Spr("Menu/ScrollbarHandle"), Color.white, true);
        Scrollbar scrollbar = Get<Scrollbar>(bar);
        SerializedObject barSo = new SerializedObject(scrollbar);
        barSo.FindProperty("m_HandleRect").objectReferenceValue = Rt(handle);
        barSo.FindProperty("m_TargetGraphic").objectReferenceValue = handleImage;
        barSo.FindProperty("m_Direction").enumValueIndex = (int)Scrollbar.Direction.BottomToTop;
        barSo.ApplyModifiedPropertiesWithoutUndo();

        ScrollRect scroll = Get<ScrollRect>(panel);
        SerializedObject so = new SerializedObject(scroll);
        so.FindProperty("m_Content").objectReferenceValue = textRect;
        so.FindProperty("m_Viewport").objectReferenceValue = Rt(viewport);
        so.FindProperty("m_Horizontal").boolValue = false;
        so.FindProperty("m_Vertical").boolValue = true;
        so.FindProperty("m_MovementType").enumValueIndex = (int)ScrollRect.MovementType.Clamped;
        so.FindProperty("m_ScrollSensitivity").floatValue = 40.0f;
        so.FindProperty("m_VerticalScrollbar").objectReferenceValue = scrollbar;
        so.FindProperty("m_VerticalScrollbarVisibility").enumValueIndex = (int)ScrollRect.ScrollbarVisibility.Permanent;
        so.ApplyModifiedPropertiesWithoutUndo();

        MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        SetField(menu, "_instructionPanel", panel);
        panel.SetActive(false);

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log.Add("menu instructions ok");
    }

    // The English text of the instructions is the key of its translation (the first entry of Polish.txt with the heading "GOAL").
    private static string ReadInstructionKey()
    {
        foreach (string line in System.IO.File.ReadAllLines("Assets/Resources/Localization/Polish.txt", System.Text.Encoding.UTF8))
        {
            if (!line.StartsWith("<color=#D72E66>GOAL")) continue;
            return line.Substring(0, line.IndexOf('\t')).Replace("\\n", "\n");
        }
        Log.Add("NO INSTRUCTION KEY");
        return "";
    }

    // The root of the HUD was offset by the old layout; the mockup coordinates are counted from the middle of the screen, so the offset
    // goes away and the backgrounds, which stand in the same group, take it over to stay where they were.
    private static void ZeroPanelOffset()
    {
        GameObject panel = Find("Canvas/GameplayPanel");
        Vector3 offset = panel.transform.localPosition;
        if (offset == Vector3.zero) return;
        Transform layout = panel.transform.Find("Layout");
        foreach (Transform child in layout)
        {
            if (child.name.StartsWith("Background")) child.localPosition += offset;
        }
        panel.transform.localPosition = Vector3.zero;
        Log.Add("panel offset " + offset + " removed");
    }

    // The commander bars are built from pieces: the body and five stripes, one for every living member of the team (CommanderBarController
    // hides the farthest stripe when someone dies); the team name is translated and takes a smaller size when it is too long.
    private static void CommanderBars()
    {
        const string left = "Canvas/GameplayPanel/Layout/CornerTopLeft/";
        const string right = "Canvas/GameplayPanel/Layout/CornerTopRight/";
        int[] stripeTops = { 417, 448, 477, 508, 539 };
        for (int side = 0; side < 2; side++)
        {
            bool red = side == 0;
            GameObject bar = Find((red ? left + "RedBarImage" : right + "BlueBarImage"));
            HudLayout.Place(Rt(bar), red ? 24 : 1276, 24, 620, 96);
            SetImage(bar, null, Color.clear);
            GameObject body = Child(bar.transform, "Body");
            HudLayout.PlaceIn(Rt(body), red ? 0 : 222, 0, 398, 96);
            SetImage(body, Spr(red ? "Bars/BarBodyRed" : "Bars/BarBodyBlue"), Color.white);
            GameObject stripes = Child(bar.transform, "Stripes");
            Stretch(Rt(stripes));
            GameObject[] pieces = new GameObject[stripeTops.Length];
            for (int i = 0; i < stripeTops.Length; i++)
            {
                GameObject stripe = Child(stripes.transform, "Stripe" + (i + 1));
                // The stripes lean like the body; the first is the nearest to the name.
                HudLayout.PlaceIn(Rt(stripe), red ? stripeTops[i] - 45 : 608 - stripeTops[i], 0, 57, 96);
                SetImage(stripe, Spr(red ? "Bars/BarStripeRed" : "Bars/BarStripeBlue"), Color.white);
                pieces[i] = stripe;
            }
            body.transform.SetAsFirstSibling();
            stripes.transform.SetSiblingIndex(1);

            GameObject text = bar.transform.Find(red ? "RedCommanderText" : "BlueCommanderText").gameObject;
            // The name stays inside the body of the bar (about 350 px wide at its height).
            HudLayout.PlaceIn(Rt(text), red ? 20 : 250, 10, 350, 48);
            SetText(text, red ? "Team Red" : "Team Blue", 48, Color.white, red ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight, true);
            SetKey(text, red ? "Team Red" : "Team Blue");
            SetFloat(Get<FitTextSize>(text), "_maxSize", 48.0f);

            // The health points start at the left edge of the red bar and at the right edge of the blue one, 68 px from the top.
            Transform point = bar.transform.Find(red ? "healthPointPoint" : "healthPointPoint2");
            RectTransform pointRect = (RectTransform)point;
            pointRect.anchorMin = pointRect.anchorMax = pointRect.pivot = new Vector2(0.5f, 0.5f);
            pointRect.anchoredPosition = new Vector2(red ? 20 + 7 - 310 : 620 - 20 - 7 - 310, 48 - (68 + 7));
            CommanderBarController controller = bar.GetComponent<CommanderBarController>();
            SetField(controller, "_fullSprite", Spr("Bars/PipCommanderOn"));
            SetField(controller, "_lostSprite", Spr("Bars/PipCommanderOff"));
            SetFloat(controller, "_step", 20.0f);
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty list = so.FindProperty("_stripes");
            list.arraySize = pieces.Length;
            for (int i = 0; i < pieces.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Log.Add("bars ok");
    }

    // The turn banner: turn number and the player on the left, the timer on the right, the time bar under them, and the computer's "Thinking...".
    private static void Banner()
    {
        const string top = "Canvas/GameplayPanel/Layout/EdgeTop/";
        GameObject banner = Find(top + "WinnerBackgroundImage");
        HudLayout.Place(Rt(banner), 690, 24, 540, 112);
        SetImage(banner, Spr("Panels/PanelBanner"), Color.white);

        GameObject name = banner.transform.Find("EndGameText").gameObject;
        HudLayout.PlaceIn(Rt(name), 26, 34, 300, 48);
        SetText(name, "Team Blue", 48, Color.white, TextAlignmentOptions.MidlineLeft);

        GameObject turn = Child(banner.transform, "TurnLabel");
        HudLayout.PlaceIn(Rt(turn), 26, 12, 300, 16);
        SetText(turn, "TURN 1", 16, Muted, TextAlignmentOptions.MidlineLeft);

        const string corner = "Canvas/GameplayPanel/Layout/CornerTopRight/";
        GameObject timer = FindDeep(banner.transform, "TimerBackgroundImage") ?? Find(corner + "TimerBackgroundImage");
        timer.transform.SetParent(banner.transform, false);
        HudLayout.PlaceIn(Rt(timer), 270, 12, 244, 80);
        SetImage(timer, null, Color.clear);
        GameObject caption = timer.transform.Find("TimerDescriptionText").gameObject;
        HudLayout.PlaceIn(Rt(caption), 0, 0, 244, 16);
        SetText(caption, "TURN ENDS IN", 16, Muted, TextAlignmentOptions.MidlineRight);
        SetKey(caption, "TURN ENDS IN");
        GameObject seconds = timer.transform.Find("TimerText").gameObject;
        HudLayout.PlaceIn(Rt(seconds), 0, 22, 244, 48);
        SetText(seconds, "60", 48, Color.white, TextAlignmentOptions.MidlineRight, false);

        GameObject bar = Child(banner.transform, "TimerBar");
        HudLayout.PlaceIn(Rt(bar), 26, 92, 488, 8);
        GameObject track = Child(bar.transform, "Track");
        Stretch(Rt(track));
        SetImage(track, Spr("Bars/TimerBarTrack"), Color.white);
        GameObject fill = Child(bar.transform, "Fill");
        Stretch(Rt(fill));
        Image fillImage = SetImage(fill, Spr("Bars/TimerBarFill"), Hot);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1.0f;

        GameObject computer = Child(banner.transform, "ComputerBlock");
        HudLayout.PlaceIn(Rt(computer), 270, 12, 244, 80);
        GameObject computerCaption = Child(computer.transform, "ComputerCaption");
        HudLayout.PlaceIn(Rt(computerCaption), 0, 0, 244, 16);
        SetText(computerCaption, "COMPUTER", 16, Muted, TextAlignmentOptions.MidlineRight);
        SetKey(computerCaption, "COMPUTER");
        GameObject thinking = Child(computer.transform, "ThinkingText");
        HudLayout.PlaceIn(Rt(thinking), 0, 26, 244, 32);
        SetText(thinking, "THINKING...", 32, Muted, TextAlignmentOptions.MidlineRight);
        computer.SetActive(false);

        UIController ui = Object.FindFirstObjectByType<UIController>(FindObjectsInactive.Include);
        SetField(ui, "_turnLabel", turn.GetComponent<TMP_Text>());
        SetField(ui, "_timerBar", bar);
        SetField(ui, "_timerFill", fillImage);
        SetField(ui, "_computerBlock", computer);
        SetField(ui, "_thinkingText", thinking.GetComponent<TMP_Text>());
        SetField(ui, "_timerImage", timer.GetComponent<Image>());
        Log.Add("banner ok");
    }

    private static void Hint()
    {
        GameObject hint = Find("Canvas/GameplayPanel/Layout/EdgeTop/HintPanel");
        HudLayout.Place(Rt(hint), 560, 148, 800, 72);
        SetImage(hint, Spr("Panels/PanelHint"), Color.white);

        GameObject badge = Child(hint.transform, "HintBadge");
        HudLayout.PlaceIn(Rt(badge), 22, 23, 104, 26);
        SetImage(badge, Spr("Elements/BadgeAccent"), Color.white);
        GameObject badgeText = Child(badge.transform, "Label");
        Stretch(Rt(badgeText));
        SetText(badgeText, "HINT", 16, Color.white, TextAlignmentOptions.Center);
        SetKey(badgeText, "HINT");

        GameObject text = hint.transform.Find("Text").gameObject;
        HudLayout.PlaceIn(Rt(text), 142, 12, 564, 48);
        TMP_Text hintText = SetText(text, "", 16, Color.white, TextAlignmentOptions.MidlineLeft, false, true);
        LineHeight(hintText, 24.0f);

        GameObject key = Child(hint.transform, "EscKey");
        HudLayout.PlaceIn(Rt(key), 729, 14, 42, 24);
        SetImage(key, Spr("Elements/Keycap"), Color.white);
        GameObject keyText = Child(key.transform, "Label");
        Stretch(Rt(keyText));
        SetText(keyText, "ESC", 16, Ink, TextAlignmentOptions.Center);
        GameObject caption = Child(hint.transform, "EscCaption");
        HudLayout.PlaceIn(Rt(caption), 722, 42, 56, 16);
        SetText(caption, "PAUSE", 16, Muted, TextAlignmentOptions.Center);
        SetKey(caption, "Pause");
        Log.Add("hint ok");
    }

    // The main button, Call and the ability button; their places are set by UIController for the player who has the turn.
    private static void Buttons()
    {
        const string corner = "Canvas/GameplayPanel/Layout/CornerTopRight/";
        // The main button has narrower margins and key, so that "Koniec tury" (148.5 px at size 24) fits next to the key.
        ButtonSpec(Find(corner + "EndTurnButton"), "Buttons/ButtonPrimaryRed", 244, "End Turn", 32, "SPACE", 67, 10, 12, 6);
        ButtonSpec(Find(corner + "DeployMinionButton"), "Buttons/ButtonCall", 128, "Call", 16, "C", 22);
        ButtonSpec(Find(corner + "AbilityButton"), "Buttons/ButtonAbility", 384, "Ability", 16, "Q", 22);
        UIController ui = Object.FindFirstObjectByType<UIController>(FindObjectsInactive.Include);
        SetField(ui, "_primaryRedSprite", Spr("Buttons/ButtonPrimaryRed"));
        SetField(ui, "_primaryBlueSprite", Spr("Buttons/ButtonPrimaryBlue"));
        Log.Add("buttons ok");
    }

    private static void ButtonSpec(GameObject button, string sprite, float width, string key, float size, string shortcut, float keyWidth,
        float leftPad = 14.0f, float rightPad = 14.0f, float gap = 8.0f)
    {
        HudLayout.Place(Rt(button), 24, 136, width, 64);
        SetImage(button, Spr(sprite), Color.white, true);
        GameObject label = button.transform.Find("Text").gameObject;
        HudLayout.PlaceIn(Rt(label), leftPad, 2, width - leftPad - rightPad - keyWidth - gap, 62);
        SetText(label, key, size, Color.white, TextAlignmentOptions.MidlineLeft);
        if (label.GetComponent<LocalizedText>() != null) SetKey(label, key);
        // A long label ("End Turn") takes a smaller size instead of running into the shortcut.
        if (size > 16.0f) SetFloat(Get<FitTextSize>(label), "_maxSize", size);
        GameObject cap = Child(button.transform, "ShortcutKey");
        HudLayout.PlaceIn(Rt(cap), width - rightPad - keyWidth, 22, keyWidth, 24);
        SetImage(cap, Spr("Elements/Keycap"), Color.white);
        GameObject capText = Child(cap.transform, "Label");
        Stretch(Rt(capText));
        SetText(capText, shortcut, 16, Ink, TextAlignmentOptions.Center);
        SetKey(capText, shortcut);
    }

    private static void BattleLog()
    {
        const string corner = "Canvas/GameplayPanel/Layout/CornerTopRight/";
        GameObject canvas = Find(corner + "BattleLogCanvas");
        RectTransform canvasRect = Rt(canvas);
        canvasRect.anchorMin = canvasRect.anchorMax = canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.sizeDelta = new Vector2(1920, 1080);
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.localScale = Vector3.one;
        GameObject log = canvas.transform.Find("BattleLog").gameObject;
        HudLayout.Place(Rt(log), 24, 212, 384, 216);
        GameObject panel = log.transform.Find("Panel").gameObject;
        Stretch(Rt(panel));
        SetImage(panel, Spr("Panels/PanelLog"), Color.white);
        GameObject title = panel.transform.Find("Title").gameObject;
        HudLayout.PlaceIn(Rt(title), 16, 14, 240, 16);
        SetText(title, "Battle log", 16, Color.white, TextAlignmentOptions.MidlineLeft);
        SetKey(title, "Battle log");
        GameObject toggle = panel.transform.Find("ToggleButton").gameObject;
        RectTransform toggleRect = Rt(toggle);
        toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.pivot = new Vector2(1, 1);
        toggleRect.sizeDelta = new Vector2(96, 24);
        toggleRect.anchoredPosition = new Vector2(-16, -10);
        SetImage(toggle, null, Color.clear, true);
        GameObject label = toggle.transform.Find("Label").gameObject;
        Stretch(Rt(label));
        SetText(label, "Hide", 16, Accent, TextAlignmentOptions.MidlineRight);
        label.GetComponent<TMP_Text>().raycastTarget = true;
        GameObject body = panel.transform.Find("Body").gameObject;
        Stretch(Rt(body), 16, 42, 16, 14);
        GameObject entries = body.transform.Find("Entries").gameObject;
        TMP_Text entriesText = SetText(entries, "", 16, Color.white, TextAlignmentOptions.TopLeft, false);

        BattleLogController controller = log.GetComponent<BattleLogController>();
        SetField(controller, "_abilityButton", Find(corner + "AbilityButton"));
        SetField(controller, "_fullSprite", Spr("Panels/PanelLog"));
        SetField(controller, "_shortSprite", Spr("Panels/PanelLogShort"));
        SetField(controller, "_collapsedSprite", Spr("Panels/PanelLogCollapsed"));
        SetColor(controller, "_hotColor", Hot);
        SetColor(controller, "_coldColor", Cold);
        Log.Add("log ok");
    }

    // The details panel: name, the row of the kind and the statistics, the tags with icons, and the empty state.
    private static void InfoPanel()
    {
        GameObject panel = Find("Canvas/GameplayPanel/Layout/CornerBottomLeft/InfoPanel");
        HudLayout.Place(Rt(panel), 24, 848, 872, 208);
        SetImage(panel, Spr("Panels/PanelInfo"), Color.white);
        UnitTilePanelController controller = panel.GetComponent<UnitTilePanelController>();

        GameObject name = panel.transform.Find("NameText").gameObject;
        HudLayout.PlaceIn(Rt(name), 21, 15, 640, 32);
        TMP_Text nameText = SetText(name, "", 32, Color.white, TextAlignmentOptions.MidlineLeft);
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 16;
        nameText.fontSizeMax = 32;

        GameObject row = Child(panel.transform, "StatsRow");
        HudLayout.PlaceIn(Rt(row), 19, 52, 820, 32);
        Row(row, 26.0f);
        GameObject kindGroup = Child(row.transform, "KindGroup");
        Row(kindGroup, 11.0f);
        GameObject square = Child(kindGroup.transform, "KindSquare");
        SetImage(square, Spr("Elements/Keycap"), Hot);
        LayoutElement squareLayout = Get<LayoutElement>(square);
        squareLayout.preferredWidth = 10;
        squareLayout.preferredHeight = 10;
        squareLayout.flexibleWidth = 0;
        GameObject kind = FindDeep(panel.transform, "DescriptionText");
        kind.transform.SetParent(kindGroup.transform, false);
        SetText(kind, "", 16, Muted, TextAlignmentOptions.BaselineLeft, false);

        string[] groups = { "MoveGroup", "StrengthGroup", "RangeGroup" };
        string[] labelNames = { "MoveRangeText", "AttackStrengthText", "HPText" };
        string[] valueNames = { "MoveRange", "AttackStrength", "HP" };
        string[] keys = { "MOVE", "STRENGTH", "RANGE" };
        string[] newLabelNames = { "MoveRangeText", "AttackStrengthText", "AttackRangeText" };
        string[] newValueNames = { "MoveRange", "AttackStrength", "AttackRange" };
        TMP_Text[] labelTexts = new TMP_Text[3];
        TMP_Text[] valueTexts = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject group = Child(row.transform, groups[i]);
            Row(group, 8.0f);
            GameObject label = FindDeep(panel.transform, labelNames[i]) ?? FindDeep(panel.transform, newLabelNames[i]);
            GameObject value = FindDeep(panel.transform, valueNames[i]) ?? FindDeep(panel.transform, newValueNames[i]);
            label.transform.SetParent(group.transform, false);
            value.transform.SetParent(group.transform, false);
            label.name = newLabelNames[i];
            value.name = newValueNames[i];
            labelTexts[i] = SetText(label, keys[i], 16, Muted, TextAlignmentOptions.BaselineLeft);
            SetKey(label, keys[i]);
            valueTexts[i] = SetText(value, "", 32, Color.white, TextAlignmentOptions.BaselineLeft, false);
        }

        foreach (string old in new[] { "SkillsText", "EffectsText", "Skills", "Effects" })
        {
            Transform t = panel.transform.Find(old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        GameObject tagList = Child(panel.transform, "TagList");
        HudLayout.PlaceIn(Rt(tagList), 20, 86, 832, 116);
        GameObject template = Child(tagList.transform, "TagRowTemplate");
        HudLayout.PlaceIn(Rt(template), 0, 0, 832, 58);
        GameObject frame = Child(template.transform, "IconFrame");
        HudLayout.PlaceIn(Rt(frame), 0, 0, 36, 36);
        SetImage(frame, Spr("Elements/IconFrame"), Color.white);
        GameObject icon = Child(frame.transform, "Icon");
        HudLayout.PlaceIn(Rt(icon), 7, 7, 22, 22);
        SetImage(icon, null, Color.white);
        GameObject tagName = Child(template.transform, "Name");
        HudLayout.PlaceIn(Rt(tagName), 48, 2, 784, 16);
        SetText(tagName, "TAG", 16, Accent, TextAlignmentOptions.MidlineLeft);
        GameObject tagDescription = Child(template.transform, "Description");
        HudLayout.PlaceIn(Rt(tagDescription), 48, 22, 784, 36);
        TMP_Text descriptionText = SetText(tagDescription, "description", 16, Muted, TextAlignmentOptions.TopLeft, false, true);
        LineHeight(descriptionText, 24.0f);
        descriptionText.enableAutoSizing = true;
        descriptionText.fontSizeMin = 12;
        descriptionText.fontSizeMax = 16;
        template.SetActive(false);

        GameObject emptyTitle = Child(panel.transform, "EmptyTitle");
        HudLayout.PlaceIn(Rt(emptyTitle), 22, 15, 640, 32);
        SetText(emptyTitle, "DETAILS", 32, Muted, TextAlignmentOptions.MidlineLeft);
        SetKey(emptyTitle, "DETAILS");
        GameObject emptyHint = Child(panel.transform, "EmptyHint");
        HudLayout.PlaceIn(Rt(emptyHint), 22, 61, 700, 48);
        TMP_Text hintText = SetText(emptyHint, "Hover over a unit or tile, or click a card below.", 16, Muted, TextAlignmentOptions.TopLeft, false, true);
        LineHeight(hintText, 24.0f);
        SetKey(emptyHint, "Hover over a unit or tile, or click a card below.");

        SetField(controller, "_name", nameText);
        SetField(controller, "_description", kind.GetComponent<TMP_Text>());
        SetField(controller, "_kindSquare", square.GetComponent<Image>());
        SetField(controller, "_moveRangeText", labelTexts[0]);
        SetField(controller, "_moveRange", valueTexts[0]);
        SetField(controller, "_attackStrengthText", labelTexts[1]);
        SetField(controller, "_attackStrength", valueTexts[1]);
        SetField(controller, "_attackRangeText", labelTexts[2]);
        SetField(controller, "_attackRange", valueTexts[2]);
        SetField(controller, "_tagList", Rt(tagList));
        SetField(controller, "_tagRowTemplate", template);
        SetField(controller, "_emptyTitle", emptyTitle.GetComponent<TMP_Text>());
        SetField(controller, "_emptyHint", emptyHint.GetComponent<TMP_Text>());
        string[,] icons =
        {
            { "TOUGH", "Tough" }, { "GUNMAN", "Gunman" }, { "BINARY", "Binary" }, { "SWIFT", "Swift" }, { "RECOVERY", "Recovery" },
            { "PIERCING", "Piercing" }, { "SPY", "Spy" }, { "IMPAIR", "Impair" }, { "IMPALE", "Impale" }, { "ZEAL", "Zeal" },
            { "PROVOKE", "Provoke" }, { "BACKSTAB", "Backstab" }, { "Teleporter", "Teleporter" }, { "Confuser", "Confuser" }, { "Caller", "Caller" }
        };
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty list = so.FindProperty("_tagIcons");
        list.arraySize = icons.GetLength(0);
        for (int i = 0; i < icons.GetLength(0); i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("key").stringValue = icons[i, 0];
            element.FindPropertyRelative("sprite").objectReferenceValue = Spr("Icons/IconTag" + icons[i, 1]);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Log.Add("info ok");
    }

    private static void TeamPanel()
    {
        GameObject panel = Find("Canvas/GameplayPanel/Layout/CornerBottomRight/PlayerUnitsPanel");
        HudLayout.Place(Rt(panel), 1024, 848, 872, 208);
        SetImage(panel, Spr("Panels/PanelTeam"), Color.white);
        GameObject team = Child(panel.transform, "TeamText");
        HudLayout.PlaceIn(Rt(team), 376, 10, 300, 16);
        SetText(team, "RED TEAM", 16, Hot, TextAlignmentOptions.MidlineLeft);
        GameObject count = Child(panel.transform, "CountText");
        HudLayout.PlaceIn(Rt(count), 649, 10, 200, 16);
        SetText(count, "IN PLAY 4/5", 16, Muted, TextAlignmentOptions.MidlineRight);
        int index = 0;
        foreach (Transform child in panel.transform)
        {
            if (child.GetComponent<ButtonUnitController>() == null) continue;
            // The cards of the old layout left their own positions on the children; the prefab has the new ones.
            PrefabUtility.RevertPrefabInstance(child.gameObject, InteractionMode.AutomatedAction);
            HudLayout.PlaceIn((RectTransform)child, 373.0f + index * 96.7f, 40, 90, 150);
            child.localScale = Vector3.one;
            index++;
        }
        PlayerUnitsController controller = panel.GetComponent<PlayerUnitsController>();
        SetField(controller, "_teamText", team.GetComponent<TMP_Text>());
        SetField(controller, "_countText", count.GetComponent<TMP_Text>());
        Log.Add("team ok " + index);
    }

    // The damage preview next to an enemy in range: name and health, the damage, and a note about his cover.
    private static void DamageCallout()
    {
        GameObject layout = Find("Canvas/GameplayPanel/Layout");
        GameObject callout = Child(layout.transform, "DamageCallout");
        HudLayout.Place(Rt(callout), 944, 404, 340, 126);
        SetImage(callout, Spr("Panels/PanelCallout"), Color.white);
        Get<CanvasGroup>(callout);
        callout.transform.SetAsLastSibling();

        GameObject name = Child(callout.transform, "NameText");
        HudLayout.PlaceIn(Rt(name), 18, 14, 200, 16);
        TMP_Text nameText = SetText(name, "", 16, Cold, TextAlignmentOptions.MidlineLeft);
        GameObject health = Child(callout.transform, "HealthText");
        HudLayout.PlaceIn(Rt(health), 222, 14, 100, 16);
        TMP_Text healthText = SetText(health, "", 16, Muted, TextAlignmentOptions.MidlineRight, false);
        GameObject damage = Child(callout.transform, "DamageText");
        HudLayout.PlaceIn(Rt(damage), 18, 40, 300, 32);
        TMP_Text damageText = SetText(damage, "", 32, Color.white, TextAlignmentOptions.MidlineLeft, false);

        GameObject note = Child(callout.transform, "Note");
        HudLayout.PlaceIn(Rt(note), 18, 82, 304, 22);
        GameObject frame = Child(note.transform, "IconFrame");
        HudLayout.PlaceIn(Rt(frame), 0, 0, 22, 22);
        SetImage(frame, Spr("Elements/IconFrame"), Color.white);
        GameObject icon = Child(frame.transform, "Icon");
        HudLayout.PlaceIn(Rt(icon), 2, 2, 18, 18);
        Image iconImage = SetImage(icon, Spr("Icons/IconTagPiercing"), Color.white);
        GameObject noteLabel = Child(note.transform, "NoteText");
        HudLayout.PlaceIn(Rt(noteLabel), 30, 3, 274, 16);
        TMP_Text noteText = SetText(noteLabel, "", 16, Muted, TextAlignmentOptions.MidlineLeft);

        DamageCalloutController controller = Get<DamageCalloutController>(callout);
        SetField(controller, "_nameText", nameText);
        SetField(controller, "_healthText", healthText);
        SetField(controller, "_damageText", damageText);
        SetField(controller, "_note", note);
        SetField(controller, "_noteText", noteText);
        SetField(controller, "_noteIcon", iconImage);
        SetField(controller, "_piercingIcon", Spr("Icons/IconTagPiercing"));
        SetField(controller, "_coverIcon", Spr("Icons/IconTagTough"));
        UIController ui = Object.FindFirstObjectByType<UIController>(FindObjectsInactive.Include);
        SetField(ui, "_damageCallout", controller);
        Log.Add("callout ok");
    }

    private static void EndGame()
    {
        GameObject overlay = Find("Canvas/EndGameOverlay");
        GameObject dim = overlay.transform.Find("Dim").gameObject;
        SetImage(dim, Spr("Menu/OverlayDim"), Color.white, true);
        GameObject buttons = overlay.transform.Find("Buttons").gameObject;
        Sprite normal = Spr("Menu/ButtonMenu");

        GameObject resume = buttons.transform.Find("ResumeButton") != null ? buttons.transform.Find("ResumeButton").gameObject : null;
        if (resume == null)
        {
            resume = Object.Instantiate(buttons.transform.Find("PlayAgainButton").gameObject, buttons.transform, false);
            resume.name = "ResumeButton";
            Button resumeButton = resume.GetComponent<Button>();
            resumeButton.onClick = new Button.ButtonClickedEvent();
        }
        string[] names = { "PlayAgainButton", "BackToMenuButton", "SettingsButton", "QuitGameButton", "SwapSidesButton", "ResumeButton" };
        string[] keys = { "Play again", "Back to Menu", "Settings", "Quit", null, "Resume" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject button = buttons.transform.Find(names[i]).gameObject;
            SetImage(button, normal, Color.white, true);
            HudLayout.Place(Rt(button), 580, 624, 372, 72);
            GameObject label = button.transform.Find("Text").gameObject;
            Stretch(Rt(label));
            TMP_Text text = SetText(label, keys[i] ?? "", 32, Color.white, TextAlignmentOptions.Center);
            if (keys[i] != null) SetKey(label, keys[i]);
            if (names[i] == "SwapSidesButton") text.fontSize = 32;
        }

        GameObject summary = buttons.transform.Find("SummaryPanel").gameObject;
        HudLayout.Place(Rt(summary), 580, 318, 760, 240);
        SetImage(summary, Spr("Panels/PanelSummary"), Color.white);
        GameObject summaryText = summary.transform.Find("Text").gameObject;
        TMP_Text summaryTmp = SetText(summaryText, "", 16, Color.white, TextAlignmentOptions.MidlineLeft, false);

        GameObject pauseTitle = Child(buttons.transform, "PauseTitle");
        TMP_Text pauseTitleText = SetText(pauseTitle, "Pause", 48, Color.white, TextAlignmentOptions.Center);
        SetKey(pauseTitle, "Pause");
        HudLayout.Place(Rt(pauseTitle), 0, 330, 1920, 48);
        GameObject pauseHint = Child(buttons.transform, "PauseHint");
        TMP_Text pauseHintText = SetText(pauseHint, "ESC - Resume the game", 16, Muted, TextAlignmentOptions.Center);
        SetKey(pauseHint, "ESC - Resume the game");
        HudLayout.Place(Rt(pauseHint), 0, 596, 1920, 24);
        GameObject record = Child(buttons.transform, "RecordText");
        TMP_Text recordText = SetText(record, "", 16, Accent, TextAlignmentOptions.Center);
        HudLayout.Place(Rt(record), 580, 574, 760, 24);

        EndGameController controller = overlay.GetComponent<EndGameController>();
        SetField(controller, "_dim", dim.GetComponent<Image>());
        SetField(controller, "_resumeButton", resume.GetComponent<Button>());
        SetField(controller, "_pauseTitle", pauseTitleText);
        SetField(controller, "_pauseHint", pauseHintText);
        SetField(controller, "_recordText", recordText);
        SetField(controller, "_buttonSprite", normal);
        SetField(controller, "_buttonSelectedSprite", Spr("Menu/ButtonMenuSelected"));
        SetField(controller, "_bannerSprite", Spr("Panels/PanelWinner"));
        SetFloat(controller, "_dimAlpha", 1.0f);
        SetFloat(controller, "_summaryFontSize", 32.0f);
        SetFloat(controller, "_summaryLabelSize", 16.0f);
        SetFloat(controller, "_winnerFontSize", 48.0f);
        Log.Add("endgame ok");
    }

    // The settings of the pause menu and the end screen, and the question "Are you sure?": the panels of the new style in the middle of the screen,
    // the rows and buttons of the same grid as the menu (760 px between the margins of 40 px).
    private static void SettingsPanels()
    {
        GameObject overlay = Find("Canvas/EndGameOverlay");
        Sprite menuButton = Spr("Menu/ButtonMenu");

        GameObject settings = overlay.transform.Find("SettingsPanel").gameObject;
        HudLayout.Place(Rt(settings), 540, 290, 840, 500);
        SetImage(settings, Spr("Panels/PanelSettings"), Color.white);
        Restyle(settings.transform.Find("Title").gameObject, 48, Color.white, TextAlignmentOptions.Center, 40, 24, 760, 48);
        Restyle(settings.transform.Find("SoundLabel").gameObject, 16, Muted, TextAlignmentOptions.MidlineLeft, 40, 112, 280, 32);
        Restyle(settings.transform.Find("MusicLabel").gameObject, 16, Muted, TextAlignmentOptions.MidlineLeft, 40, 164, 280, 32);
        StyleSlider(settings.transform.Find("SoundSlider").gameObject, 340, 112);
        StyleSlider(settings.transform.Find("MusicSlider").gameObject, 340, 164);
        MenuButton(settings.transform.Find("ResolutionButton").gameObject, Spr("Menu/ButtonMenuWide"), 40, 216, 760, false);
        MenuButton(settings.transform.Find("DifficultyButton").gameObject, Spr("Menu/ButtonMenuWide"), 40, 296, 760, false);
        MenuButton(settings.transform.Find("BackButton").gameObject, menuButton, 234, 396, 372, false);

        GameObject confirm = overlay.transform.Find("ConfirmPanel").gameObject;
        HudLayout.Place(Rt(confirm), 540, 400, 840, 280);
        SetImage(confirm, Spr("Panels/PanelConfirm"), Color.white);
        Restyle(confirm.transform.Find("Title").gameObject, 32, Color.white, TextAlignmentOptions.Center, 40, 28, 760, 32);
        TMP_Text detail = Restyle(confirm.transform.Find("Detail").gameObject, 16, Muted, TextAlignmentOptions.Top, 40, 84, 760, 48, false, true);
        LineHeight(detail, 24.0f);
        MenuButton(confirm.transform.Find("YesButton").gameObject, Spr("Menu/ButtonMenuSelected"), 40, 168, 372, true);
        MenuButton(confirm.transform.Find("NoButton").gameObject, menuButton, 428, 168, 372, false);
        Log.Add("settings panels ok");
    }

    // The text of an object keeps its words and takes the style and the place (inside its parent, from the top left corner).
    private static TMP_Text Restyle(GameObject go, float size, Color color, TextAlignmentOptions alignment, float x, float y, float width, float height, bool upper = true, bool wrap = false)
    {
        HudLayout.PlaceIn(Rt(go), x, y, width, height);
        return SetText(go, go.GetComponent<TMP_Text>().text, size, color, alignment, upper, wrap);
    }

    // A menu button with its label in the size of the menu (32, smaller when the words are too long for the button).
    private static void MenuButton(GameObject button, Sprite sprite, float x, float y, float width, bool selected)
    {
        HudLayout.PlaceIn(Rt(button), x, y, width, 72);
        SetImage(button, sprite, Color.white, true);
        GameObject label = button.transform.Find("Text").gameObject;
        Stretch(Rt(label), 14, 6, 14, 0);
        SetText(label, label.GetComponent<TMP_Text>().text, 32, selected ? Accent : Color.white, TextAlignmentOptions.Center);
        SetFloat(Get<FitTextSize>(label), "_maxSize", 32.0f);
    }

    // The slider of the settings: a thin track and fill in the middle of its rect, and a handle (before it was a bare flat bar).
    private static void StyleSlider(GameObject slider, float x, float y)
    {
        HudLayout.PlaceIn(Rt(slider), x, y, 460, 32);
        GameObject background = slider.transform.Find("Background").gameObject;
        CenterBar(Rt(background), 0.0f);
        SetImage(background, Spr("Menu/SliderTrack"), Color.white, true);
        GameObject fillArea = slider.transform.Find("Fill Area").gameObject;
        CenterBar(Rt(fillArea), -16.0f);
        SetImage(fillArea.transform.Find("Fill").gameObject, Spr("Menu/SliderFill"), Accent);
        GameObject slide = Child(slider.transform, "Handle Slide Area");
        Stretch(Rt(slide), 8, 0, 8, 0);
        GameObject handle = Child(slide.transform, "Handle");
        RectTransform handleRect = Rt(handle);
        handleRect.anchorMin = new Vector2(0.0f, 0.0f);
        handleRect.anchorMax = new Vector2(0.0f, 1.0f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(16.0f, 0.0f);
        handleRect.anchoredPosition = Vector2.zero;
        Image handleImage = SetImage(handle, Spr("Menu/SliderHandle"), Color.white, true);
        SerializedObject so = new SerializedObject(slider.GetComponent<Slider>());
        so.FindProperty("m_HandleRect").objectReferenceValue = handleRect;
        so.FindProperty("m_TargetGraphic").objectReferenceValue = handleImage;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CenterBar(RectTransform rect, float widthDelta)
    {
        rect.anchorMin = new Vector2(0.0f, 0.5f);
        rect.anchorMax = new Vector2(1.0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(widthDelta, 8.0f);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void WireUi()
    {
        UIController ui = Object.FindFirstObjectByType<UIController>(FindObjectsInactive.Include);
        SetColor(ui, "_warningColor", new Color32(0xFF, 0x4D, 0x4D, 0xFF));
    }
}
