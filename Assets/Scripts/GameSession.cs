using UnityEngine;

public enum AiDifficulty { Easy, Normal, Hard }

// Settings chosen in the menu that the game scene reads.
public static class GameSession
{
    private const string DifficultyPrefKey = "Options.AiDifficulty";

    public const int NoAi = 0;
    public const int DefaultAiPlayer = 2;

    // Player controlled by the computer (1 = Red Boss, 2 = Blue Boss), NoAi for hot-seat.
    public static int AiPlayerId { get; set; } = NoAi;

    // Set by Back in the unit draft: the menu opens on the game mode choice instead of the main buttons.
    public static bool OpenPlayMenu { get; set; }

    public static bool IsAiPlayer(int playerId) => AiPlayerId != NoAi && AiPlayerId == playerId;

    // Rematch with the sides swapped: the computer takes the other team. Nothing changes in a two-player game.
    public static void SwapSides()
    {
        if (AiPlayerId != NoAi) AiPlayerId = AiPlayerId == 1 ? 2 : 1;
    }

    // Chosen in Options, remembered between sessions.
    public static AiDifficulty Difficulty
    {
        get => (AiDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, (int)AiDifficulty.Normal), 0, 2);
        set
        {
            PlayerPrefs.SetInt(DifficultyPrefKey, (int)value);
            PlayerPrefs.Save();
        }
    }
}
