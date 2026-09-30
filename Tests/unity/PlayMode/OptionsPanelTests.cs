using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static GameTestUtil;

// The OPTIONS panel of the main menu is laid out like the RECORDS panel: same text size, left edge, title and back button;
// every row turns pink under the mouse and brings in the bar of the menu.
public class OptionsPanelTests
{
    private static readonly string[] Rows = { "EffectsLabel", "MusicLabel", "DifficultyButton", "ResolutionButton", "LanguageButton" };
    private static readonly Color Pink = new Color32(0xD7, 0x2E, 0x66, 0xFF);

    [SetUp]
    public void SetUp() => Reset();

    [TearDown]
    public void TearDown() => Reset();

    private static IEnumerator OpenOptions()
    {
        yield return LoadScene("MenuScene");
        Find<Button>("optionsButton").onClick.Invoke();
        yield return null;
    }

    private static Transform OptionsPanel() => Find<RectTransform>("OptionsPanel");

    private static float LeftEdge(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return corners[0].x;
    }

    [UnityTest]
    public IEnumerator TheRowsHaveTheTextSizeOfTheRecordsTabs()
    {
        yield return OpenOptions();
        float tabSize = Find<RectTransform>("TabLEADERBOARD").GetComponentInChildren<TMP_Text>(true).fontSize;

        foreach (string name in Rows)
        {
            TMP_Text label = OptionsPanel().Find(name).GetComponentInChildren<TMP_Text>(true);
            Assert.AreEqual(tabSize, label.fontSize, name);
            Assert.IsFalse(label.enableAutoSizing, name + " has a fixed size");
        }
    }

    [UnityTest]
    public IEnumerator TheTitleAndTheBackButtonShareTheLeftEdgeLikeInRecords()
    {
        yield return OpenOptions();
        RectTransform title = (RectTransform)OptionsPanel().Find("Text");
        RectTransform back = (RectTransform)OptionsPanel().Find("backButton (1)");
        RectTransform recordsTitle = (RectTransform)Find<RectTransform>("RecordsPanel").Find("Title");
        RectTransform recordsBack = Find<RectTransform>("RecordsBackButton");

        Assert.AreEqual(LeftEdge(back), LeftEdge(title), 1.0f, "OPTIONS is aligned with BACK");
        Assert.AreEqual(LeftEdge(recordsTitle), LeftEdge(title), 1.0f, "Same left edge as RECORDS");
        Assert.AreEqual(recordsTitle.position.y, title.position.y, 1.0f, "Same height of the title");
        Assert.AreEqual(recordsBack.position.y, back.position.y, 1.0f, "BACK is as low as in RECORDS");
        foreach (string name in Rows) Assert.AreEqual(LeftEdge(back), LeftEdge((RectTransform)OptionsPanel().Find(name)), 1.0f, name);
    }

    [UnityTest]
    public IEnumerator EveryRowTurnsPinkUnderTheMouseAndBringsTheBarToItsHeight()
    {
        yield return OpenOptions();
        MainMenuController menu = Find<MainMenuController>("MainMenuPanel");
        Image bar = GetPrivate<Image>(menu, "_barImage");

        foreach (string name in Rows)
        {
            Transform row = OptionsPanel().Find(name);
            HoverTint hover = row.GetComponent<HoverTint>();
            Assert.IsNotNull(hover, name + " reacts to the mouse");
            TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
            Color normal = label.color;

            hover.OnPointerEnter(null);
            Assert.AreEqual(Pink, label.color, name + " turns pink");
            Assert.AreEqual(label.transform.position.y, bar.transform.position.y, 0.5f, "The bar comes behind " + name);

            hover.OnPointerExit(null);
            Assert.AreEqual(normal, label.color, name + " goes back");
        }
    }

