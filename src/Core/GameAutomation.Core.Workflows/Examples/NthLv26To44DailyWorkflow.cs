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
/// NTH Game Lv26 to Lv44 Daily Quest Workflow - Step 6
/// Flow: Open daily menu -> Complete daily tasks -> Reset if needed
/// Uses multi-scale template matching for all images
/// If any step fails to find image, skip to next step
/// </summary>
public class NthLv26To44DailyWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for Daily flow
    private const string OpenButtonTemplate = "01_daily_openbutton.png";
    private const string MainTaleTemplate = "02_daily_maintale.png";
    private const string AdventureTemplate = "03_daily_adventure.png";
    private const string PhotographTemplate = "04_daily_photograph.png";
    private const string ResetTemplate = "05_daily_reset.png";
    private const string Reset2Template = "05_daily_reset_2.png";
    private const string MhlTemplate = "06_daily_mhl.png";
    private const string Mhl2Template = "06_daily_mhl_2.png";  // Vung can tranh khi tim reset
    private const string DungoanTemplate = "07_daily_dungoan.png";

    // "Done" template file names - khi tat ca 4 anh nay xuat hien thi co the chuyen sang tim dungoan
    private const string MainTaleDoneTemplate = "02_daily_maintale_done.png";
    private const string AdventureDoneTemplate = "03_daily_adventure_done.png";
    private const string PhotographDoneTemplate = "04_daily_photograph_done.png";
    private const string MhlDoneTemplate = "06_daily_mhl_done.png";

    // Multi-scale settings
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterClickDelayMs = 500;  // 0.5s sau moi click
    private const int SearchTimeoutMs = 500;     // 0.5s timeout tim anh

    public string Name => "NTH Lv26-44 Daily Quest";
    public string Description => "Quy trinh nhiem vu hang ngay cho game NTH tu level 26 den 44 (Step 6)";

    public NthLv26To44DailyWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "ingame_daily");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the Daily Quest workflow (Step 6 of lv26-44 flow)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH Daily] Starting Step 6: Daily Quest workflow...");

            // ===== BUOC 6.1: Tim nut mo daily, an Esc neu k thay (toi da 5 lan) =====
            Log("[NTH Daily] Step 6.1: Looking for daily open button...");
            DetectionResult? openButton = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                openButton = await FindMultiScaleAsync(
                    OpenButtonTemplate,
                    timeoutMs: 2000,
                    cancellationToken);

                if (openButton != null)
                {
                    Log($"[NTH Daily] Open button found on attempt {attempt + 1}");
                    break;
                }

                Log($"[NTH Daily] Open button not found, pressing Esc (attempt {attempt + 1}/5)...");
                await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
                await Task.Delay(AfterClickDelayMs, cancellationToken);
            }

            if (openButton == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Daily open button not found after 5 attempts"
                };
            }

            // Click vao nut mo
            Log("[NTH Daily] Clicking open button...");
            await ClickAtCenterAsync(openButton);
            await Task.Delay(AfterClickDelayMs, cancellationToken);

            // ===== BUOC 6.2: Vong lap tim va click cac nhiem vu =====
            Log("[NTH Daily] Step 6.2: Starting daily quest loop...");

            int questClickCount = 0;
            const int requiredQuestClicks = 4;
            bool allQuestsDone = false;

            // Vong lap chinh: tim cac nhiem vu hoac reset cho toi khi:
            // - Click du 4 quest HOAC
            // - Tim thay du 4 anh "done"
            while (questClickCount < requiredQuestClicks && !allQuestsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Kiem tra xem da co du 4 anh "done" chua
                allQuestsDone = await CheckAllQuestsDoneAsync(cancellationToken);
                if (allQuestsDone)
                {
                    Log("[NTH Daily] All 4 quest done images found! Moving to dungoan search...");
                    break;
                }

                // Tim 1 trong cac anh nhiem vu
                var questResult = await FindFirstQuestAsync(cancellationToken);

                if (questResult != null)
                {
                    questClickCount++;
                    Log($"[NTH Daily] Found quest, clicking... (Quest {questClickCount}/{requiredQuestClicks})");
                    await ClickAtCenterAsync(questResult);
                    await Task.Delay(AfterClickDelayMs, cancellationToken);
                    // Tiep tuc vong lap, tim nhiem vu tiep
                    continue;
                }

                // Khong tim thay nhiem vu nao -> tim va click tat ca reset buttons
                Log("[NTH Daily] No quest found, looking for reset buttons...");
                var clickedReset = await FindAndClickAllResetButtonsAsync(cancellationToken);

                if (!clickedReset)
                {
                    Log("[NTH Daily] No reset button found, breaking loop...");
                    break;
                }
            }

            Log($"[NTH Daily] Quest click phase completed. Quests clicked: {questClickCount}, All done: {allQuestsDone}");

            // ===== BUOC 6.3: Tim 07_daily_dungoan =====
            Log("[NTH Daily] Step 6.3: Looking for dungoan button...");

            bool dungoanFound = false;
            int dungoanAttempts = 0;
            const int maxDungoanAttempts = 10;

            while (!dungoanFound && dungoanAttempts < maxDungoanAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dungoanAttempts++;

                var dungoan = await FindMultiScaleAsync(
                    DungoanTemplate,
                    timeoutMs: SearchTimeoutMs,
                    cancellationToken);

                if (dungoan != null)
                {
                    Log("[NTH Daily] Dungoan button found, clicking...");
                    await ClickAtCenterAsync(dungoan);
                    await Task.Delay(AfterClickDelayMs, cancellationToken);
                    dungoanFound = true;
                    break;
                }

                // Khong thay dungoan -> tim va click tat ca reset buttons
                Log($"[NTH Daily] Dungoan not found, looking for reset (attempt {dungoanAttempts}/{maxDungoanAttempts})...");
                var clickedReset = await FindAndClickAllResetButtonsAsync(cancellationToken);

                if (!clickedReset)
                {
                    Log("[NTH Daily] No reset button found, trying again...");
                    await Task.Delay(AfterClickDelayMs, cancellationToken);
                }
            }

            Log("[NTH Daily] Step 6 completed!");
            return new WorkflowResult
            {
                Success = true,
                Message = dungoanFound
                    ? "Daily Quest workflow completed - dungoan found!"
                    : "Daily Quest workflow completed"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Daily] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Daily] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Find and click all reset buttons (05_daily_reset and 05_daily_reset_2)
    /// Returns true if at least one reset was clicked
    /// </summary>
    private async Task<bool> FindAndClickAllResetButtonsAsync(CancellationToken cancellationToken)
    {
        var allResets = new System.Collections.Generic.List<DetectionResult>();

        using var screenshot = _visionService.CaptureScreen();

        // Tim 05_daily_reset
        var resetPath = Path.Combine(_assetsPath, ResetTemplate);
        if (File.Exists(resetPath))
        {
            var results = _visionService.FindTemplateMultiScale(
                screenshot,
                resetPath,
                MatchThreshold + 0.1,
                MinScale,
                MaxScale,
                ScaleSteps);

            allResets.AddRange(results.Where(r => r.Confidence >= MatchThreshold));
        }

        // Tim 05_daily_reset_2
        var reset2Path = Path.Combine(_assetsPath, Reset2Template);
        if (File.Exists(reset2Path))
        {
            var results2 = _visionService.FindTemplateMultiScale(
                screenshot,
                reset2Path,
                MatchThreshold + 0.1,
                MinScale,
                MaxScale,
                ScaleSteps);

            allResets.AddRange(results2.Where(r => r.Confidence >= MatchThreshold));
        }

        if (allResets.Count == 0)
        {
            return false;
        }

        // Sap xep theo vi tri Y (tu tren xuong duoi) de click co thu tu
        var sortedResets = allResets.OrderBy(r => r.Y).ThenBy(r => r.X).ToList();

        Log($"[NTH Daily] Found {sortedResets.Count} reset button(s), clicking all...");

        // Click lan luot vao tat ca cac reset button
        foreach (var reset in sortedResets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log($"[NTH Daily] Clicking reset at ({reset.X}, {reset.Y}) - Confidence: {reset.Confidence:P1}");
            await ClickAtCenterAsync(reset);
            await Task.Delay(AfterClickDelayMs, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Check if all 4 quest "done" images are present on screen
    /// Returns true if all 4 done images are found
    /// </summary>
    private async Task<bool> CheckAllQuestsDoneAsync(CancellationToken cancellationToken)
    {
        var doneTemplates = new[]
        {
            MainTaleDoneTemplate,
            AdventureDoneTemplate,
            PhotographDoneTemplate,
            MhlDoneTemplate
        };

        int foundCount = 0;

        foreach (var template in doneTemplates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var templatePath = Path.Combine(_assetsPath, template);
            if (!File.Exists(templatePath))
            {
                // Neu file khong ton tai, bo qua (co the chua tao anh done)
                continue;
            }

            var result = await FindMultiScaleAsync(
                template,
                timeoutMs: 200,  // Tim nhanh
                cancellationToken,
                0.92);

            if (result != null)
            {
                foundCount++;
                Log($"[NTH Daily] Found done image: {template}");
            }
        }

        Log($"[NTH Daily] Done images found: {foundCount}/4");
        return foundCount >= 4;
    }

    /// <summary>
    /// Find first available quest from multiple templates
    /// Returns the first one found (maintale, adventure, photograph, mhl)
    /// </summary>
    private async Task<DetectionResult?> FindFirstQuestAsync(CancellationToken cancellationToken)
    {
        var questTemplates = new[]
        {
            MainTaleTemplate,
            AdventureTemplate,
            PhotographTemplate,
            MhlTemplate
        };

        foreach (var template in questTemplates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await FindMultiScaleAsync(
                template,
                timeoutMs: SearchTimeoutMs,
                cancellationToken);

            if (result != null)
            {
                Log($"[NTH Daily] Found quest: {template}");
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Find template using multi-scale matching
    /// Returns best match (highest confidence, closest to original size)
    /// </summary>
    private async Task<DetectionResult?> FindMultiScaleAsync(
        string templateFileName,
        int timeoutMs = 500,
        CancellationToken cancellationToken = default,
        double? threshold = null)
    {
        var templatePath = Path.Combine(_assetsPath, templateFileName);
        if (!File.Exists(templatePath))
        {
            Log($"[NTH Daily] WARNING: Template not found: {templateFileName}");
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
                    Log($"[NTH Daily] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y})");
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
