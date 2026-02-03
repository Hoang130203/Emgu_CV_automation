using System.Text.Json;
using GameAutomation.Core.Models.Configuration;
using GameAutomation.Core.Models.Vision;

namespace GameAutomation.Core.Services.Configuration;

/// <summary>
/// Service for loading, saving, and managing custom region configurations
/// </summary>
public class RegionConfigService
{
    private readonly string _configPath;
    private readonly object _lock = new();
    private RegionConfig _config = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RegionConfigService(string? configPath = null)
    {
        _configPath = configPath ?? GetDefaultConfigPath();
    }

    /// <summary>
    /// Event raised when configuration changes
    /// </summary>
    public event EventHandler? ConfigChanged;

    /// <summary>
    /// Get default config path (alongside exe in config folder)
    /// </summary>
    private static string GetDefaultConfigPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "config");
        Directory.CreateDirectory(configDir);
        return Path.Combine(configDir, "region-config.json");
    }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<RegionConfig>(json, JsonOptions);
                if (config != null)
                {
                    lock (_lock)
                    {
                        _config = config;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegionConfigService] Error loading config: {ex.Message}");
        }
    }

    /// <summary>
    /// Save configuration to file
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            RegionConfig configCopy;
            lock (_lock)
            {
                _config.LastModified = DateTime.UtcNow;
                configCopy = _config;
            }

            var json = JsonSerializer.Serialize(configCopy, JsonOptions);
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(_configPath, json);
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegionConfigService] Error saving config: {ex.Message}");
        }
    }

    /// <summary>
    /// Import configuration from external file
    /// </summary>
    public async Task<bool> ImportAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            var json = await File.ReadAllTextAsync(path);
            var importedConfig = JsonSerializer.Deserialize<RegionConfig>(json, JsonOptions);
            if (importedConfig == null)
                return false;

            lock (_lock)
            {
                // Merge imported regions into current config
                foreach (var kvp in importedConfig.Regions)
                {
                    _config.Regions[kvp.Key] = kvp.Value;
                }
            }

            await SaveAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegionConfigService] Error importing: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Export configuration to file
    /// </summary>
    public async Task<bool> ExportAsync(string path)
    {
        try
        {
            RegionConfig configCopy;
            lock (_lock)
            {
                configCopy = _config;
            }

            var json = JsonSerializer.Serialize(configCopy, JsonOptions);
            await File.WriteAllTextAsync(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegionConfigService] Error exporting: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Try to get region for a template key
    /// </summary>
    public bool TryGetRegion(string key, out SearchRegion? region)
    {
        region = null;
        lock (_lock)
        {
            if (_config.Regions.TryGetValue(key, out var entry) && entry.Enabled)
            {
                region = entry.ToSearchRegion();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get region entry for a template key (includes metadata)
    /// </summary>
    public RegionEntry? GetRegionEntry(string key)
    {
        lock (_lock)
        {
            return _config.Regions.TryGetValue(key, out var entry) ? entry : null;
        }
    }

    /// <summary>
    /// Set or update region for a template key
    /// </summary>
    public void SetRegion(string key, SearchRegion region, string? notes = null)
    {
        lock (_lock)
        {
            _config.Regions[key] = RegionEntry.FromSearchRegion(region, notes);
        }
    }

    /// <summary>
    /// Remove custom region (will fall back to hardcoded)
    /// </summary>
    public bool RemoveRegion(string key)
    {
        lock (_lock)
        {
            return _config.Regions.Remove(key);
        }
    }

    /// <summary>
    /// Check if a custom region exists for key
    /// </summary>
    public bool HasCustomRegion(string key)
    {
        lock (_lock)
        {
            return _config.Regions.ContainsKey(key);
        }
    }

    /// <summary>
    /// Get all custom region keys
    /// </summary>
    public IReadOnlyList<string> GetAllKeys()
    {
        lock (_lock)
        {
            return _config.Regions.Keys.ToList();
        }
    }

    /// <summary>
    /// Get config file path
    /// </summary>
    public string ConfigPath => _configPath;
}
