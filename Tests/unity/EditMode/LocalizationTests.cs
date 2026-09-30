using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

// Keeps Polish.txt complete and consistent: a text added to a scene or to the code without a translation shows
// up here instead of staying in English in the Polish version.
public class LocalizationTests
{
    private static string ProjectPath(string relative) => Path.Combine(Application.dataPath, relative);

    private static IEnumerable<string> PolishLines()
    {
        foreach (string line in File.ReadAllLines(ProjectPath("Resources/Localization/Polish.txt")))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            yield return line;
        }
    }

    private static string Placeholders(string text)
    {
        List<string> found = new List<string>();
        foreach (Match match in Regex.Matches(text, @"\{\d+\}")) found.Add(match.Value);
        found.Sort();
        return string.Join(",", found);
    }

    [Test]
    public void EveryPolishLineHasAKeyAndATranslation()
    {
        foreach (string line in PolishLines())
        {
            int tab = line.IndexOf('\t');
            Assert.Greater(tab, 0, "No tab between the English text and the translation: " + line);
            Assert.IsNotEmpty(line.Substring(tab + 1).Trim(), "Empty translation: " + line);
        }
    }

    [Test]
    public void PolishFileHasNoDuplicateKeys()
    {
        HashSet<string> keys = new HashSet<string>();
        foreach (string line in PolishLines())
        {
            int tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            Assert.IsTrue(keys.Add(line.Substring(0, tab)), "Duplicate key: " + line.Substring(0, tab));
        }
    }

    [Test]
    public void TranslationsKeepTheFormatPlaceholders()
    {
        foreach (string line in PolishLines())
        {
            int tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            string key = line.Substring(0, tab);
            Assert.AreEqual(Placeholders(key), Placeholders(line.Substring(tab + 1)), "Placeholders differ: " + key);
        }
    }

    [Test]
    public void EveryTextOfTheScenesHasATranslation()
    {
        List<string> missing = new List<string>();
        foreach (string scene in Directory.GetFiles(ProjectPath("Scenes"), "*.unity"))
        {
            foreach (string key in SceneKeys(File.ReadAllLines(scene)))
            {
                if (key.Length > 0 && !Loc.HasPolish(key)) missing.Add(Path.GetFileName(scene) + ": " + key);
            }
        }
        Assert.IsEmpty(missing, "Scene texts without a Polish translation:\n" + string.Join("\n", missing));
    }

    [Test]
    public void EveryTextInTheCodeHasATranslation()
    {
        List<string> missing = new List<string>();
        // Only whole literals: a text built with + is not a key.
        // Texts passed to Loc, and the ones kept for it: hints (const string Hint...) and questions (Ask("...", ...)).
        Regex call = new Regex("(?:Loc\\.[TF]\\(|const string Hint\\w* = |\\bAsk\\()\"((?:[^\"\\\\]|\\\\.)*)\"\\s*[,)|;]");
        foreach (string file in Directory.GetFiles(ProjectPath("Scripts"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in call.Matches(File.ReadAllText(file)))
            {
                string key = Regex.Unescape(match.Groups[1].Value);
                if (!Loc.HasPolish(key)) missing.Add(Path.GetFileName(file) + ": " + key);
            }
        }
        Assert.IsEmpty(missing, "Texts in the code without a Polish translation:\n" + string.Join("\n", missing));
    }

    // The "_key" values of all LocalizedText components. Unity writes long texts as quoted YAML that continues on
    // the following lines (a line break in the file is a space), with \n and \uXXXX escapes.
    private static IEnumerable<string> SceneKeys(string[] lines)
    {
        Regex start = new Regex("^\\s*_key: ?(.*)$");
        for (int i = 0; i < lines.Length; i++)
        {
            Match match = start.Match(lines[i]);
            if (!match.Success) continue;
            string value = match.Groups[1].Value.Trim();
            if (value.Length > 0 && (value[0] == '"' || value[0] == '\''))
            {
                char quote = value[0];
                int blankLines = 0;
                while (!EndsQuoted(value, quote) && i + 1 < lines.Length)
                {
                    string next = lines[++i].Trim();
                    // A line break folds into a space, an empty line in between is a real line break.
                    if (next.Length == 0) blankLines++;
                    else
                    {
                        value += blankLines == 0 ? " " : new string('\n', blankLines);
                        value += next;
                        blankLines = 0;
                    }
                }
                value = value.Substring(1, value.Length - 2);
                if (quote == '"')
                {
                    value = Regex.Replace(value, "\\\\u([0-9A-Fa-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                    value = value.Replace("\\n", "\n").Replace("\\\"", "\"");
                }
                else value = value.Replace("''", "'");
            }
            yield return value;
        }
    }

    private static bool EndsQuoted(string value, char quote)
    {
        if (value.Length < 2 || value[value.Length - 1] != quote) return false;
        if (quote == '\'') return true;
        // In double quotes a backslash before the last quote makes it part of the text.
        int backslashes = 0;
        for (int i = value.Length - 2; i >= 0 && value[i] == '\\'; i--) backslashes++;
        return backslashes % 2 == 0;
    }
}
