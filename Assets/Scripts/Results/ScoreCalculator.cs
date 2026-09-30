using UnityEngine;

// The score of one team at the end of a game. Kept apart from everything else so the formula is easy to change and to test.
public static class ScoreCalculator
{
    public const int WinPoints = 1000;
    public const int KillPoints = 100;
    public const int LostUnitPenalty = 50;
    // Bonus for a quick win: points for every turn (of both players together) under the limit.
    public const int SpeedTurnLimit = 30;
    public const int SpeedPointsPerTurn = 20;
    // Bonus for the health the winner's commander has left.
    public const int HealthPointsPerHealth = 30;

    // Against the computer the score follows the difficulty; a game of two players counts as Normal.
    public static float DifficultyMultiplier(AiDifficulty difficulty)
    {
        switch (difficulty)
        {
            case AiDifficulty.Easy: return 0.5f;
            case AiDifficulty.Hard: return 1.5f;
            default: return 1.0f;
        }
    }

    public static int Score(bool won, int kills, int lostUnits, int turns, int commanderHealth, float multiplier)
    {
        float score = kills * KillPoints - lostUnits * LostUnitPenalty;
        if (won)
        {
            score += WinPoints;
            score += Mathf.Max(0, SpeedTurnLimit - turns) * SpeedPointsPerTurn;
            score += Mathf.Max(0, commanderHealth) * HealthPointsPerHealth;
        }
        return Mathf.Max(0, Mathf.RoundToInt(score * multiplier));
    }
}
