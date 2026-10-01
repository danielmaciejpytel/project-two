using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Puts the layered gameplay background (Assets/Sprites/Background/Layers) into MainScene in place of the old frame animation:
// the old object is renamed BackgroundAnimationOld and switched off, the new BackgroundAnimation is a copy of its placement
// (WorldBackdrop) with one sprite per layer and a BackgroundMotion that animates them. Safe to run again: the new object is rebuilt.
// Menu: Tools > Background > Build.
public static class BackgroundBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string SpriteRoot = "Assets/Sprites/Background/Layers/";
    private const string OldName = "BackgroundAnimationOld";
    private const string NewName = "BackgroundAnimation";
    private const float DepthStep = 0.02f; // world units between layers: all of them must stay behind BackgroundDim (z -0.2)
    private const float WrapHeight = 1080.0f;

    private struct Spec
    {
        public string Name;
        public string Copy;
        public Vector2 Sway, SwayFrequency, Drift;
        public float Phase, Rotation, RotationFrequency, ScalePulse, ScaleFrequency, AlphaPulse, AlphaFrequency, Wrap;
    }

    // Bottom to top. The old animation moved its shards about 6 px (a cycle of 4 s); these move 14-26 px, a little more lively but still calm.
    private static readonly Spec[] Specs =
    {
        new Spec { Name = "BackgroundBase" },
        new Spec { Name = "ShardA", Sway = new Vector2(14, 10), SwayFrequency = new Vector2(0.11f, 0.14f), Phase = 0.00f, Rotation = 0.25f, RotationFrequency = 0.08f, ScalePulse = 0.004f, ScaleFrequency = 0.10f },
        new Spec { Name = "ShardB", Sway = new Vector2(18, 13), SwayFrequency = new Vector2(0.13f, 0.10f), Phase = 0.20f, Rotation = 0.40f, RotationFrequency = 0.07f, ScalePulse = 0.005f, ScaleFrequency = 0.09f },
        new Spec { Name = "ShardC", Sway = new Vector2(22, 16), SwayFrequency = new Vector2(0.09f, 0.15f), Phase = 0.45f, Rotation = 0.50f, RotationFrequency = 0.09f, ScalePulse = 0.006f, ScaleFrequency = 0.08f },
        new Spec { Name = "ShardD", Sway = new Vector2(26, 18), SwayFrequency = new Vector2(0.12f, 0.11f), Phase = 0.70f, Rotation = 0.60f, RotationFrequency = 0.06f, ScalePulse = 0.007f, ScaleFrequency = 0.11f },
        new Spec { Name = "DustFar", Copy = "DustFarCopy", Sway = new Vector2(12, 0), SwayFrequency = new Vector2(0.08f, 0.0f), Drift = new Vector2(0, 8), Wrap = WrapHeight, AlphaPulse = 0.25f, AlphaFrequency = 0.1f },
        new Spec { Name = "DustNear", Copy = "DustNearCopy", Sway = new Vector2(24, 0), SwayFrequency = new Vector2(0.05f, 0.0f), Drift = new Vector2(0, 16), Phase = 0.4f, Wrap = WrapHeight, AlphaPulse = 0.30f, AlphaFrequency = 0.07f },
    };

    [MenuItem("Tools/Background/Build")]
    public static string Build()
    {
        List<string> log = new List<string>();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject old = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == NewName && t.GetComponent<WorldBackdrop>() != null && t.GetComponent<BackgroundMotion>() != null) Object.DestroyImmediate(t.gameObject);
            }
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if ((t.name == NewName || t.name == OldName) && t.GetComponent<WorldBackdrop>() != null) old = t.gameObject;
        if (old == null) return "The old BackgroundAnimation was not found.";

        SpriteRenderer oldRenderer = old.GetComponent<SpriteRenderer>();
        GameObject copy = Object.Instantiate(old, old.transform.parent);
        old.name = OldName;
        old.SetActive(false);
        copy.name = NewName;
        copy.SetActive(true);
        copy.transform.SetSiblingIndex(old.transform.GetSiblingIndex() + 1);
        foreach (System.Type type in new[] { typeof(SpriteRenderer), typeof(Animator), typeof(CanvasRenderer) })
        {
            Component component = copy.GetComponent(type);
            if (component != null) Object.DestroyImmediate(component);
        }

        BackgroundMotion motion = copy.AddComponent<BackgroundMotion>();
        List<BackgroundMotion.Layer> layers = new List<BackgroundMotion.Layer>();
        float rootDepth = Mathf.Abs(copy.transform.lossyScale.z) < 1e-6f ? 1.0f : copy.transform.lossyScale.z;
        for (int i = 0; i < Specs.Length; i++)
        {
            Spec spec = Specs[i];
            Transform target = Layer(copy.transform, spec.Name, oldRenderer, i, rootDepth, 0.0f, log);
            Transform second = spec.Copy != null ? Layer(copy.transform, spec.Copy, oldRenderer, i, rootDepth, -WrapHeight / 100.0f, log) : null;
            layers.Add(new BackgroundMotion.Layer
            {
                Name = spec.Name, Target = target, Copy = second, Sway = spec.Sway, SwayFrequency = spec.SwayFrequency, Phase = spec.Phase,
                Rotation = spec.Rotation, RotationFrequency = spec.RotationFrequency == 0.0f ? 0.07f : spec.RotationFrequency,
                ScalePulse = spec.ScalePulse, ScaleFrequency = spec.ScaleFrequency == 0.0f ? 0.09f : spec.ScaleFrequency,
                Drift = spec.Drift, WrapHeight = spec.Wrap, AlphaPulse = spec.AlphaPulse, AlphaFrequency = spec.AlphaFrequency == 0.0f ? 0.08f : spec.AlphaFrequency,
            });
        }
        SerializedObject serialized = new SerializedObject(motion);
        SerializedProperty array = serialized.FindProperty("_layers");
        array.arraySize = layers.Count;
        for (int i = 0; i < layers.Count; i++)
        {
            SerializedProperty p = array.GetArrayElementAtIndex(i);
            BackgroundMotion.Layer l = layers[i];
            p.FindPropertyRelative("Name").stringValue = l.Name;
            p.FindPropertyRelative("Target").objectReferenceValue = l.Target;
            p.FindPropertyRelative("Copy").objectReferenceValue = l.Copy;
            p.FindPropertyRelative("Sway").vector2Value = l.Sway;
            p.FindPropertyRelative("SwayFrequency").vector2Value = l.SwayFrequency;
            p.FindPropertyRelative("Phase").floatValue = l.Phase;
            p.FindPropertyRelative("Rotation").floatValue = l.Rotation;
            p.FindPropertyRelative("RotationFrequency").floatValue = l.RotationFrequency;
            p.FindPropertyRelative("ScalePulse").floatValue = l.ScalePulse;
            p.FindPropertyRelative("ScaleFrequency").floatValue = l.ScaleFrequency;
            p.FindPropertyRelative("Drift").vector2Value = l.Drift;
            p.FindPropertyRelative("WrapHeight").floatValue = l.WrapHeight;
            p.FindPropertyRelative("AlphaPulse").floatValue = l.AlphaPulse;
            p.FindPropertyRelative("AlphaFrequency").floatValue = l.AlphaFrequency;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        log.Add(Upgrade(copy.transform));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Add("Built " + layers.Count + " layers under " + NewName + ", old object renamed " + OldName + " and disabled.");
        return string.Join("\n", log);
    }

    // Menu: Tools > Background > Add Shadows And Groups. Puts the shadow of every shard on its own layer (ShardAShadow ... just behind the shard)
    // and tags the dust and shadow layers so BackgroundMotion can switch them off. It keeps the motion values that were tuned in the Inspector
    // and is safe to run again.
    [MenuItem("Tools/Background/Add Shadows And Groups")]
    public static string UpgradeScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BackgroundMotion motion = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (BackgroundMotion found in root.GetComponentsInChildren<BackgroundMotion>(true)) motion = found;
        if (motion == null) return "BackgroundMotion not found.";
        string result = Upgrade(motion.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return result;
    }

    private static string Upgrade(Transform root)
    {
        List<string> log = new List<string>();
        SerializedObject serialized = new SerializedObject(root.GetComponent<BackgroundMotion>());
        SerializedProperty array = serialized.FindProperty("_layers");
        string[] shards = { "ShardA", "ShardB", "ShardC", "ShardD" };
        float depth = Mathf.Abs(root.lossyScale.z) < 1e-6f ? 1.0f : root.lossyScale.z;
        foreach (string shard in shards)
        {
            string shadowName = shard + "Shadow";
            Transform shardTransform = root.Find(shard);
            if (shardTransform == null || root.Find(shadowName) != null) continue;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + shadowName + ".png");
            if (sprite == null) { log.Add("MISSING SPRITE " + shadowName); continue; }
            SpriteRenderer shardRenderer = shardTransform.GetComponent<SpriteRenderer>();
            GameObject go = new GameObject(shadowName, typeof(SpriteRenderer));
            go.layer = root.gameObject.layer;
            go.transform.SetParent(root, false);
            go.transform.SetSiblingIndex(shardTransform.GetSiblingIndex());
            go.transform.localPosition = shardTransform.localPosition + new Vector3(0.0f, 0.0f, DepthStep * 0.5f / depth); // just behind the shard
            go.transform.localRotation = shardTransform.localRotation;
            go.transform.localScale = shardTransform.localScale;
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = shardRenderer.sortingLayerID;
            renderer.sortingOrder = shardRenderer.sortingOrder;

            int source = -1;
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue == shard) source = i;
            if (source < 0) { log.Add("No layer entry for " + shard); continue; }
            array.InsertArrayElementAtIndex(source); // the copy goes in front of the shard's entry and starts as its twin
            SerializedProperty entry = array.GetArrayElementAtIndex(source);
            entry.FindPropertyRelative("Name").stringValue = shadowName;
            entry.FindPropertyRelative("Target").objectReferenceValue = go.transform;
            entry.FindPropertyRelative("Copy").objectReferenceValue = null;
            entry.FindPropertyRelative("Group").enumValueIndex = (int)BackgroundMotion.LayerGroup.Shadows;
            log.Add("Added " + shadowName);
        }
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty entry = array.GetArrayElementAtIndex(i);
            string name = entry.FindPropertyRelative("Name").stringValue;
            if (name.StartsWith("Dust")) entry.FindPropertyRelative("Group").enumValueIndex = (int)BackgroundMotion.LayerGroup.Dust;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        log.Add("Layers: " + array.arraySize);
        return string.Join(", ", log);
    }

    private static Transform Layer(Transform parent, string name, SpriteRenderer like, int index, float rootDepth, float offsetY, List<string> log)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + (name.EndsWith("Copy") ? name.Substring(0, name.Length - 4) : name) + ".png");
        if (sprite == null) log.Add("MISSING SPRITE " + name);
        GameObject go = new GameObject(name, typeof(SpriteRenderer));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0.0f, offsetY, -DepthStep * index / rootDepth);
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = like.sortingLayerID;
        renderer.sortingOrder = like.sortingOrder;
        return go.transform;
    }
}
