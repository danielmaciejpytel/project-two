using System;
using System.Collections.Generic;
using UnityEngine;

public enum Language { English, Polish }

/// <summary>
/// Translations of the game's texts. The English text is the key, so code and scenes keep their
/// English strings and Loc.T returns the Polish one from Resources/Localization/pl.txt.
/// Texts without a translation stay in English. The language is chosen in Options (menu only).
/// </summary>
public static class Loc
{
    private const string PrefKey = "Options.Language";

    private static Dictionary<string, string> _polish;
    private static Language? _current;

    public static event Action Changed;

    public static Language Current
    {
        get
        {
            if (_current == null) _current = (Language)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, 1);
            return _current.Value;
        }
        set
        {
            if (Current == value) return;
            _current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    // Language names are shown in their own language.
    public static string LanguageName(Language language) => language == Language.Polish ? "Polski" : "English";

    public static string T(string key)
    {
        if (string.IsNullOrEmpty(key) || Current == Language.English) return key;
        if (_polish == null) Load();
        return _polish.TryGetValue(key, out string translation) ? translation : key;
    }

    // Same text with a different translation in some sentences ("Confuse" as the name of a button,
    // "Confuse@used" as the object of "used"). Falls back to the plain translation.
    public static string T(string key, string context)
    {
        if (string.IsNullOrEmpty(key) || Current == Language.English) return key;
        if (_polish == null) Load();
        return _polish.TryGetValue(key + "@" + context, out string translation) ? translation : T(key);
    }

    // Translates the format first, then fills in the arguments.
    public static string F(string format, params object[] args) => string.Format(T(format), args);

    // True when the text has a translation (used by the checks in the editor).
    public static bool HasPolish(string key)
    {
        if (_polish == null) Load();
        return _polish.ContainsKey(key);
    }

    private static void Load()
    {
        _polish = new Dictionary<string, string>();
        TextAsset file = Resources.Load<TextAsset>("Localization/pl");
        if (file == null)
        {
            Debug.LogError("Missing Resources/Localization/pl.txt");
            return;
        }
        // One entry per line: English text, a tab, the translation. "\n" stands for a line break.
        foreach (string line in file.text.Split('\n'))
        {
            string entry = line.TrimEnd('\r');
            if (entry.Length == 0 || entry[0] == '#') continue;
            int tab = entry.IndexOf('\t');
            if (tab <= 0) continue;
            _polish[Unescape(entry.Substring(0, tab))] = Unescape(entry.Substring(tab + 1));
        }
    }

    // The two characters backslash and n in the file stand for a line break.
    private static string Unescape(string text) => text.Replace("\\n", "\n");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _polish = null;
        _current = null;
        Changed = null;
    }
}
