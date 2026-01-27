using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameAutomation.Core.Models.Configuration;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GameAutomation.UI.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _botStatus = "Stopped";

    [ObservableProperty]
    private bool _isBotRunning = false;

    [ObservableProperty]
    private string _gameWindowTitle = string.Empty;

    [ObservableProperty]
    private AIProvider _selectedAIProvider = AIProvider.None;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _logs = "[2026-01-27 14:30:00] Bot initialized.\n[2026-01-27 14:30:01] Waiting for user input...";

    [ObservableProperty]
    private string _runtime = "00:00:00";

    [ObservableProperty]
    private int _actionCount = 0;

    [ObservableProperty]
    private int _detectionCount = 0;

    [ObservableProperty]
    private int _fps = 0;

    [ObservableProperty]
    private double _memoryUsage = 0;

    public ObservableCollection<WorkflowViewModel> Workflows { get; } = new();

    public MainViewModel()
    {
        // Add sample workflows
        Workflows.Add(new WorkflowViewModel { Name = "Auto-Farm Workflow", Status = "Ready", ExecutionCount = 0 });
        Workflows.Add(new WorkflowViewModel { Name = "Combat Sequence", Status = "Ready", ExecutionCount = 0 });
    }

    [RelayCommand]
    private void StartBot()
    {
        if (!IsBotRunning)
        {
            IsBotRunning = true;
            BotStatus = "Running";
            AddLog("Bot started successfully.");
        }
    }

    [RelayCommand]
    private void StopBot()
    {
        if (IsBotRunning)
        {
            IsBotRunning = false;
            BotStatus = "Stopped";
            AddLog("Bot stopped.");
        }
    }

    [RelayCommand]
    private void OpenAssetsFolder()
    {
        var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets");

        if (!Directory.Exists(assetsPath))
        {
            Directory.CreateDirectory(assetsPath);
        }

        try
        {
            Process.Start("explorer.exe", assetsPath);
            AddLog($"Opened assets folder: {assetsPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open assets folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void UploadTemplates()
    {
        AddLog("Template upload feature not yet implemented.");
        MessageBox.Show("Please manually place template images in the Assets/GameTemplates folder.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void CaptureScreenshot()
    {
        AddLog("Screenshot capture feature not yet implemented.");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs = string.Empty;
        AddLog("Logs cleared.");
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Logs += $"\n[{timestamp}] {message}";
    }
}

public partial class WorkflowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private int _executionCount = 0;
}
