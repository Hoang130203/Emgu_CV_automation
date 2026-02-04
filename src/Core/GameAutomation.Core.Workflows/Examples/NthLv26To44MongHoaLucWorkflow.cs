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
/// NTH Game Lv26 to Lv44 Mộng Hoa Lục (MHL) Workflow - Step 3
/// Flow: Open mailbox -> Navigate to discover -> Like posts -> Exit
/// Uses multi-scale template matching for all images
/// </summary>
public class NthLv26To44MongHoaLucWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for MHL flow
    private const string MailboxTemplate = "01_mhl_mailbox.png";
    private const string Mailbox2Template = "01_mhl_mailbox2.png";
    private const string BackButtonTemplate = "02_mhl_backbuttonn.png";
    private const string PostTabTemplate = "03_mhl_posttab.png";
    private const string DiscoverTemplate = "04_mhl_discover.png";
    private const string LikeButtonTemplate = "05_mhl_like.png";

    // Multi-scale settings - cho phép tìm ảnh ở nhiều kích thước
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterClickDelayMs = 500;  // Chờ 0.5s sau khi click
    private const int ShortDelayMs = 500;       // Chờ ngắn 0.5s
    private const int MediumDelayMs = 1000;     // Chờ vừa 1s

    public string Name => "NTH Lv26-44 Mộng Hoa Lục";
    public string Description => "Quy trình Mộng Hoa Lục cho game NTH từ level 26 đến 44 (Step 3)";

    public NthLv26To44MongHoaLucWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "ingame_monghoaluc");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the Mộng Hoa Lục workflow (Step 3 of lv26-44 flow)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH MHL] Starting Step 3: Mộng Hoa Lục workflow...");

            // ===== BƯỚC 3.1: Tìm và click vào mailbox =====
            Log("[NTH MHL] Step 3.1: Looking for mailbox button (5s timeout)...");
            var mailbox = await WaitAndClickMultiScaleAsync(
                MailboxTemplate,
                timeoutMs: 5000,
                cancellationToken);
            if (mailbox == null)
            {
                // Thử template thay thế
                Log("[NTH MHL] Mailbox button not found, trying alternative template...");
                mailbox = await WaitAndClickMultiScaleAsync(
                    Mailbox2Template,
                    timeoutMs: 5000,
                    cancellationToken);
            }
            if (mailbox == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Mailbox button not found within 5 seconds"
                };
            }
            //await Task.Delay(AfterClickDelayMs, cancellationToken); // Chờ 2s sau khi click

            // ===== BƯỚC 3.2: Chờ và click back button =====
            Log("[NTH MHL] Step 3.2: Looking for back button...");
            var backButton = await WaitAndClickMultiScaleAsync(
                BackButtonTemplate,
                timeoutMs: 5000,
                cancellationToken);

            //if (backButton == null)
            //{
            //    return new WorkflowResult
            //    {
            //        Success = false,
            //        Message = "Back button not found"
            //    };
            //}
            //await Task.Delay(AfterClickDelayMs, cancellationToken); // Chờ 2s sau khi click

            // ===== BƯỚC 3.3: Chờ và click post tab =====
            Log("[NTH MHL] Step 3.3: Looking for post tab...");
            var postTab = await WaitAndClickMultiScaleAsync(
                PostTabTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (postTab == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Post tab not found"
                };
            }
            //await Task.Delay(AfterClickDelayMs, cancellationToken); // Chờ 2s sau khi click

            // ===== BƯỚC 3.4: Chờ và click discover =====
            Log("[NTH MHL] Step 3.4: Looking for discover button...");
            var discover = await WaitAndClickMultiScaleAsync(
                DiscoverTemplate,
                timeoutMs: 5000,
                cancellationToken);

            if (discover == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Discover button not found"
                };
            }
            await Task.Delay(AfterClickDelayMs, cancellationToken); // Chờ 2s sau khi click

            // ===== BƯỚC 3.5: Chờ và click like 2 lần, cách nhau 1 giây =====
            Log("[NTH MHL] Step 3.5: Looking for like button (will click 2 times)...");
            var likeButton = await FindMultiScaleAsync(
                LikeButtonTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (likeButton == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Like button not found"
                };
            }

            // Click lần 1
            Log("[NTH MHL] Clicking like button (1/2)...");
            await ClickAtCenterAsync(likeButton);
            await Task.Delay(MediumDelayMs, cancellationToken); // Chờ 1s

            // Click lần 2
            Log("[NTH MHL] Clicking like button (2/2)...");
            // Tìm lại like button (vị trí có thể thay đổi)
            var likeButton2 = await FindMultiScaleAsync(
                LikeButtonTemplate,
                timeoutMs: 2000,
                cancellationToken);

            if (likeButton2 != null)
            {
                await ClickAtCenterAsync(likeButton2);
            }
            else
            {
                // Click lại vị trí cũ nếu không tìm thấy
                await ClickAtCenterAsync(likeButton);
            }

            // ===== BƯỚC 3.6: Chờ 0.5s và ấn Esc =====
            Log("[NTH MHL] Step 3.6: Waiting 0.5s then pressing Escape...");
            await Task.Delay(ShortDelayMs, cancellationToken); // Chờ 0.5s
            await _humanSim.KeyPressAsync(VirtualKeyCode.ESCAPE);
            await Task.Delay(MediumDelayMs, cancellationToken); // Chờ thêm 1s sau khi ấn Esc

            Log("[NTH MHL] Step 3 completed successfully!");
            return new WorkflowResult
            {
                Success = true,
                Message = "Mộng Hoa Lục workflow completed successfully"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH MHL] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH MHL] Error: {ex.Message}");
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
            Log($"[NTH MHL] WARNING: Template not found: {templateFileName}");
            return null;
        }

        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);

        // Get search region using GetEffectiveRegion (respects UseRegionSearch setting)
        var templateKey = $"mhl/{Path.GetFileNameWithoutExtension(templateFileName)}";
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
                Log($"[NTH MHL] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y}){regionInfo}");
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
