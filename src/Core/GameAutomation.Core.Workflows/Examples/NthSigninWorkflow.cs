using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Excel;
using GameAutomation.Core.Workflows.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameAutomation.Core.Workflows.Examples;

/// <summary>
/// NTH Game Sign-in Workflow
/// Flow: Read Excel -> Find username box -> Enter credentials -> Login -> Handle popups -> Start game
/// Uses multi-scale template matching for all images
/// </summary>
public class NthSigninWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly ExcelService _excelService;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names
    private const string UsernameBoxTemplate = "01_usernamebox.png";
    private const string LoginButtonTemplate = "03_loginbutton.png";
    private const string CheckboxTemplate = "04_checkbox.png";
    private const string AgreeButtonTemplate = "05_agree.png";
    private const string StartButtonTemplate = "06_startbutton.png";
    private const string BackButtonTemplate = "07_backbutton.png";
    private const string StartButton2Template = "08_startbutton2.png";

    // Multi-scale settings - cho phép tìm ảnh ở nhiều kích thước
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterTypeDelayMs = 2000;  // Chờ 2s sau khi nhập text
    private const int AfterTabDelayMs = 1000;   // Chờ 1s sau khi ấn Tab

    public string Name => "NTH Sign-in Flow";
    public string Description => "Đăng nhập game NTH với thông tin từ file Excel";

    public NthSigninWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _humanSim = new HumanLikeSimulator(inputService);
        _excelService = new ExcelService();
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "signin");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the sign-in workflow
    /// </summary>
    /// <param name="context">Game context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="sheetName">Excel sheet name (null for first sheet)</param>
    /// <param name="startRow">Starting row number (1-indexed data row)</param>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default,
        string? sheetName = null,
        int startRow = 1)
    {
        try
        {
            Log($"[NTH Signin] Starting workflow from row {startRow}...");

            // Step 1: Read credentials from Excel
            var credentials = await ReadCredentialsFromExcelAsync(sheetName, startRow);
            if (credentials == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = $"Cannot read credentials from Excel at row {startRow}"
                };
            }

            var (username, password) = credentials.Value;
            Log($"[NTH Signin] Read credentials: {username} / ***");

            // Step 2: Find and click username box (wait max 5s)
            Log("[NTH Signin] Step 1: Finding username box...");
            var usernameBox = await WaitAndClickMultiScaleAsync(
                UsernameBoxTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (usernameBox == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Username box not found"
                };
            }

            // Step 3: Type username
            Log($"[NTH Signin] Step 2: Typing username: {username}");
            await _humanSim.TypeTextAsync(username);
            await Task.Delay(AfterTypeDelayMs, cancellationToken);

            // Step 4: Press Tab
            Log("[NTH Signin] Step 3: Pressing Tab...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.TAB);
            await Task.Delay(AfterTabDelayMs, cancellationToken);

            // Step 5: Type password
            Log("[NTH Signin] Step 4: Typing password...");
            await _humanSim.TypeTextAsync(password);
            await Task.Delay(AfterTypeDelayMs, cancellationToken);

            // Step 6: Find and click login button
            Log("[NTH Signin] Step 5: Finding login button...");
            var loginButton = await WaitAndClickMultiScaleAsync(
                LoginButtonTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (loginButton == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Login button not found"
                };
            }

            // Step 7: Handle checkbox popup (optional, max 5s)
            Log("[NTH Signin] Step 6: Checking for checkbox popup...");
            var checkbox = await FindMultiScaleAsync(CheckboxTemplate, timeoutMs: 5000, cancellationToken);

            if (checkbox != null)
            {
                Log("[NTH Signin] Checkbox found, clicking...");
                await ClickAtCenterAsync(checkbox);
                await Task.Delay(1000, cancellationToken);

                // Wait for agree button and click
                Log("[NTH Signin] Finding agree button...");
                var agreeButton = await WaitAndClickMultiScaleAsync(
                    AgreeButtonTemplate,
                    timeoutMs: 5000,
                    cancellationToken);

                if (agreeButton == null)
                {
                    Log("[NTH Signin] Warning: Agree button not found after checkbox");
                }
            }
            else
            {
                Log("[NTH Signin] No checkbox popup, continuing...");
            }

            // Step 8: Find and click start button
            Log("[NTH Signin] Step 7: Finding start button...");
            var startButton = await WaitAndClickMultiScaleAsync(
                StartButtonTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (startButton == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Start button not found"
                };
            }

            // Step 9: Wait for back button, then find and click start button 2
            Log("[NTH Signin] Step 8: Waiting for back button to appear...");
            var backButton = await FindMultiScaleAsync(BackButtonTemplate, timeoutMs: 30000, cancellationToken);

            if (backButton != null)
            {
                Log("[NTH Signin] Back button found, finding start button 2...");
                var startButton2 = await WaitAndClickMultiScaleAsync(
                    StartButton2Template,
                    timeoutMs: 15000,
                    cancellationToken);

                if (startButton2 == null)
                {
                    return new WorkflowResult
                    {
                        Success = false,
                        Message = "Start button 2 not found"
                    };
                }
            }
            else
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Back button not found - game may not have loaded properly"
                };
            }

            Log("[NTH Signin] Login completed successfully!");
            return new WorkflowResult
            {
                Success = true,
                Message = $"Logged in successfully as {username}",
                Data = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["username"] = username,
                    ["row"] = startRow
                }
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Signin] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Signin] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    // IWorkflow interface implementation
    public Task<WorkflowResult> ExecuteAsync(GameContext context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(context, cancellationToken, null, 1);
    }

    #region Private Methods

    /// <summary>
    /// Read credentials from Excel file
    /// Looks for "Nick phu nth.xlsx" or single xlsx file in assets folder
    /// </summary>
    private async Task<(string username, string password)?> ReadCredentialsFromExcelAsync(
        string? sheetName,
        int rowNumber)
    {
        var excelPath = FindExcelFile();
        if (excelPath == null)
        {
            Log($"[NTH Signin] ERROR: No Excel file found in assets folder: {_assetsPath}");
            return null;
        }

        Log($"[NTH Signin] Reading from: {excelPath}");

        try
        {
            // Check total rows
            var totalRows = _excelService.GetRowCount(excelPath, sheetName);
            Log($"[NTH Signin] Total data rows in Excel: {totalRows}");

            if (rowNumber > totalRows)
            {
                Log($"[NTH Signin] ERROR: Row {rowNumber} exceeds total rows ({totalRows})");
                return null;
            }

            if (rowNumber < 1)
            {
                Log($"[NTH Signin] ERROR: Row number must be >= 1, got {rowNumber}");
                return null;
            }

            var result = _excelService.ReadCredentials(excelPath, sheetName, rowNumber);
            if (result == null)
            {
                Log($"[NTH Signin] ERROR: Could not read data at row {rowNumber} - username may be empty");
            }
            else
            {
                Log($"[NTH Signin] Read successfully: {result.Value.username}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log($"[NTH Signin] ERROR reading Excel: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Find Excel file in assets folder
    /// Priority: "Nick phu nth.xlsx" -> single xlsx file
    /// </summary>
    private string? FindExcelFile()
    {
        if (!Directory.Exists(_assetsPath))
        {
            Log($"[NTH Signin] ERROR: Assets folder not found: {_assetsPath}");
            return null;
        }

        // First, look for specific file
        var specificFile = Path.Combine(_assetsPath, "Nick phu nth.xlsx");
        if (File.Exists(specificFile))
            return specificFile;

        // Otherwise, find any single xlsx file
        var xlsxFiles = Directory.GetFiles(_assetsPath, "*.xlsx");
        if (xlsxFiles.Length == 1)
            return xlsxFiles[0];

        if (xlsxFiles.Length > 1)
            Log($"[NTH Signin] WARNING: Multiple Excel files found, please use 'Nick phu nth.xlsx'");

        return null;
    }

    /// <summary>
    /// Find template using multi-scale matching
    /// Returns best match (highest confidence, closest to original size)
    /// </summary>
    private async Task<DetectionResult?> FindMultiScaleAsync(
        string templateFileName,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(_assetsPath, templateFileName);
        if (!File.Exists(templatePath))
        {
            Log($"[NTH Signin] WARNING: Template not found: {templateFileName}");
            return null;
        }

        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        while (DateTime.Now < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplateMultiScale(
                screenshot,
                templatePath,
                MatchThreshold,
                MinScale,
                MaxScale,
                ScaleSteps);

            if (results.Count > 0)
            {
                // Get best result: highest confidence, and prefer scale closest to 1.0
                var best = GetBestMatch(results, templatePath);
                Log($"[NTH Signin] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y})");
                return best;
            }

            await Task.Delay(300, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Get best match from multi-scale results
    /// Prefers: highest confidence + scale closest to 1.0 (original size)
    /// </summary>
    private DetectionResult GetBestMatch(System.Collections.Generic.List<DetectionResult> results, string templatePath)
    {
        if (results.Count == 1)
            return results[0];

        // Load template to get original size
        using var template = new System.Drawing.Bitmap(templatePath);
        int originalWidth = template.Width;
        int originalHeight = template.Height;

        // Score = confidence * (1 - scale_deviation)
        // scale_deviation = abs(detected_size / original_size - 1)
        var scored = results.Select(r =>
        {
            double scaleX = (double)r.Width / originalWidth;
            double scaleY = (double)r.Height / originalHeight;
            double avgScale = (scaleX + scaleY) / 2;
            double scaleDeviation = Math.Abs(avgScale - 1.0);
            double score = r.Confidence * (1 - scaleDeviation * 0.5); // Weight scale deviation at 50%
            return (result: r, score);
        });

        return scored.OrderByDescending(x => x.score).First().result;
    }

    /// <summary>
    /// Wait for template and click at center with human-like movement
    /// </summary>
    private async Task<DetectionResult?> WaitAndClickMultiScaleAsync(
        string templateFileName,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var result = await FindMultiScaleAsync(templateFileName, timeoutMs, cancellationToken);
        if (result == null)
            return null;

        await ClickAtCenterAsync(result);
        return result;
    }

    /// <summary>
    /// Click at center of detection result with human-like movement
    /// </summary>
    private async Task ClickAtCenterAsync(DetectionResult result)
    {
        int centerX = result.X + result.Width / 2;
        int centerY = result.Y + result.Height / 2;

        await _humanSim.MoveMouseAsync(centerX, centerY);
        await _humanSim.LeftClickAsync();
    }

    private void Log(string message)
    {
        _logger?.Invoke(message);
        Console.WriteLine(message);
    }

    #endregion
}
