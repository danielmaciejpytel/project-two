using NUnit.Framework;

// The tests read texts of the game in English. The language is a saved setting of the player, so for the length of the run it is
// English, and the player's own choice is put back afterwards.
[SetUpFixture]
public class PlayModeEnvironment
{
    private Language _savedLanguage;

    [OneTimeSetUp]
    public void UseEnglish()
    {
        _savedLanguage = Loc.Current;
        Loc.Current = Language.English;
    }

    [OneTimeTearDown]
    public void PutTheLanguageBack()
    {
        Loc.Current = _savedLanguage;
    }
}
