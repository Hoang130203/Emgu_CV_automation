using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using System.Collections.Concurrent;

namespace GameAutomation.Core.Workflows.Visual;

/// <summary>
/// Default implementation of IActivityContext.
/// Wraps existing services and provides variable storage.
/// </summary>
public class ActivityContext : IActivityContext
{
    private readonly Action<string>? _logAction;
    private readonly Action<string>? _logErrorAction;
    private readonly ConcurrentDictionary<string, object?> _variables;

    public ActivityContext(
        IVisionService visionService,
        IInputService inputService,
        CancellationToken cancellationToken = default,
        Action<string>? logAction = null,
        Action<string>? logErrorAction = null)
    {
        Vision = visionService ?? throw new ArgumentNullException(nameof(visionService));
        Input = inputService ?? throw new ArgumentNullException(nameof(inputService));
        CancellationToken = cancellationToken;
        _logAction = logAction;
        _logErrorAction = logErrorAction;
        _variables = new ConcurrentDictionary<string, object?>();
    }

    public IVisionService Vision { get; }
    public IInputService Input { get; }
    public CancellationToken CancellationToken { get; }

    public IDictionary<string, object?> Variables => _variables;

    public void Log(string message)
    {
        _logAction?.Invoke(message);
    }

    public void LogError(string message)
    {
        _logErrorAction?.Invoke(message);
    }

    /// <summary>
    /// Get typed variable with default value
    /// </summary>
    public T GetVariable<T>(string key, T defaultValue = default!)
    {
        if (_variables.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Set variable value
    /// </summary>
    public void SetVariable(string key, object? value)
    {
        _variables[key] = value;
    }
}
