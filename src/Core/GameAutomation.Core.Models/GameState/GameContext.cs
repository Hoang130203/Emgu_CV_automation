namespace GameAutomation.Core.Models.GameState;

/// <summary>
/// Represents the current state of the game
/// </summary>
public class GameContext
{
    public string GameName { get; set; } = string.Empty;
    public GameStatus Status { get; set; }
    public Dictionary<string, object> StateData { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public enum GameStatus
{
    Unknown,
    MainMenu,
    InGame,
    Loading,
    Paused,
    GameOver
}