    [UnityTest]
    public IEnumerator TheSlidersAreAPartOfTheirRow()
    {
        yield return OpenOptions();
        TMP_Text effects = OptionsPanel().Find("EffectsLabel").GetComponent<TMP_Text>();

        OptionsPanel().Find("EffectsSlider").GetComponent<HoverTint>().OnPointerEnter(null);
        Assert.AreEqual(Pink, effects.color, "Over the slider the label of the row is pink");

        OptionsPanel().Find("EffectsSlider").GetComponent<HoverTint>().OnPointerExit(null);
        Assert.AreNotEqual(Pink, effects.color);
    }

    [UnityTest]
    public IEnumerator ClosingThePanelWithTheMouseOverARowDoesNotLeaveItPink()
    {
        yield return OpenOptions();
        TMP_Text label = OptionsPanel().Find("LanguageButton").GetComponentInChildren<TMP_Text>(true);
        Color normal = label.color;
        OptionsPanel().Find("LanguageButton").GetComponent<HoverTint>().OnPointerEnter(null);

        Find<Button>("backButton (1)").onClick.Invoke();
        yield return null;

        Assert.IsFalse(OptionsPanel().gameObject.activeSelf);
        Assert.AreEqual(normal, label.color);
    }

    [UnityTest]
    public IEnumerator ResetSettingsIsWhereClearResultsIsInRecords()
    {
        yield return LoadScene("MenuScene");
        RectTransform reset = (RectTransform)OptionsPanel().Find("ResetSettingsButton");
        RectTransform clear = (RectTransform)Find<RectTransform>("RecordsPanel").Find("ClearButton");

        Assert.AreEqual(clear.position.x, reset.position.x, 0.5f);
        Assert.AreEqual(clear.position.y, reset.position.y, 0.5f, "Just above BACK, like Clear results");
        Assert.AreEqual(clear.GetComponentInChildren<TMP_Text>(true).fontSize, reset.GetComponentInChildren<TMP_Text>(true).fontSize);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ResetSettingsHalvesTheVolumesAndRestoresNormalAndTheNativeResolutionButKeepsTheLanguage()
    {
        SoundController sound = null;
        float savedSound = 0f, savedMusic = 0f;
        AiDifficulty savedDifficulty = GameSession.Difficulty;
        Vector2Int savedResolution = DisplaySettings.Current;
        Language savedLanguage = Loc.Current;
        yield return OpenOptions();
        sound = SoundController.Instance;
        savedSound = sound.SoundVolume;
        savedMusic = sound.MusicVolume;
        try
        {
            Slider effects = OptionsPanel().Find("EffectsSlider").GetComponent<Slider>();
            Slider music = OptionsPanel().Find("MusicSlider").GetComponent<Slider>();
            effects.value = 0.9f;
            music.value = 0.1f;
            GameSession.Difficulty = AiDifficulty.Hard;
            var resolutions = DisplaySettings.Available();
            if (resolutions.Count > 1) DisplaySettings.Set(resolutions[1]);
            Loc.Current = Language.Polish;

            Find<Button>("ResetSettingsButton").onClick.Invoke();

            Assert.AreEqual(0.5f, effects.value, 0.001f, "Effects halfway");
            Assert.AreEqual(0.5f, music.value, 0.001f, "Music halfway");
            Assert.AreEqual(0.5f, sound.SoundVolume, 0.001f);
            Assert.AreEqual(0.5f, sound.MusicVolume, 0.001f);
            Assert.AreEqual(AiDifficulty.Normal, GameSession.Difficulty);
            Assert.AreEqual(DisplaySettings.Native, DisplaySettings.Current, "The native resolution is back");
            Assert.AreEqual(Language.Polish, Loc.Current, "The language is not touched");
            StringAssert.Contains(Loc.T("Normal"), OptionsPanel().Find("DifficultyButton").GetComponentInChildren<TMP_Text>(true).text, "The label follows");
        }
        finally
        {
            sound.SoundVolume = savedSound;
            sound.MusicVolume = savedMusic;
            GameSession.Difficulty = savedDifficulty;
            DisplaySettings.Set(savedResolution);
            Loc.Current = savedLanguage;
        }
    }
}
