using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameAutomation.Core.Models.Configuration;
using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Workflows.Examples;
using GameAutomation.UI.WPF;
using GameAutomation.UI.WPF.Dialogs;
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
    private readonly BotConfiguration _configuration;

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
        _configuration = new BotConfiguration();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _screenshotsFolder = Path.Combine(baseDir, "Screenshots");
        _templatesFolder = Path.Combine(baseDir, "Templates");

        if (!Directory.Exists(_screenshotsFolder))
            Directory.CreateDirectory(_screenshotsFolder);
        if (!Directory.Exists(_templatesFolder))
            Directory.CreateDirectory(_templatesFolder);

        // Add workflows - truyền reference để gọi command
        Workflows.Add(new WorkflowViewModel("Open Excel Flow", this));
        Workflows.Add(new WorkflowViewModel("NTH Sign-in Flow", this));
        Workflows.Add(new WorkflowViewModel("NTH Lv26-44 Camera", this));
        Workflows.Add(new WorkflowViewModel("NTH Lv26-44 MHL", this));
        Workflows.Add(new WorkflowViewModel("Auto-Farm Workflow", this));
        Workflows.Add(new WorkflowViewModel("Combat Sequence", this));

        AddLog("Bot initialized.");
        AddLog("Waiting for user input...");
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_configuration)
        {
            Owner = Application.Current.MainWindow
        };

        if (settingsWindow.ShowDialog() == true)
        {
            AddLog("Settings saved successfully.");
            // Apply settings to current session
            ApplySettings();
        }
    }

    private void ApplySettings()
    {
        // Update AI provider display
        SelectedAIProvider = _configuration.AIProvider;
        ApiKey = _configuration.AIApiKey ?? string.Empty;
        GameWindowTitle = _configuration.GameWindowTitle;

        // Apply feature matching settings to AutomationActions
        ConfigureFeatureMatching(
            _configuration.UseFeatureMatching,
            _configuration.FeatureAlgorithm,
            _configuration.MinMatchCount,
            _configuration.FeatureMatchRatio);

        AddLog($"Feature Matching: {(_configuration.UseFeatureMatching ? "Enabled" : "Disabled")}");
        if (_configuration.UseFeatureMatching)
        {
            AddLog($"  Algorithm: {_configuration.FeatureAlgorithm}");
            AddLog($"  Min Matches: {_configuration.MinMatchCount}");
            AddLog($"  Ratio Threshold: {_configuration.FeatureMatchRatio:F2}");
        }
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
            case "NTH Sign-in Flow":
                await RunNthSigninFlowAsync(workflow);
                break;
            case "NTH Lv26-44 Camera":
                await RunNthCameraFlowAsync(workflow);
                break;
            case "NTH Lv26-44 MHL":
                await RunNthMongHoaLucFlowAsync(workflow);
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
    /// Step-by-step: Phải hoàn thành bước trước mới làm bước sau
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

        // Template paths
        var searchIconPath = Path.Combine(_templatesFolder, "template.png");  // Search icon
        var excelInitPath = Path.Combine(_templatesFolder, "Excel_Init.png");

        try
        {
            AddLog("[Open Excel Flow] Starting...");

            // Chạy toàn bộ flow trên background thread
            await Task.Run(async () =>
            {
                // ============ STEP 1: Tìm và click Search Icon ============
                AddLog("  [Step 1] Finding search icon...");
                CaptureAndUpdatePreview();

                if (!File.Exists(searchIconPath))
                {
                    AddLog($"    ERROR: Search icon template not found: {searchIconPath}");
                    UpdateWorkflowStatus(workflow, "Failed");
                    return;
                }

                // Dùng Multi-Scale để tìm icon ở nhiều kích thước
                var searchResult = FindTemplateMultiScale(
                    searchIconPath,
                    threshold: 0.7,
                    minScale: 0.5,
                    maxScale: 1.5,
                    scaleSteps: 10);

                if (searchResult == null || searchResult.Count == 0)
                {
                    AddLog("    Search icon NOT FOUND. Trying with lower threshold...");

                    //// Thử lại với threshold thấp hơn
                    searchResult = FindTemplateMultiScale(
                        searchIconPath,
                        threshold: 0.6,
                        minScale: 0.3,
                        maxScale: 2.0,
                        scaleSteps: 15);
                }

                if (searchResult == null || searchResult.Count == 0)
                {
                    AddLog("    ERROR: Search icon not found after retries.");
                    AddLog($"    Make sure '{searchIconPath}' exists and matches the icon on screen.");
                    UpdateWorkflowStatus(workflow, "Failed");
                    return;
                }

                var bestMatch = searchResult[0];
                AddLog($"    Found search icon! Confidence: {bestMatch.Confidence:P1} at ({bestMatch.X}, {bestMatch.Y})");
                IncrementDetection();

                if (token.IsCancellationRequested) return;

                // Click vào search icon
                AddLog("    Clicking search icon...");
                int centerX = bestMatch.X + bestMatch.Width / 2;
                int centerY = bestMatch.Y + bestMatch.Height / 2;
                await ClickAtHumanAsync(centerX, centerY);
                IncrementAction();

                // Chờ một chút để search box mở
                await ThinkAsync(500, 800);

                if (token.IsCancellationRequested) return;

                // ============ STEP 2: Gõ "Excel" ============
                AddLog("  [Step 2] Typing 'Excel'...");
                await TypeHumanAsync("Excel");
                IncrementAction();

                if (token.IsCancellationRequested) return;

                // ============ STEP 3: Nhấn Enter ============
                await ThinkAsync(300, 500);
                AddLog("  [Step 3] Pressing Enter...");
                Input.KeyPress(VirtualKeyCode.RETURN);
                IncrementAction();

                if (token.IsCancellationRequested) return;

                // ============ STEP 4: Chờ Excel xuất hiện ============
                AddLog("  [Step 4] Waiting for Excel to open (max 15s)...");
                var excelResult = WaitForTemplate(excelInitPath, threshold: 0.7, timeoutMs: 15000, checkIntervalMs: 500);

                CaptureAndUpdatePreview();
                IncrementDetection();

                if (excelResult != null)
                {
                    AddLog($"    Excel found! Confidence: {excelResult.Confidence:P1}");

                    // ============ STEP 5: Click vào Excel ============
                    AddLog("  [Step 5] Clicking on Excel...");
                    await FindAndClickHumanAsync(excelInitPath, threshold: 0.7);
                    IncrementAction();

                    AddLog("[Open Excel Flow] Completed successfully!");
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

    /// <summary>
    /// Flow NTH Sign-in: Đăng nhập game với thông tin từ Excel
    /// Hiển thị dialog để người dùng nhập sheet và dòng bắt đầu
    /// </summary>
    private async Task RunNthSigninFlowAsync(WorkflowViewModel workflow)
    {
        if (_botCancellationTokenSource == null || _botCancellationTokenSource.IsCancellationRequested)
        {
            AddLog("[NTH Sign-in] Bot not running.");
            return;
        }

        // Show input dialog on UI thread
        string? sheetName = null;
        int startRow = 1;
        bool dialogResult = false;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new ExcelInputDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                sheetName = dialog.SheetName;
                startRow = dialog.StartRow;
                dialogResult = true;
            }
        });

        if (!dialogResult)
        {
            AddLog("[NTH Sign-in] Cancelled by user.");
            return;
        }

        // Minimize app to allow screen capture of game
        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        });

        // Wait a bit for window to minimize
        await Task.Delay(500);

        workflow.Status = "Running";
        var token = _botCancellationTokenSource.Token;

        try
        {
            AddLog($"[NTH Sign-in] Starting from row {startRow}" + (sheetName != null ? $", sheet: {sheetName}" : ""));

            // Run workflow on background thread
            await Task.Run(async () =>
            {
                var inputService = new InputService();
                var nthWorkflow = new NthSigninWorkflow(
                    _visionService,
                    inputService,
                    assetsPath: Path.Combine(_templatesFolder, "nth", "signin"),
                    logger: msg => AddLog(msg));

                var context = new GameContext { GameName = "NTH Game" };
                var result = await nthWorkflow.ExecuteAsync(context, token, sheetName, startRow);

                if (result.Success)
                {
                    AddLog($"[NTH Sign-in] Completed successfully!");
                    UpdateWorkflowStatus(workflow, "Ready", incrementExecution: true);
                }
                else
                {
                    AddLog($"[NTH Sign-in] Failed: {result.Message}");
                    UpdateWorkflowStatus(workflow, "Failed");
                }

                // Restore window after completion
                RestoreMainWindow();
            }, token);
        }
        catch (OperationCanceledException)
        {
            AddLog("[NTH Sign-in] Cancelled.");
            workflow.Status = "Cancelled";
            RestoreMainWindow();
        }
        catch (Exception ex)
        {
            AddLog($"[NTH Sign-in] Error: {ex.Message}");
            workflow.Status = "Error";
            RestoreMainWindow();
        }
    }

    /// <summary>
    /// Flow NTH Camera: Quy trình camera cho game từ level 26-44
    /// </summary>
    private async Task RunNthCameraFlowAsync(WorkflowViewModel workflow)
    {
        if (_botCancellationTokenSource == null || _botCancellationTokenSource.IsCancellationRequested)
        {
            AddLog("[NTH Camera] Bot not running.");
            return;
        }

        // Minimize app to allow screen capture of game
        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        });

        // Wait a bit for window to minimize
        await Task.Delay(500);

        workflow.Status = "Running";
        var token = _botCancellationTokenSource.Token;

        try
        {
            AddLog("[NTH Camera] Starting Step 2: Camera workflow...");

            // Run workflow on background thread
            await Task.Run(async () =>
            {
                var inputService = new InputService();
                var cameraWorkflow = new NthLv26To44CameraWorkflow(
                    _visionService,
                    inputService,
                    assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_camera"),
                    logger: msg => AddLog(msg));

                var context = new GameContext { GameName = "NTH Game" };
                var result = await cameraWorkflow.ExecuteAsync(context, token);

                if (result.Success)
                {
                    AddLog($"[NTH Camera] Completed successfully!");
                    UpdateWorkflowStatus(workflow, "Ready", incrementExecution: true);
                }
                else
                {
                    AddLog($"[NTH Camera] Failed: {result.Message}");
                    UpdateWorkflowStatus(workflow, "Failed");
                }

                // Restore window after completion
                RestoreMainWindow();
            }, token);
        }
        catch (OperationCanceledException)
        {
            AddLog("[NTH Camera] Cancelled.");
            workflow.Status = "Cancelled";
            RestoreMainWindow();
        }
        catch (Exception ex)
        {
            AddLog($"[NTH Camera] Error: {ex.Message}");
            workflow.Status = "Error";
            RestoreMainWindow();
        }
    }

    /// <summary>
    /// Flow NTH Mộng Hoa Lục: Quy trình bước 3 cho game từ level 26-44
    /// Mở mailbox -> Like posts -> Exit
    /// </summary>
    private async Task RunNthMongHoaLucFlowAsync(WorkflowViewModel workflow)
    {
        if (_botCancellationTokenSource == null || _botCancellationTokenSource.IsCancellationRequested)
        {
            AddLog("[NTH MHL] Bot not running.");
            return;
        }

        // Minimize app to allow screen capture of game
        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        });

        // Wait a bit for window to minimize
        await Task.Delay(500);

        workflow.Status = "Running";
        var token = _botCancellationTokenSource.Token;

        try
        {
            AddLog("[NTH MHL] Starting Step 3: Mộng Hoa Lục workflow...");

            // Run workflow on background thread
            await Task.Run(async () =>
            {
                var inputService = new InputService();
                var mhlWorkflow = new NthLv26To44MongHoaLucWorkflow(
                    _visionService,
                    inputService,
                    assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_monghoaluc"),
                    logger: msg => AddLog(msg));

                var context = new GameContext { GameName = "NTH Game" };
                var result = await mhlWorkflow.ExecuteAsync(context, token);

                if (result.Success)
                {
                    AddLog($"[NTH MHL] Completed successfully!");
                    UpdateWorkflowStatus(workflow, "Ready", incrementExecution: true);
                }
                else
                {
                    AddLog($"[NTH MHL] Failed: {result.Message}");
                    UpdateWorkflowStatus(workflow, "Failed");
                }

                // Restore window after completion
                RestoreMainWindow();
            }, token);
        }
        catch (OperationCanceledException)
        {
            AddLog("[NTH MHL] Cancelled.");
            workflow.Status = "Cancelled";
            RestoreMainWindow();
        }
        catch (Exception ex)
        {
            AddLog($"[NTH MHL] Error: {ex.Message}");
            workflow.Status = "Error";
            RestoreMainWindow();
        }
    }

    /// <summary>
    /// Restore main window from minimized state
    /// </summary>
    private void RestoreMainWindow()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        });
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
