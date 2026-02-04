using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Workflows.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GameAutomation.Core.Models.Vision.ImageResourceRegistry;

namespace GameAutomation.Core.Workflows.Examples;

/// <summary>
/// NTH Game Lv26 to Lv44 Setting Combat Workflow - Step 4
/// Flow: Press B -> Setup auto medicine -> Configure combat settings
/// Uses multi-scale template matching for all images
/// If any step fails to find image, skip to next step
/// </summary>
public class NthLv26To44SettingCombatWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for Setting Combat flow
    private const string SearchButtonTemplate = "01_settingcombat_searchbutton.png";
    private const string AutoMedicineTemplate = "02_settingcombat_automedicine.png";
    private const string UseMedicineTemplate = "03_settingcombat_usemedicine.png";
    private const string SkipTemplate = "04_settingcombat_skip.png";
    private const string OpenAutoTemplate = "05_settingcombat_openauto.png";
    private const string QuickSwapTemplate = "06_settingcombat_quickswap.png";

    // Multi-scale settings - cho phep tim anh o nhieu kich thuoc
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterTypeDelayMs = 1000;   // Cho 1s sau khi nhap text
    private const int ShortDelayMs = 500;        // Cho ngan 0.5s
    private const int MediumDelayMs = 1000;      // Cho vua 1s
    private const int DoubleClickDelayMs = 500;  // Cho giua 2 lan click

    public string Name => "NTH Lv26-44 Setting Combat";
    public string Description => "Quy trinh cai dat tu dong danh cho game NTH tu level 26 den 44 (Step 4)";

    public NthLv26To44SettingCombatWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "ingame_settingcombat");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the Setting Combat workflow (Step 4 of lv26-44 flow)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH Combat] Starting Step 4: Setting Combat workflow...");

            // ===== BUOC 4.1: An phim B =====
            Log("[NTH Combat] Step 4.1: Pressing B key to open combat settings...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.B);
            await Task.Delay(MediumDelayMs, cancellationToken); // Cho 1s de menu mo

            // ===== BUOC 4.2: Tim va click vao 02_settingcombat_automedicine =====
            Log("[NTH Combat] Step 4.2: Looking for auto medicine button...");
            var autoMedicine = await WaitAndClickMultiScaleAsync(
                AutoMedicineTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (autoMedicine == null)
            {
                Log("[NTH Combat] Auto medicine button not found, skipping to next step...");
            }
            else
            {
                await Task.Delay(ShortDelayMs, cancellationToken);
            }

            // ===== BUOC 4.3: Tim va click vao 03_settingcombat_usemedicine =====
            Log("[NTH Combat] Step 4.3: Looking for use medicine button...");
            var useMedicine = await WaitAndClickMultiScaleAsync(
                UseMedicineTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (useMedicine == null)
            {
                Log("[NTH Combat] Use medicine button not found, skipping to next step...");
            }
            else
            {
                await Task.Delay(ShortDelayMs, cancellationToken);
            }

            // ===== BUOC 4.4: An Esc roi an Y =====
            Log("[NTH Combat] Step 4.4: Pressing Escape then Y...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
            await Task.Delay(ShortDelayMs, cancellationToken);
            await _humanSim.KeyPressAsync(VirtualKeyCode.Y);
            await Task.Delay(MediumDelayMs, cancellationToken); // Cho 1s

            // ===== BUOC 4.5: Tim va click vao 04_settingcombat_skip =====
            Log("[NTH Combat] Step 4.5: Looking for skip button...");
            var skipButton = await WaitAndClickMultiScaleAsync(
                SkipTemplate,
                timeoutMs: 2000,
                cancellationToken);

            if (skipButton == null)
            {
                Log("[NTH Combat] Skip button not found, skipping to next step...");
            }
            else
            {
                await Task.Delay(ShortDelayMs, cancellationToken);
                await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
                await Task.Delay(ShortDelayMs, cancellationToken);
            }

            // ===== BUOC 4.6: Tim va click vao 01_settingcombat_searchbutton 2 lan =====
            Log("[NTH Combat] Step 4.6: Looking for search button (will click 2 times)...");
            var searchButton = await FindMultiScaleAsync(
                SearchButtonTemplate,
                timeoutMs: 2000,
                cancellationToken);

            if (searchButton == null)
            {
                Log("[NTH Combat] Search button not found, skipping to next step...");
            }
            else
            {
                // Click lan 1
                Log("[NTH Combat] Clicking search button (1/2)...");
                await ClickAtCenterAsync(searchButton);
                await Task.Delay(DoubleClickDelayMs, cancellationToken); // Cho 500ms

                // Click lan 2
                Log("[NTH Combat] Clicking search button (2/2)...");
                await ClickAtCenterAsync(searchButton);
                await Task.Delay(MediumDelayMs, cancellationToken); // Cho 1s
            }

            // ===== BUOC 4.7: Tim va click vao 05_settingcombat_openauto =====
            Log("[NTH Combat] Step 4.7: Looking for open auto button...");
            var openAuto = await WaitAndClickMultiScaleAsync(
                OpenAutoTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (openAuto == null)
            {
                Log("[NTH Combat] Open auto button not found, skipping to next step...");
            }
            else
            {
                await Task.Delay(ShortDelayMs, cancellationToken);
            }

            // ===== BUOC 4.8: Tim va click vao 06_settingcombat_quickswap =====
            Log("[NTH Combat] Step 4.8: Looking for quick swap button...");
            var quickSwap = await WaitAndClickMultiScaleAsync(
                QuickSwapTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (quickSwap == null)
            {
                Log("[NTH Combat] Quick swap button not found, skipping to next step...");
            }
            else
            {
                await Task.Delay(ShortDelayMs, cancellationToken);
            }

            // ===== BUOC 4.9: An Esc de dong menu =====
            Log("[NTH Combat] Step 4.9: Pressing Escape to close menu...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
            await Task.Delay(MediumDelayMs, cancellationToken);

            Log("[NTH Combat] Step 4 completed successfully!");
            return new WorkflowResult
            {
                Success = true,
                Message = "Setting Combat workflow completed successfully"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Combat] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Combat] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Find template using multi-scale matching with optional ROI
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
            Log($"[NTH Combat] WARNING: Template not found: {templateFileName}");
            return null;
        }

        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        // Get search region using GetEffectiveRegion (respects UseRegionSearch setting)
        var templateKey = $"combat/{Path.GetFileNameWithoutExtension(templateFileName)}";
        var searchRegion = GetEffectiveRegion(templateKey);

        while (DateTime.Now < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplateMultiScaleInRegion(
                screenshot,
                templatePath,
                searchRegion,
                MatchThreshold,
                MinScale,
                MaxScale,
                ScaleSteps);

            if (results.Count > 0)
            {
                // Get best result: highest confidence, and prefer scale closest to 1.0
                var best = GetBestMatch(results, templatePath);
                var regionInfo = searchRegion != null ? $" [ROI: {searchRegion}]" : " [Full]";
                Log($"[NTH Combat] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y}){regionInfo}");
                return best;
            }

            await Task.Delay(100, cancellationToken);
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
