// Settings chosen in the menu that the game scene reads.
public static class GameSession
{
    public const int NoAi = 0;
    public const int DefaultAiPlayer = 2;

    // Player controlled by the computer (1 = Super Hot, 2 = Super Cold), NoAi for hot-seat.
    public static int AiPlayerId { get; set; } = NoAi;

    public static bool IsAiPlayer(int playerId) => AiPlayerId != NoAi && AiPlayerId == playerId;
}
