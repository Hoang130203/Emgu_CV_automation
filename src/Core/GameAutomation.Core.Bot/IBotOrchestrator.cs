using GameAutomation.Core.Models.Configuration;
using GameAutomation.Core.Models.GameState;

namespace GameAutomation.Core.Bot;

/// <summary>
/// Main orchestrator for the game automation bot
/// </summary>
public interface IBotOrchestrator
{
    /// <summary>
    /// Starts the bot with the given configuration
    /// </summary>
    Task StartAsync(BotConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the bot
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets the current game context
    /// </summary>
    GameContext GetCurrentContext();

    /// <summary>
    /// Event triggered when the game state changes
    /// </summary>
    event EventHandler<GameContext>? StateChanged;
}
