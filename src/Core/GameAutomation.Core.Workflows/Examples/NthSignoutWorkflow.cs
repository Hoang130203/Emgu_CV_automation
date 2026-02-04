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
/// NTH Game Signout Workflow - Step 8
/// Flow: Esc -> Settings -> Signout -> Other account -> Zing signin
/// Uses multi-scale template matching for all images
/// If any step fails to find image, skip to next step
/// </summary>
public class NthSignoutWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for Signout flow
    private const string SettingTemplate = "01_signout_setting.png";
    private const string SignoutPanelTemplate = "02_signout_signout.png";
    private const string SignoutButtonTemplate = "03_signout_signout_button.png";
    private const string OtherAccountTemplate = "04_signout_otheraccount.png";
    private const string ZingSigninTemplate = "05_signout_zingsignin.png";

    // Multi-scale settings
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterActionDelayMs = 300;   // 0.3s sau moi hanh dong
    private const int DoubleClickDelayMs = 1000;  // 1s giua 2 lan click

    public string Name => "NTH Signout";
    public string Description => "Quy trinh dang xuat game NTH (Step 8)";

    public NthSignoutWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "signout");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the Signout workflow (Step 8)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH Signout] Starting Step 8: Signout workflow...");

            // ===== BUOC 8.1 + 8.2: An Esc va tim setting (retry toi da 4 lan) =====
            Log("[NTH Signout] Step 8.1-8.2: Pressing Escape and looking for setting button...");
            DetectionResult? settingButton = null;
            const int maxEscAttempts = 4;

            for (int attempt = 0; attempt < maxEscAttempts && settingButton == null; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Log($"[NTH Signout] Pressing Escape (attempt {attempt + 1}/{maxEscAttempts})...");
                await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
                await Task.Delay(AfterActionDelayMs, cancellationToken);

                // Tim setting button
                settingButton = await FindMultiScaleAsync(
                    SettingTemplate,
                    timeoutMs: 3000,
                    cancellationToken);

                if (settingButton != null)
                {
                    Log("[NTH Signout] Setting button found, clicking...");
                    await ClickAtCenterAsync(settingButton);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                    break;
                }
            }

            if (settingButton == null)
            {
                Log("[NTH Signout] Setting button not found after 4 attempts, skipping...");
            }

            // ===== BUOC 8.3: Tim signout panel va click vao signout button ben trong =====
            Log("[NTH Signout] Step 8.3: Looking for signout panel and button...");

            // Tim signout panel truoc
            var signoutPanel = await FindMultiScaleAsync(
                SignoutPanelTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (signoutPanel != null)
            {
                Log("[NTH Signout] Signout panel found, looking for signout button inside...");

                // Tim signout button trong vung cua panel
                var signoutButton = await FindButtonInRegionAsync(
                    SignoutButtonTemplate,
                    signoutPanel,
                    timeoutMs: 3000,
                    cancellationToken);

                if (signoutButton != null)
                {
                    Log("[NTH Signout] Signout button found, clicking...");
                    await ClickAtCenterAsync(signoutButton);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                }
                else
                {
                    // Neu k tim thay trong vung, thu tim tren toan man hinh
                    Log("[NTH Signout] Button not found in region, searching full screen...");
                    var signoutButtonFull = await WaitAndClickMultiScaleAsync(
                        SignoutButtonTemplate,
                        timeoutMs: 2000,
                        cancellationToken);

                    if (signoutButtonFull != null)
                    {
                        await Task.Delay(AfterActionDelayMs, cancellationToken);
                    }
                }
            }
            else
            {
                Log("[NTH Signout] Signout panel not found, skipping...");
            }

            // ===== BUOC 8.4: An Space =====
            Log("[NTH Signout] Step 8.4: Pressing Space...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
            await Task.Delay(AfterActionDelayMs, cancellationToken);

            // ===== BUOC 8.5: Doi other account (30s) va click 2 lan =====
            Log("[NTH Signout] Step 8.5: Waiting for other account button (30s timeout)...");
            var otherAccount = await FindMultiScaleAsync(
                OtherAccountTemplate,
                timeoutMs: 30000,
                cancellationToken,
                0.85);

            if (otherAccount != null)
            {
                Log("[NTH Signout] Other account button found, clicking twice...");

                // Click lan 1
                await ClickAtCenterAsync(otherAccount);
                await Task.Delay(DoubleClickDelayMs, cancellationToken); // Cho 1s

                // Click lan 2
                await ClickAtCenterAsync(otherAccount);
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }
            else
            {
                Log("[NTH Signout] Other account button not found after 30s...");
            }

            // ===== BUOC 8.6: Tim va click zing signin =====
            Log("[NTH Signout] Step 8.6: Looking for Zing signin button...");
            var zingSignin = await WaitAndClickMultiScaleAsync(
                ZingSigninTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (zingSignin == null)
            {
                Log("[NTH Signout] Zing signin button not found...");
            }
            else
            {
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }

            Log("[NTH Signout] Step 8 completed!");
            return new WorkflowResult
            {
                Success = true,
                Message = "Signout workflow completed successfully"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Signout] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Signout] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Find a button template within a specific region (inside another detected element)
    /// </summary>
    private async Task<DetectionResult?> FindButtonInRegionAsync(
        string templateFileName,
        DetectionResult region,
        int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(_assetsPath, templateFileName);
        if (!File.Exists(templatePath))
        {
            Log($"[NTH Signout] WARNING: Template not found: {templateFileName}");
            return null;
        }

        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        // Get search region using GetEffectiveRegion (respects UseRegionSearch setting)
        var templateKey = $"signout/{Path.GetFileNameWithoutExtension(templateFileName)}";
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
                // Loc chi lay ket qua nam trong vung cua region
                var inRegion = results.Where(r =>
                    r.X >= region.X &&
                    r.Y >= region.Y &&
                    r.X + r.Width <= region.X + region.Width &&
                    r.Y + r.Height <= region.Y + region.Height
                ).ToList();

                if (inRegion.Count > 0)
                {
                    var best = GetBestMatch(inRegion, templatePath);
                    var regionInfo = searchRegion != null ? $" [ROI: {searchRegion}]" : " [Full]";
                    Log($"[NTH Signout] Found {templateFileName} in region - Confidence: {best.Confidence:P1}{regionInfo}");
                    return best;
                }
            }

            await Task.Delay(100, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Find template using multi-scale matching with optional ROI
    /// Returns best match (highest confidence, closest to original size)
    /// </summary>
    private async Task<DetectionResult?> FindMultiScaleAsync(
        string templateFileName,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default,
        double? threshold = null)
    {
        var templatePath = Path.Combine(_assetsPath, templateFileName);
        if (!File.Exists(templatePath))
        {
            Log($"[NTH Signout] WARNING: Template not found: {templateFileName}");
            return null;
        }

        var actualThreshold = threshold ?? MatchThreshold;
        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        // Get search region using GetEffectiveRegion (respects UseRegionSearch setting)
        var templateKey = $"signout/{Path.GetFileNameWithoutExtension(templateFileName)}";
        var searchRegion = GetEffectiveRegion(templateKey);

        while (DateTime.Now < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplateMultiScaleInRegion(
                screenshot,
                templatePath,
                searchRegion,
                actualThreshold,
                MinScale,
                MaxScale,
                ScaleSteps);

            if (results.Count > 0)
            {
                var filteredResults = results.Where(r => r.Confidence >= actualThreshold).ToList();

                if (filteredResults.Count > 0)
                {
                    var best = GetBestMatch(filteredResults, templatePath);
                    var regionInfo = searchRegion != null ? $" [ROI: {searchRegion}]" : " [Full]";
                    Log($"[NTH Signout] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y}){regionInfo}");
                    return best;
                }
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

        using var template = new System.Drawing.Bitmap(templatePath);
        int originalWidth = template.Width;
        int originalHeight = template.Height;

        var scored = results.Select(r =>
        {
            double scaleX = (double)r.Width / originalWidth;
            double scaleY = (double)r.Height / originalHeight;
            double avgScale = (scaleX + scaleY) / 2;
            double scaleDeviation = Math.Abs(avgScale - 1.0);
            double score = r.Confidence * (1 - scaleDeviation * 0.5);
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
