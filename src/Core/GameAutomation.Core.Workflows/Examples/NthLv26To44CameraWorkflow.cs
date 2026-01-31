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
/// NTH Game Lv26 to Lv44 Camera Workflow
/// Flow: Skip tale -> Open camera -> Take photo -> Close camera
/// Uses multi-scale template matching for all images
/// </summary>
public class NthLv26To44CameraWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;
    private readonly Action<string>? _logger;

    // Template file names for camera flow
    private const string SkipTaleButtonTemplate = "00_skiptalebutton.png";
    private const string SkipTaleButton2Template = "00_skiptalebutton2.png";
    private const string SkipTaleButton3Template = "00_skiptalebutton3.png";
    private const string SkipTaleButton4Template = "00_skiptalebutton4.png";
    private const string SkipTaleButton5Template = "00_skiptalebutton5.png";

    private const string MenuButtonTemplate = "01_menubutton.png";
    private const string CameraButtonTemplate = "02_camerabutton.png";
    private const string CameraEnterButtonTemplate = "03_camera_enterbutton.png";
    private const string CameraSkipButtonTemplate = "04_camera_skipbutton.png";
    private const string CameraSkipButton2Template = "04_camera_skipbutton2.png";
    private const string CameraCloseImageTemplate = "05_camera_closeimage.png";
    private const string CameraBackButtonTemplate = "06_camera_backbutton.png";

    // Multi-scale settings - cho phép tìm ảnh ở nhiều kích thước
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    // Delays
    private const int AfterTypeDelayMs = 1000;  // Chờ 2s sau khi nhập text
    private const int ShortDelayMs = 200;       // Chờ ngắn 0.5s
    private const int MediumDelayMs = 500;     // Chờ vừa 1s
    private const int LongDelayMs = 1000;       // Chờ dài 2s

    public string Name => "NTH Lv26-44 Camera Flow";
    public string Description => "Quy trình camera cho game NTH từ level 26 đến 44";

    public NthLv26To44CameraWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string? assetsPath = null,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = assetsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "nth", "ingame_camera");
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    /// <summary>
    /// Execute the camera workflow (Step 2 of lv26-44 flow)
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("[NTH Camera] Starting Step 2: Camera workflow...");

            // Get screen dimensions for center click
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Cannot get primary screen"
                };
            }
            var screenWidth = primaryScreen.Bounds.Width;
            var screenHeight = primaryScreen.Bounds.Height;
            int centerX = screenWidth / 2;
            int centerY = screenHeight / 2;

            // ===== BƯỚC 2.1: Chờ tìm skip tale button và click liên tục =====
            Log("[NTH Camera] Step 2.1: Looking for skip tale button (5s timeout)...");
            await HandleSkipTaleButtonAsync(cancellationToken);

            // ===== BƯỚC 2.2: Tìm và click camera button =====
            Log("[NTH Camera] Step 2.2: Finding camera button...");
            var cameraButton = await WaitAndClickMultiScaleAsync(
                CameraButtonTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (cameraButton == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Camera button not found"
                };
            }
            await Task.Delay(MediumDelayMs, cancellationToken);

            // ===== BƯỚC 2.3: Click vào giữa màn hình 3 lần =====
            Log("[NTH Camera] Step 2.3: Clicking center of screen 3 times...");
            await ClickCenterScreenMultipleTimesAsync(centerX, centerY, 3, MediumDelayMs, cancellationToken);

            // ===== BƯỚC 2.4: Chờ 2s xem có skip button không =====
            Log("[NTH Camera] Step 2.4: Waiting 2s to check for skip button...");
            var skipButton1 = await FindMultiScaleAsync(
                CameraSkipButtonTemplate,
                timeoutMs: LongDelayMs,
                cancellationToken);

            if (skipButton1 != null)
            {
                Log("[NTH Camera] Skip button found, clicking and pressing Space...");
                await ClickAtCenterAsync(skipButton1);
                await Task.Delay(ShortDelayMs, cancellationToken);
                await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
                await Task.Delay(MediumDelayMs, cancellationToken);
            }
            else
            {
                Log("[NTH Camera] No skip button found, continuing...");
            }

            // ===== BƯỚC 2.5: Chờ camera enter button, ấn Enter, xử lý skip button =====
            Log("[NTH Camera] Step 2.5: Waiting for camera enter button...");
            var enterButton = await FindMultiScaleAsync(
                CameraEnterButtonTemplate,
                timeoutMs: 15000,
                cancellationToken);

            if (enterButton != null)
            {
                Log("[NTH Camera] Camera enter button found, pressing Enter...");
                await _humanSim.KeyPressAsync(VirtualKeyCode.RETURN);
                await Task.Delay(MediumDelayMs, cancellationToken);

                // Chờ xem có skip button không (timeout 2s)
                Log("[NTH Camera] Checking for camera skip button (2s timeout)...");
                var skipButton2 = await FindMultiScaleAsync(
                    CameraSkipButtonTemplate,
                    timeoutMs: LongDelayMs,
                    cancellationToken);
                if (skipButton2 == null)
                {
                    skipButton2 = await FindMultiScaleAsync(
                    CameraSkipButton2Template,
                    timeoutMs: LongDelayMs,
                    cancellationToken);
                }
                if (skipButton2 != null)
                {
                    Log("[NTH Camera] Skip button found, clicking and pressing Space...");
                    await ClickAtCenterAsync(skipButton2);
                    await Task.Delay(ShortDelayMs, cancellationToken);
                    await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
                    await Task.Delay(MediumDelayMs, cancellationToken);
                }
                else
                {
                    Log("[NTH Camera] No skip button found, continuing...");
                }
            }
            else
            {
                Log("[NTH Camera] Warning: Camera enter button not found, continuing...");
            }

            // ===== BƯỚC 2.6: Tìm và click close image button =====
            Log("[NTH Camera] Step 2.6: Finding camera close image button...");
            var closeImage = await WaitAndClickMultiScaleAsync(
                CameraCloseImageTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (closeImage == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Camera close image button not found"
                };
            }
            await Task.Delay(MediumDelayMs, cancellationToken);

            // ===== BƯỚC 2.7: Chờ và click back button =====
            Log("[NTH Camera] Step 2.7: Waiting for camera back button...");
            var backButton = await WaitAndClickMultiScaleAsync(
                CameraBackButtonTemplate,
                timeoutMs: 10000,
                cancellationToken);

            if (backButton == null)
            {
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Camera back button not found"
                };
            }
            await Task.Delay(MediumDelayMs, cancellationToken);

            // ===== BƯỚC 2.8: Click vào giữa màn hình 3 lần =====
            Log("[NTH Camera] Step 2.8: Clicking center of screen 3 times...");
            await ClickCenterScreenMultipleTimesAsync(centerX, centerY, 3, MediumDelayMs, cancellationToken);

            // ===== BƯỚC 2.9: Chờ 2s =====
            Log("[NTH Camera] Step 2.9: Waiting 2 seconds...");
            await Task.Delay(LongDelayMs, cancellationToken);

            Log("[NTH Camera] Step 2 completed successfully!");
            return new WorkflowResult
            {
                Success = true,
                Message = "Camera workflow completed successfully"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Camera] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Camera] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Handle skip tale button - click repeatedly until menu button appears
    /// </summary>
    private async Task HandleSkipTaleButtonAsync(CancellationToken cancellationToken)
    {
        // Chờ 2s tìm skip tale button
        var skipTale = await FindMultiScaleAsync(SkipTaleButtonTemplate, timeoutMs: 1000, cancellationToken);

        if (skipTale == null)
        {
            Log("[NTH Camera] Skip tale button not found, skipping...");
            return;
        }

        Log("[NTH Camera] Skip tale button found, clicking repeatedly...");

        // Di chuyển chuột tới và click
        int clickX = skipTale.X + skipTale.Width / 2;
        int clickY = skipTale.Y + skipTale.Height / 2;
        await _humanSim.MoveMouseAsync(clickX, clickY);
        await _humanSim.LeftClickAsync();
        await Task.Delay(ShortDelayMs, cancellationToken);
        await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);

        // Click mỗi giây cho tới khi qua doan lung tung
        var startTime = DateTime.Now;
        var maxDuration = TimeSpan.FromSeconds(30); // Giới hạn 30s

        while (DateTime.Now - startTime < maxDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var skipbutton3 = await FindMultiScaleAsync(SkipTaleButton3Template, timeoutMs: 200, cancellationToken);
            var skipbutton4 = await FindMultiScaleAsync(SkipTaleButton4Template, timeoutMs: 200, cancellationToken);
            var skipbutton5 = await FindMultiScaleAsync(SkipTaleButton5Template, timeoutMs: 200, cancellationToken);
            // Kiểm tra xem menu button đã xuất hiện chưa
            //var menuButton = await FindMultiScaleAsync(MenuButtonTemplate, timeoutMs: 500, cancellationToken);
            //if (menuButton != null)
            //{
            //    Log("[NTH Camera] Menu button appeared, stop clicking skip tale");
            //    break;
            //}
            if (skipbutton3 != null)
            {
                clickX = skipbutton3.X + skipbutton3.Width / 2;
                clickY = skipbutton3.X + skipbutton3.Height / 2;
                await _humanSim.MoveMouseAsync(clickX, clickY);
            }
            else if (skipbutton4 != null)
            {
                clickX = skipbutton4.X + skipbutton4.Width / 2;
                clickY = skipbutton4.X + skipbutton4.Height / 2;
                await _humanSim.MoveMouseAsync(clickX, clickY);
            }
            else if (skipbutton5 != null)
            {
                clickX = skipbutton5.X + skipbutton5.Width / 2;
                clickY = skipbutton5.X + skipbutton5.Height / 2;
                await _humanSim.MoveMouseAsync(clickX, clickY);
            }
            else
            {
                break;
            }
            await _humanSim.LeftClickAsync();
            await Task.Delay(ShortDelayMs, cancellationToken);
        }
        // Kiểm tra skip tale còn xuất hiện không
        var currentSkipTale = await FindMultiScaleAsync(SkipTaleButtonTemplate, timeoutMs: 500, cancellationToken);
        if (currentSkipTale != null)
        {
            // Di chuyển và click vào vị trí mới của skip tale
            clickX = currentSkipTale.X + currentSkipTale.Width / 2;
            clickY = currentSkipTale.Y + currentSkipTale.Height / 2;
            await _humanSim.MoveMouseAsync(clickX, clickY);
        }

        // Click tại vị trí hiện tại
        await _humanSim.LeftClickAsync();
        await Task.Delay(ShortDelayMs, cancellationToken);
        await _humanSim.KeyPressAsync(VirtualKeyCode.SPACE);
        await Task.Delay(MediumDelayMs, cancellationToken);
    }

    /// <summary>
    /// Click at center of screen multiple times with delay
    /// </summary>
    private async Task ClickCenterScreenMultipleTimesAsync(
        int centerX,
        int centerY,
        int times,
        int delayMs,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < times; i++)
        {
            Log($"[NTH Camera] Clicking center ({i + 1}/{times})...");
            await _humanSim.MoveMouseAsync(centerX, centerY);
            await _humanSim.LeftClickAsync();

            if (i < times - 1)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
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
            Log($"[NTH Camera] WARNING: Template not found: {templateFileName}");
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
                Log($"[NTH Camera] Found {templateFileName} - Confidence: {best.Confidence:P1} at ({best.X}, {best.Y})");
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
