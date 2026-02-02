using GameAutomation.Core.Workflows.Visual.Activities;

namespace GameAutomation.Core.Workflows.Visual;

/// <summary>
/// Registry of all available activity types.
/// Used by the executor to find activities and by the UI for the toolbox.
/// </summary>
public class ActivityRegistry
{
    private readonly Dictionary<string, IActivity> _activities = new();

    public ActivityRegistry()
    {
        // Register built-in activities
        RegisterBuiltInActivities();
    }

    /// <summary>
    /// Get all registered activities
    /// </summary>
    public IEnumerable<IActivity> GetAll() => _activities.Values;

    /// <summary>
    /// Get activities by category
    /// </summary>
    public IEnumerable<IActivity> GetByCategory(string category) =>
        _activities.Values.Where(a => a.Category == category);

    /// <summary>
    /// Get all unique categories
    /// </summary>
    public IEnumerable<string> GetCategories() =>
        _activities.Values.Select(a => a.Category).Distinct().OrderBy(c => c);

    /// <summary>
    /// Get activity by type name
    /// </summary>
    public IActivity? Get(string type) =>
        _activities.TryGetValue(type, out var activity) ? activity : null;

    /// <summary>
    /// Register a custom activity
    /// </summary>
    public void Register(IActivity activity)
    {
        _activities[activity.Type] = activity;
    }

    private void RegisterBuiltInActivities()
    {
        Register(new StartActivity());
        Register(new EndActivity());
        Register(new FindTemplateActivity());
        Register(new ClickActivity());
        Register(new TypeTextActivity());
        Register(new WaitActivity());
        Register(new ConditionActivity());
    }
}
