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

namespace GameAutomation.Core.Workflows.Examples;

/// <summary>
/// NTH Game Lv26 to Lv44 Map Workflow - Step 5
/// Flow: Open map -> Zoom -> Navigate to treasure chest location
/// Uses multi-scale template matching for all images
/// If any step fails to find image, skip to next step
/// </summary>
public class NthLv26To44MapWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for Map flow
    private const string ZoomUpButtonTemplate = "02_map_zoomupbutton.png";
    private const string SkipInstructionTemplate = "03_map_skipinstruction.png";
    private const string InputButtonTemplate = "04_map_inputbutton.png";
    private const string FollowButtonTemplate = "05_map_follow.png";
    private const string ZIndex555Template = "06_map_zindex555.png";
    private const string ChestTemplate = "07_map_ruong.png";
    private const string Chest2Template = "07_map_ruong2.png";
    private const string Undo = "map_delete.png";
    // Multi-scale settings
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterActionDelayMs = 100;   // Cho 0.5s sau moi hanh dong
    private const int ShortDelayMs = 100;         // Cho ngan 0.2s
    private const int MediumDelayMs = 300;        // Cho vua 0.5s

    public string Name => "NTH Lv26-44 Map Navigation";
    public string Description => "Quy trinh tim duong toi ruong bau cho game NTH tu level 26 den 44 (Step 5)";

    public NthLv26To44MapWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "ingame_map");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the Map Navigation workflow (Step 5 of lv26-44 flow)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH Map] Starting Step 5: Map Navigation workflow...");

            // ===== BUOC 5.1: An phim M de mo map =====
            Log("[NTH Map] Step 5.1: Pressing M key to open map...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.M);
            await Task.Delay(MediumDelayMs, cancellationToken);

            // ===== BUOC 5.2: Tim zoom button va click 10 lan =====
            Log("[NTH Map] Step 5.2: Looking for zoom up button (will click 10 times)...");
            var zoomButton = await FindMultiScaleAsync(
                ZoomUpButtonTemplate,
                timeoutMs: 15000,
                cancellationToken);

            if (zoomButton != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Kiem tra xem co skip instruction khong
                    var skipInstruction = await FindMultiScaleAsync(
                        SkipInstructionTemplate,
                        timeoutMs: 100,
                        cancellationToken);

                    if (skipInstruction != null)
                    {
                        Log("[NTH Map] Skip instruction found, clicking...");
                        await ClickAtCenterAsync(skipInstruction);
                        await Task.Delay(ShortDelayMs, cancellationToken);
                        await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
                        await Task.Delay(ShortDelayMs, cancellationToken);
                    }

                    Log($"[NTH Map] Clicking zoom button ({i + 1}/10)...");
                    await ClickAtCenterAsync(zoomButton);
                    await Task.Delay(ShortDelayMs, cancellationToken); // 0.2s giua cac lan click
                }
            }
            else
            {
                Log("[NTH Map] Zoom button not found, skipping...");
            }

            // ===== BUOC 5.3: Tim va click input button =====
            Log("[NTH Map] Step 5.3: Looking for input button...");
            var inputButton = await WaitAndClickMultiScaleAsync(
                InputButtonTemplate,
                timeoutMs: 3000,
                cancellationToken);

            if (inputButton == null)
            {
                Log("[NTH Map] Input button not found, skipping...");
            }
            else
            {
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }

            // ===== BUOC 5.4: Click so 8 -> 5 -> 9 =====
            Log("[NTH Map] Step 5.4: Entering coordinates 8 -> 5 -> 9...");
            await ClickNumberSequenceAsync(new[] { 8, 5, 9 }, cancellationToken);
            await Task.Delay(AfterActionDelayMs, cancellationToken);

            // ===== BUOC 5.5: Tim zindex555 va click giua zindex555 va input button =====
            Log("[NTH Map] Step 5.5: Looking for Z-index 555 button...");
            var zindex555 = await FindMultiScaleAsync(
                FollowButtonTemplate,
                timeoutMs: 2000,
                cancellationToken);

            // Tim lai input button de tinh vi tri giua
            var inputButtonAgain = await FindMultiScaleAsync(
                InputButtonTemplate,
                timeoutMs: 1000,
                cancellationToken);

            if (zindex555 != null && inputButtonAgain != null)
            {
                // Tinh vi tri giua 2 anh
                int zindexCenterY = zindex555.Y + zindex555.Height / 2;
                int inputCenterY = inputButtonAgain.Y + inputButtonAgain.Height / 2;
                int middleY = (zindexCenterY + inputCenterY) / 2;
                int middleX = (zindex555.X + zindex555.Width / 2 + inputButtonAgain.X + inputButtonAgain.Width / 2) / 2;

                Log($"[NTH Map] Clicking between zindex555 and input button at ({middleX}, {middleY})...");
                await _humanSim.MoveMouseAsync(middleX - 10, middleY);
                await _humanSim.LeftClickAsync();
                await Task.Delay(AfterActionDelayMs, cancellationToken);

                var undo = await FindMultiScaleAsync(
                            Undo,
                            timeoutMs: 2000,
                            cancellationToken);
                if (undo != null)
                {
                    // Di chuyen toi chinh giua nut Undo va click 3 lan
                    int undoCenterX = undo.X + undo.Width / 2;
                    int undoCenterY = undo.Y + undo.Height / 2;
                    Log($"[NTH Map] Found Undo button, clicking 3 times at ({undoCenterX}, {undoCenterY})...");

                    await _humanSim.MoveMouseAsync(undoCenterX, undoCenterY);
                    await _humanSim.LeftClickAsync();
                    await Task.Delay(ShortDelayMs, cancellationToken);
                    await _humanSim.LeftClickAsync();
                    await Task.Delay(ShortDelayMs, cancellationToken);
                    await _humanSim.LeftClickAsync();
                    await Task.Delay(ShortDelayMs, cancellationToken);
                }
                else
                {
                    Log("[NTH Map] Undo not found, skipping...");
                }
                // Click so 9 -> 8 -> 3
                Log("[NTH Map] Entering Z-index coordinates 9 -> 8 -> 3...");
                await ClickNumberSequenceAsync(new[] { 9, 8, 3 }, cancellationToken);
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }
            else
            {
                Log("[NTH Map] Z-index or input button not found, skipping...");
            }

            // ===== BUOC 5.6: Tim va click follow button =====
            Log("[NTH Map] Step 5.6: Looking for follow button...");
            var followButton = await WaitAndClickMultiScaleAsync(
                FollowButtonTemplate,
                timeoutMs: 3000,
                cancellationToken);

            if (followButton == null)
            {
                Log("[NTH Map] Follow button not found, skipping...");
            }
            else
            {
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }

            // ===== BUOC 5.7: An Esc =====
            Log("[NTH Map] Step 5.7: Pressing Escape to close map...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
            await Task.Delay(MediumDelayMs, cancellationToken);

            // ===== BUOC 5.8: Tim ruong bau (toi da 3 lan thu) =====
            Log("[NTH Map] Step 5.8: Looking for treasure chest...");
            await Task.Delay(MediumDelayMs, cancellationToken);
            await Task.Delay(5000, cancellationToken);
            int maxAttempts = 3;
            bool chestFound = false;

            for (int attempt = 0; attempt < maxAttempts && !chestFound; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Log($"[NTH Map] Attempt {attempt + 1}/{maxAttempts}: Pressing E twice...");
                await _humanSim.KeyPressAsync(VirtualKeyCode.E);
                await Task.Delay(ShortDelayMs, cancellationToken);
                await _humanSim.KeyPressAsync(VirtualKeyCode.E);
                await Task.Delay(MediumDelayMs, cancellationToken);

                // Tim ruong 1
                Log("[NTH Map] Looking for chest type 1...");
                var chest = await FindMultiScaleAsync(
                    ChestTemplate,
                    timeoutMs: 2000,
                    cancellationToken);

                if (chest != null)
                {
                    Log("[NTH Map] Chest type 1 found, clicking...");
                    await ClickAtCenterAsync(chest);
                    chestFound = true;
                    break;
                }

                // Tim ruong 2
                Log("[NTH Map] Chest type 1 not found, looking for chest type 2...");
                var chest2 = await FindMultiScaleAsync(
                    Chest2Template,
                    timeoutMs: 2000,
                    cancellationToken);

                if (chest2 != null)
                {
                    Log("[NTH Map] Chest type 2 found, pressing F...");
                    await _humanSim.KeyPressAsync(VirtualKeyCode.F);
                    chestFound = true;
                    break;
                }

                if (attempt < maxAttempts - 1)
                {
                    Log($"[NTH Map] No chest found, will retry...");
                }
            }

            if (!chestFound)
            {
                Log("[NTH Map] No chest found after 3 attempts, moving on...");
            }

            Log("[NTH Map] Step 5 completed!");
            return new WorkflowResult
            {
                Success = true,
                Message = chestFound
                    ? "Map Navigation workflow completed - chest found!"
                    : "Map Navigation workflow completed - no chest found"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Map] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Map] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Click a sequence of numbers using map_x.png templates
    /// Uses high threshold (95%) because numbers like 3 and 8 look similar
    /// </summary>
    private async Task ClickNumberSequenceAsync(int[] numbers, CancellationToken cancellationToken)
    {
        foreach (var num in numbers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var templateName = $"map_{num}.png";
            Log($"[NTH Map] Looking for number {num} (threshold 95%)...");

            // Dung threshold cao 95% vi cac so 3 va 8 giong nhau
            var numButton = await FindMultiScaleAsync(
                templateName,
                timeoutMs: 2000,
                cancellationToken,
                threshold: 0.95);

            if (numButton != null)
            {
                Log($"[NTH Map] Found number {num} with {numButton.Confidence:P1} confidence, clicking...");
                await ClickAtCenterAsync(numButton);
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }
            else
            {
                Log($"[NTH Map] Number {num} not found with 95% confidence, skipping...");
            }
        }
    }

    /// <summary>
    /// Find template using multi-scale matching
    /// Returns best match (highest confidence, closest to original size)
    /// </summary>
    /// <param name="threshold">Custom threshold (default uses MatchThreshold = 0.7)</param>
    private async Task<DetectionResult?> FindMultiScaleAsync(
        string templateFileName,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default,
        double? threshold = null)
    {
        var templatePath = Path.Combine(_assetsPath, templateFileName);
        if (!File.Exists(templatePath))
        {
            Log($"[NTH Map] WARNING: Template not found: {templateFileName}");
            return null;
        }

        var actualThreshold = threshold ?? MatchThreshold;
        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        while (DateTime.Now < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplateMultiScale(
                screenshot,
                templatePath,
                actualThreshold,
                MinScale,
                MaxScale,
                ScaleSteps);

            if (results.Count > 0)
            {
                // Loc chi lay ket qua co confidence >= threshold
                var filteredResults = results.Where(r => r.Confidence >= actualThreshold).ToList();

                if (filteredResults.Count > 0)
                {
                    var best = GetBestMatch(filteredResults, templatePath);
                    Log($"[NTH Map] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y})");
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
