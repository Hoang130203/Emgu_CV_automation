using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameAutomation.Core.Models.Configuration;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using static GameAutomation.Core.Workflows.Actions.AutomationActions;

namespace GameAutomation.UI.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly VisionService _visionService;
    private readonly string _screenshotsFolder;
    private readonly string _templatesFolder;
    private const int MaxScreenshots = 3;
    private CancellationTokenSource? _botCancellationTokenSource;

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
    private string _logs = "";

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

    [ObservableProperty]
    private BitmapImage? _screenPreview;

    public ObservableCollection<WorkflowViewModel> Workflows { get; } = new();

    public MainViewModel()
    {
        _visionService = new VisionService();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _screenshotsFolder = Path.Combine(baseDir, "Screenshots");
        _templatesFolder = Path.Combine(baseDir, "Templates");

        if (!Directory.Exists(_screenshotsFolder))
            Directory.CreateDirectory(_screenshotsFolder);
        if (!Directory.Exists(_templatesFolder))
            Directory.CreateDirectory(_templatesFolder);

        // Add workflows - truyền reference để gọi command
        Workflows.Add(new WorkflowViewModel("Open Excel Flow", this));
        Workflows.Add(new WorkflowViewModel("Auto-Farm Workflow", this));
        Workflows.Add(new WorkflowViewModel("Combat Sequence", this));

        AddLog("Bot initialized.");
        AddLog("Waiting for user input...");
    }

    [RelayCommand]
    private void StartBot()
    {
        if (!IsBotRunning)
        {
            IsBotRunning = true;
            BotStatus = "Running";
            _botCancellationTokenSource = new CancellationTokenSource();
            AddLog("Bot started successfully.");
        }
    }

    [RelayCommand]
    private void StopBot()
    {
        if (IsBotRunning)
        {
            _botCancellationTokenSource?.Cancel();
            _botCancellationTokenSource?.Dispose();
            _botCancellationTokenSource = null;
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
            Directory.CreateDirectory(assetsPath);

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
        try
        {
            Process.Start("explorer.exe", _templatesFolder);
            AddLog($"Opened templates folder: {_templatesFolder}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open templates folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs = string.Empty;
        AddLog("Logs cleared.");
    }

    /// <summary>
    /// Chạy workflow theo tên - được gọi từ nút Play trong DataGrid
    /// </summary>
    [RelayCommand]
    private async Task RunWorkflowAsync(WorkflowViewModel? workflow)
    {
        if (workflow == null) return;

        // Auto start bot nếu chưa chạy
        if (!IsBotRunning)
        {
            StartBot();
        }

        switch (workflow.Name)
        {
            case "Open Excel Flow":
                await RunOpenExcelFlowAsync(workflow);
                break;
            case "Auto-Farm Workflow":
                AddLog($"[{workflow.Name}] Not implemented yet.");
                break;
            case "Combat Sequence":
                AddLog($"[{workflow.Name}] Not implemented yet.");
                break;
            default:
                AddLog($"Unknown workflow: {workflow.Name}");
                break;
        }
    }

    /// <summary>
    /// Capture màn hình nội bộ - dùng trong flow, tự động cleanup ảnh cũ
    /// </summary>
    private Bitmap CaptureScreenInternal()
    {
        var screenshot = _visionService.CaptureScreen();

        // Save với timestamp
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var filename = $"screenshot_{timestamp}.png";
        var filepath = Path.Combine(_screenshotsFolder, filename);
        screenshot.Save(filepath, ImageFormat.Png);

        // Cleanup ảnh cũ - giữ lại MaxScreenshots
        CleanupOldScreenshots();

        // Update preview trên UI
        UpdateScreenPreview(filepath);

        return screenshot;
    }

    private void CleanupOldScreenshots()
    {
        try
        {
            var screenshots = Directory.GetFiles(_screenshotsFolder, "screenshot_*.png")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (screenshots.Count > MaxScreenshots)
            {
                var toDelete = screenshots.Skip(MaxScreenshots).ToList();
                foreach (var file in toDelete)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore if file is in use
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void UpdateScreenPreview(string imagePath)
    {
        try
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                bitmap.Freeze();
                ScreenPreview = bitmap;
            });
        }
        catch
        {
            // Ignore preview errors
        }
    }

    /// <summary>
    /// Flow mẫu: Mở Excel - chạy trên background thread để không block UI
    /// </summary>
    private async Task RunOpenExcelFlowAsync(WorkflowViewModel workflow)
    {
        if (_botCancellationTokenSource == null || _botCancellationTokenSource.IsCancellationRequested)
        {
            AddLog("[Open Excel Flow] Bot not running.");
            return;
        }

        workflow.Status = "Running";
        var token = _botCancellationTokenSource.Token;
        var excelInitPath = Path.Combine(_templatesFolder, "Excel_Init.png");
        // Test 1: Screen Capture
        Console.WriteLine("[1] Testing Screen Capture...");
        try
        {
            using var screenshot = CaptureScreen();
            var screenshotPath = Path.Combine(Environment.CurrentDirectory, "screenshot.png");
            screenshot.Save(screenshotPath, ImageFormat.Png);
            Console.WriteLine($"    Screenshot saved: {screenshotPath}");
            Console.WriteLine($"    Size: {screenshot.Width}x{screenshot.Height}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Error: {ex.Message}");
        }

        // Test 2: Template Matching + Human-like Click
        Console.WriteLine("\n[2] Testing Find & Click (Human-like)...");
        var templatePath = Path.Combine(_templatesFolder, "template.png");

        if (File.Exists(templatePath))
        {
            try
            {
                // Sử dụng human-like movement: Bezier curve + ease-in-out
                var result = await FindAndClickHumanAsync(templatePath, threshold: 0.8);

                if (result != null)
                {
                    Console.WriteLine($"    Found and clicked at: ({result.X + result.Width / 2}, {result.Y + result.Height / 2})");
                    Console.WriteLine($"    Confidence: {result.Confidence:P1}");
                }
                else
                {
                    Console.WriteLine("    Template not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"    Skipped - Create '{templatePath}' to test.");
        }

        try
        {
            AddLog("[Open Excel Flow] Starting...");

            // Chạy toàn bộ flow trên background thread
            await Task.Run(async () =>
            {
                // Step 1: Wait a bit
                AddLog("    Preparing...");
                await ThinkAsync(300, 500);
                IncrementAction();

                if (token.IsCancellationRequested) return;

                // Step 2: Type "Excel"
                AddLog("    Typing 'Excel'...");
                await TypeHumanAsync("Excel");
                IncrementAction();

                if (token.IsCancellationRequested) return;

                // Step 3: Wait and press Enter
                await ThinkAsync(200, 400);
                AddLog("    Pressing Enter...");
                Input.KeyPress(VirtualKeyCode.RETURN);
                IncrementAction();

                if (token.IsCancellationRequested) return;

                // Step 4: Wait for Excel to appear
                AddLog("    Waiting for Excel to open (max 15s)...");
                var excelResult = WaitForTemplate(excelInitPath, threshold: 0.7, timeoutMs: 15000, checkIntervalMs: 500);

                // Capture và update preview
                CaptureAndUpdatePreview();
                IncrementDetection();

                if (excelResult != null)
                {
                    AddLog($"    Excel found! Confidence: {excelResult.Confidence:P1}");

                    // Step 5: Click on Excel
                    AddLog("    Clicking on Excel...");
                    await FindAndClickHumanAsync(excelInitPath, threshold: 0.7);
                    IncrementAction();

                    AddLog("[Open Excel Flow] Completed!");
                    UpdateWorkflowStatus(workflow, "Ready", incrementExecution: true);

                    // Handle License Popup
                    await HandleExcelLicensePopupInternalAsync(token);
                }
                else
                {
                    AddLog("    Excel not found (timeout).");
                    AddLog($"    Template needed: {excelInitPath}");
                    UpdateWorkflowStatus(workflow, "Failed");
                }
            }, token);
        }
        catch (OperationCanceledException)
        {
            AddLog("[Open Excel Flow] Cancelled.");
            workflow.Status = "Cancelled";
        }
        catch (Exception ex)
        {
            AddLog($"[Open Excel Flow] Error: {ex.Message}");
            workflow.Status = "Error";
        }
    }

    private async Task HandleExcelLicensePopupInternalAsync(CancellationToken token)
    {
        var licensePopupPath = Path.Combine(_templatesFolder, "Excel_license_popup.png");
        var licenseClosePath = Path.Combine(_templatesFolder, "Excel_license_close.png");

        if (!File.Exists(licensePopupPath) || !File.Exists(licenseClosePath))
            return;

        try
        {
            AddLog("    Checking for license popup...");
            var popupResult = WaitForTemplate(licensePopupPath, threshold: 0.7, timeoutMs: 5000, checkIntervalMs: 300);

            if (popupResult != null && !token.IsCancellationRequested)
            {
                AddLog("    License popup found! Closing...");
                await ThinkAsync(200, 400);

                var closeResult = await FindAndClickHumanAsync(licenseClosePath, threshold: 0.7);
                if (closeResult != null)
                {
                    AddLog("    License popup closed.");
                    IncrementAction();
                }
            }
        }
        catch
        {
            // Ignore
        }
    }

    // Helper methods để update UI từ background thread
    private void IncrementAction()
    {
        Application.Current?.Dispatcher.Invoke(() => ActionCount++);
    }

    private void IncrementDetection()
    {
        Application.Current?.Dispatcher.Invoke(() => DetectionCount++);
    }

    private void UpdateWorkflowStatus(WorkflowViewModel workflow, string status, bool incrementExecution = false)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            workflow.Status = status;
            if (incrementExecution) workflow.ExecutionCount++;
        });
    }

    private void CaptureAndUpdatePreview()
    {
        try
        {
            using var screenshot = _visionService.CaptureScreen();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var filename = $"screenshot_{timestamp}.png";
            var filepath = Path.Combine(_screenshotsFolder, filename);
            screenshot.Save(filepath, ImageFormat.Png);
            CleanupOldScreenshots();
            UpdateScreenPreview(filepath);
        }
        catch { }
    }

    private async Task HandleExcelLicensePopupAsync(CancellationToken token)
    {
        var licensePopupPath = Path.Combine(_templatesFolder, "Excel_license_popup.png");
        var licenseClosePath = Path.Combine(_templatesFolder, "Excel_license_close.png");

        if (!File.Exists(licensePopupPath) || !File.Exists(licenseClosePath))
            return;

        try
        {
            AddLog("    Checking for license popup...");
            var popupResult = WaitForTemplate(licensePopupPath, threshold: 0.7, timeoutMs: 5000, checkIntervalMs: 300);

            if (popupResult != null && !token.IsCancellationRequested)
            {
                AddLog("    License popup found! Closing...");
                await ThinkAsync(200, 400);

                var closeResult = await FindAndClickHumanAsync(licenseClosePath, threshold: 0.7);
                if (closeResult != null)
                {
                    AddLog("    License popup closed.");
                    ActionCount++;
                }
            }
        }
        catch
        {
            // Ignore
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs += (string.IsNullOrEmpty(Logs) ? "" : "\n") + logEntry;
        });
    }
}

public partial class WorkflowViewModel : ObservableObject
{
    private readonly MainViewModel? _mainViewModel;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private int _executionCount = 0;

    public WorkflowViewModel()
    {
    }

    public WorkflowViewModel(string name, MainViewModel mainViewModel)
    {
        _name = name;
        _mainViewModel = mainViewModel;
    }

    /// <summary>
    /// Command cho nút Play - gọi MainViewModel.RunWorkflowAsync
    /// </summary>
    [RelayCommand]
    private async Task Play()
    {
        if (_mainViewModel != null)
        {
            await _mainViewModel.RunWorkflowCommand.ExecuteAsync(this);
        }
    }
}
