using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Screen resolution chosen in Options. The game starts in the monitor's native resolution
/// (borderless fullscreen) and remembers a different choice between sessions.
/// </summary>
public static class DisplaySettings
{
    private const string PrefKey = "Options.Resolution";
    private const int MinWidth = 1024;

    // Resolution of the monitor the game runs on.
    public static Vector2Int Native => new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);

    // Native resolution first, then the smaller ones the monitor supports.
    public static List<Vector2Int> Available()
    {
        Vector2Int native = Native;
        List<Vector2Int> list = new List<Vector2Int> { native };
        foreach (Resolution resolution in Screen.resolutions)
        {
            Vector2Int size = new Vector2Int(resolution.width, resolution.height);
            if (size.x < MinWidth || size.x > native.x || size.y > native.y || list.Contains(size)) continue;
            list.Add(size);
        }
        list.Sort((a, b) => a == native ? -1 : b == native ? 1 : b.x != a.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
        return list;
    }

    // The saved choice, or native when nothing was chosen (or the saved one isn't available any more).
    public static Vector2Int Current
    {
        get
        {
            string saved = PlayerPrefs.GetString(PrefKey, "");
            string[] parts = saved.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                Vector2Int size = new Vector2Int(width, height);
                if (Available().Contains(size)) return size;
            }
            return Native;
        }
    }

    public static bool IsNative(Vector2Int size) => size == Native;

    // Applies the resolution and saves it right away.
    public static void Set(Vector2Int size)
    {
        if (IsNative(size)) PlayerPrefs.DeleteKey(PrefKey);
        else PlayerPrefs.SetString(PrefKey, size.x + "x" + size.y);
        PlayerPrefs.Save();
        Apply(size);
    }

    private static void Apply(Vector2Int size)
    {
#if !UNITY_EDITOR
        // Native: borderless window on the desktop; smaller sizes switch the monitor's mode.
        FullScreenMode mode = IsNative(size) ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen;
        Screen.SetResolution(size.x, size.y, mode);
        Debug.Log("Resolution set to " + size.x + "x" + size.y + " " + mode);
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStart() => Apply(Current);
}
