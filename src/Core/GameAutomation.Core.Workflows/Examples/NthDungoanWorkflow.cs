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
/// NTH Game Du Ngoan (Dungoan) Workflow - Step 7
/// Flow: Skip intro -> Main tale -> Start event -> Complete dungeon -> Done
/// Uses multi-scale template matching for all images
/// If any step fails to find image, skip to next step
/// </summary>
public class NthDungoanWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for Dungoan flow
    private const string SkipTemplate = "01_dungoan_skipbutton.png";
    private const string MainTaleTemplate = "02_dungoan_maintale.png";
    private const string StartEventTemplate = "03_dungoan_startevent.png";
    private const string NameEventTemplate = "04_dungoan_nameevent.png";
    private const string GoDungeonTemplate = "05_dungoan_godungeon.png";
    private const string Skip2Template = "06_dungoan_skip.png";
    private const string RebornTemplate = "07_dungoan_reborn.png";
    private const string DoneEventTemplate = "08_dungoan_doneevent.png";
    private const string CloseEventTemplate = "09_dungoan_closeevent.png";
    private const string DailyDoneTemplate = "10_dungoan_dailydone.png";

    // Multi-scale settings
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterActionDelayMs = 300;   // 0.3s sau moi hanh dong
    private const int ShortDelayMs = 200;         // 0.2s
    private const int HalfSecondDelayMs = 500;    // 0.5s
    private const int FastPressDelayMs = 50;      // 0.05s cho an E lien tuc

    public string Name => "NTH Dungoan";
    public string Description => "Quy trinh Du Ngoan cho game NTH (Step 7)";

    public NthDungoanWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "ingame_dungoan");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the Dungoan workflow (Step 7)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH Dungoan] Starting Step 7: Du Ngoan workflow...");

            // ===== BUOC 7.1: Doi skip (10s) va click =====
            Log("[NTH Dungoan] Step 7.1: Waiting for skip button (10s timeout)...");
            var skip1 = await WaitAndClickMultiScaleAsync(
                SkipTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (skip1 != null)
            {
                await Task.Delay(ShortDelayMs, cancellationToken); // 0.2s
                await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }
            else
            {
                Log("[NTH Dungoan] Skip button not found, continuing...");
            }

            // ===== BUOC 7.2: Tim maintale (20s) va click =====
            Log("[NTH Dungoan] Step 7.2: Looking for main tale button (20s timeout)...");
            var mainTale = await WaitAndClickMultiScaleAsync(
                MainTaleTemplate,
                timeoutMs: 20000,
                cancellationToken);

            if (mainTale == null)
            {
                Log("[NTH Dungoan] Main tale button not found, continuing...");
            }
            else
            {
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }

            // ===== BUOC 7.3: Cho start event va click =====
            Log("[NTH Dungoan] Step 7.3: Looking for start event button...");
            var startEvent = await WaitAndClickMultiScaleAsync(
                StartEventTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (startEvent == null)
            {
                Log("[NTH Dungoan] Start event button not found, continuing...");
            }
            else
            {
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }

            // ===== BUOC 7.4: Vong lap click nameevent cho toi khi thay godungeon =====
            Log("[NTH Dungoan] Step 7.4: Clicking name event until go dungeon appears...");
            bool goDungeonFound = false;
            int maxNameEventClicks = 60; // Toi da 30 giay (60 * 0.5s)
            int nameEventClicks = 0;

            while (!goDungeonFound && nameEventClicks < maxNameEventClicks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Tim godungeon truoc
                var goDungeon = await FindMultiScaleAsync(
                    GoDungeonTemplate,
                    timeoutMs: 100,
                    cancellationToken);

                if (goDungeon != null)
                {
                    Log("[NTH Dungoan] Go dungeon button found, clicking...");
                    await ClickAtCenterAsync(goDungeon);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                    goDungeonFound = true;
                    break;
                }

                // Neu chua co godungeon, tim va click nameevent
                var nameEvent = await FindMultiScaleAsync(
                    NameEventTemplate,
                    timeoutMs: 100,
                    cancellationToken);

                if (nameEvent != null)
                {
                    await ClickAtCenterAsync(nameEvent);
                    await Task.Delay(ShortDelayMs, cancellationToken);
                    await ClickAtCenterAsync(nameEvent);
                    await Task.Delay(ShortDelayMs, cancellationToken);
                    await ClickAtCenterAsync(nameEvent);
                    nameEventClicks++;
                }
                else
                {
                    // Khong thay ca 2 anh, thoat vong lap
                    Log("[NTH Dungoan] Neither name event nor go dungeon found, moving on...");
                    break;
                }

                await Task.Delay(HalfSecondDelayMs, cancellationToken); // 0.5s
            }

            // ===== BUOC 7.5: Cho skip2 va click =====
            Log("[NTH Dungoan] Step 7.5: Looking for second skip button...");
            var skip2 = await WaitAndClickMultiScaleAsync(
                Skip2Template,
                timeoutMs: 10000,
                cancellationToken);

            if (skip2 != null)
            {
                await Task.Delay(ShortDelayMs, cancellationToken); // 0.2s
                await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }
            else
            {
                Log("[NTH Dungoan] Second skip button not found, continuing...");
            }

            // ===== BUOC 7.6: Vong lap an E lien tuc, xu ly reborn va doneevent =====
            Log("[NTH Dungoan] Step 7.6: Starting E spam loop...");
            bool eventDone = false;
            int maxEPresses = 6000; // Toi da 5 phut (6000 * 0.05s)
            int ePresses = 0;

            while (!eventDone && ePresses < maxEPresses)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Kiem tra doneevent truoc (uu tien cao)
                var doneEvent = await FindMultiScaleAsync(
                    DoneEventTemplate,
                    timeoutMs: 50,
                    cancellationToken);

                if (doneEvent != null)
                {
                    Log("[NTH Dungoan] Done event found, clicking...");
                    await ClickAtCenterAsync(doneEvent);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);

                    // Cho close event
                    Log("[NTH Dungoan] Waiting for close event button...");
                    var closeEvent = await WaitAndClickMultiScaleAsync(
                        CloseEventTemplate,
                        timeoutMs: 10000,
                        cancellationToken);

                    if (closeEvent != null)
                    {
                        await Task.Delay(AfterActionDelayMs, cancellationToken);
                    }

                    eventDone = true;
                    break;
                }

                // Kiem tra reborn
                var reborn = await FindMultiScaleAsync(
                    RebornTemplate,
                    timeoutMs: 50,
                    cancellationToken);

                if (reborn != null)
                {
                    Log("[NTH Dungoan] Reborn button found, clicking...");
                    await ClickAtCenterAsync(reborn);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                    // Tiep tuc an E
                }

                // An E
                for (int i = 0; i < 40; i++)
                {
                    await _humanSim.KeyPressAsync(VirtualKeyCode.E);
                    await Task.Delay(80, cancellationToken);
                }
                ePresses++;

                await Task.Delay(FastPressDelayMs, cancellationToken); // 0.05s
            }
            await Task.Delay(1000, cancellationToken);
            // ===== BUOC 7.7: An Esc, click giua man hinh, an F1 =====
            Log("[NTH Dungoan] Step 7.7: Pressing Esc, clicking center, pressing F1...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
            await Task.Delay(1000, cancellationToken); // Delay 1s

            // Lay kich thuoc man hinh va click giua 2 lan
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                int centerX = primaryScreen.Bounds.Width / 2;
                int centerY = primaryScreen.Bounds.Height / 2;

                Log($"[NTH Dungoan] Clicking center of screen ({centerX}, {centerY}) twice...");
                await _humanSim.MoveMouseAsync(centerX, centerY);
                await _humanSim.LeftClickAsync();
                await Task.Delay(AfterActionDelayMs, cancellationToken);
                await _humanSim.LeftClickAsync();
                await Task.Delay(AfterActionDelayMs, cancellationToken);
            }

            // An F1
            Log("[NTH Dungoan] Pressing F1...");
            await _humanSim.KeyPressAsync(VirtualKeyCode.F1);
            await Task.Delay(AfterActionDelayMs, cancellationToken);

            // ===== BUOC 7.8: Cho dailydone va click, retry F1 neu khong thay =====
            Log("[NTH Dungoan] Step 7.8: Looking for daily done button...");
            bool dailyDoneFound = false;
            const int maxDailyDoneAttempts = 5;

            for (int attempt = 0; attempt < maxDailyDoneAttempts && !dailyDoneFound; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dailyDone = await WaitAndClickMultiScaleAsync(
                    DailyDoneTemplate,
                    timeoutMs: 10000,
                    cancellationToken);

                if (dailyDone != null)
                {
                    dailyDoneFound = true;
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                    await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                    Log("[NTH Dungoan] Daily done button found and clicked!");
                }
                else
                {
                    Log($"[NTH Dungoan] Daily done button not found, pressing F1 and retrying (attempt {attempt + 1}/{maxDailyDoneAttempts})...");
                    await _humanSim.KeyPressAsync(VirtualKeyCode.F1);
                    await Task.Delay(AfterActionDelayMs, cancellationToken);
                }
            }

            if (!dailyDoneFound)
            {
                Log("[NTH Dungoan] Daily done button not found after all attempts...");
            }

            Log("[NTH Dungoan] Step 7 completed!");
            return new WorkflowResult
            {
                Success = true,
                Message = "Du Ngoan workflow completed successfully"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Dungoan] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Dungoan] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Find template using multi-scale matching
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
            Log($"[NTH Dungoan] WARNING: Template not found: {templateFileName}");
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
                var filteredResults = results.Where(r => r.Confidence >= actualThreshold).ToList();

                if (filteredResults.Count > 0)
                {
                    var best = GetBestMatch(filteredResults, templatePath);
                    Log($"[NTH Dungoan] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y})");
                    return best;
                }
            }

            await Task.Delay(50, cancellationToken);
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
