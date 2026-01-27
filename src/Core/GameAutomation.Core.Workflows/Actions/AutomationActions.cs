using System.Drawing;
using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Workflows.Helpers;

namespace GameAutomation.Core.Workflows.Actions;

/// <summary>
/// Static helper class for common automation actions
/// Easy to use without dependency injection
/// </summary>
public static class AutomationActions
{
    private static readonly Lazy<VisionService> _visionService = new(() => new VisionService());
    private static readonly Lazy<InputService> _inputService = new(() => new InputService());
    private static readonly Lazy<HumanLikeSimulator> _humanSimulator = new(() => new HumanLikeSimulator(_inputService.Value));

    public static IVisionService Vision => _visionService.Value;
    public static IInputService Input => _inputService.Value;
    public static HumanLikeSimulator Human => _humanSimulator.Value;

    /// <summary>
    /// Capture current screen
    /// </summary>
    public static Bitmap CaptureScreen(string? windowTitle = null)
    {
        return _visionService.Value.CaptureScreen(windowTitle);
    }

    /// <summary>
    /// Find template on screen
    /// </summary>
    public static List<DetectionResult> FindTemplate(string templatePath, double threshold = 0.8)
    {
        using var screenshot = CaptureScreen();
        return _visionService.Value.FindTemplate(screenshot, templatePath, threshold);
    }

    /// <summary>
    /// Find template and click at center
    /// </summary>
    public static DetectionResult? FindAndClick(string templatePath, double threshold = 0.8, int delayBeforeClick = 100)
    {
        var results = FindTemplate(templatePath, threshold);
        if (results.Count == 0)
            return null;

        var best = results[0];
        ClickAt(best.X + best.Width / 2, best.Y + best.Height / 2, delayBeforeClick);
        return best;
    }

    /// <summary>
    /// Wait for template to appear on screen
    /// </summary>
    public static DetectionResult? WaitForTemplate(string templatePath, double threshold = 0.8, int timeoutMs = 10000, int checkIntervalMs = 200)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var results = FindTemplate(templatePath, threshold);
            if (results.Count > 0)
                return results[0];

            Thread.Sleep(checkIntervalMs);
        }
        return null;
    }

    /// <summary>
    /// Wait for template and click when found
    /// </summary>
    public static DetectionResult? WaitAndClick(string templatePath, double threshold = 0.8, int timeoutMs = 10000, int delayBeforeClick = 100)
    {
        var result = WaitForTemplate(templatePath, threshold, timeoutMs);
        if (result == null)
            return null;

        ClickAt(result.X + result.Width / 2, result.Y + result.Height / 2, delayBeforeClick);
        return result;
    }

    /// <summary>
    /// Move mouse to position
    /// </summary>
    public static void MoveTo(int x, int y)
    {
        _inputService.Value.MoveMouse(x, y);
    }

    /// <summary>
    /// Click at position
    /// </summary>
    public static void ClickAt(int x, int y, int delayBeforeClick = 0)
    {
        _inputService.Value.MoveMouse(x, y);
        if (delayBeforeClick > 0)
            Thread.Sleep(delayBeforeClick);
        _inputService.Value.LeftClick();
    }

    /// <summary>
    /// Click at current mouse position
    /// </summary>
    public static void Click()
    {
        _inputService.Value.LeftClick();
    }

    /// <summary>
    /// Right click at position
    /// </summary>
    public static void RightClickAt(int x, int y)
    {
        _inputService.Value.MoveMouse(x, y);
        Thread.Sleep(50);
        _inputService.Value.RightClick();
    }

    /// <summary>
    /// Double click at position
    /// </summary>
    public static void DoubleClickAt(int x, int y)
    {
        _inputService.Value.MoveMouse(x, y);
        Thread.Sleep(50);
        _inputService.Value.DoubleClick();
    }

    /// <summary>
    /// Type text
    /// </summary>
    public static void Type(string text)
    {
        _inputService.Value.TypeText(text);
    }

    /// <summary>
    /// Press key
    /// </summary>
    public static void PressKey(VirtualKeyCode key)
    {
        _inputService.Value.KeyPress(key);
    }

    /// <summary>
    /// Press key combination (e.g., Ctrl+C)
    /// </summary>
    public static void PressKeys(params VirtualKeyCode[] keys)
    {
        _inputService.Value.KeyCombination(keys);
    }

    /// <summary>
    /// Delay execution
    /// </summary>
    public static void Wait(int ms)
    {
        Thread.Sleep(ms);
    }

    // ==================== HUMAN-LIKE ACTIONS ====================

    /// <summary>
    /// Move mouse like human (Bezier curve + ease-in-out)
    /// </summary>
    public static async Task MoveToHumanAsync(int x, int y)
    {
        await _humanSimulator.Value.MoveMouseAsync(x, y);
    }

    /// <summary>
    /// Click at position with human-like movement
    /// </summary>
    public static async Task ClickAtHumanAsync(int x, int y)
    {
        await _humanSimulator.Value.MoveMouseAsync(x, y);
        await _humanSimulator.Value.LeftClickAsync();
    }

    /// <summary>
    /// Find template and click with human-like movement
    /// </summary>
    public static async Task<DetectionResult?> FindAndClickHumanAsync(string templatePath, double threshold = 0.8)
    {
        var results = FindTemplate(templatePath, threshold);
        if (results.Count == 0)
            return null;

        var best = results[0];
        int centerX = best.X + best.Width / 2;
        int centerY = best.Y + best.Height / 2;

        await _humanSimulator.Value.MoveMouseAsync(centerX, centerY);
        await _humanSimulator.Value.LeftClickAsync();

        return best;
    }

    /// <summary>
    /// Wait for template and click with human-like movement
    /// </summary>
    public static async Task<DetectionResult?> WaitAndClickHumanAsync(string templatePath, double threshold = 0.8, int timeoutMs = 10000)
    {
        var result = WaitForTemplate(templatePath, threshold, timeoutMs);
        if (result == null)
            return null;

        int centerX = result.X + result.Width / 2;
        int centerY = result.Y + result.Height / 2;

        await _humanSimulator.Value.MoveMouseAsync(centerX, centerY);
        await _humanSimulator.Value.LeftClickAsync();

        return result;
    }

    /// <summary>
    /// Type text like human (variable speed, optional errors)
    /// </summary>
    public static async Task TypeHumanAsync(string text, bool withErrors = false)
    {
        if (withErrors)
            await _humanSimulator.Value.TypeTextWithErrorsAsync(text);
        else
            await _humanSimulator.Value.TypeTextAsync(text);
    }

    /// <summary>
    /// Double click with human-like movement
    /// </summary>
    public static async Task DoubleClickAtHumanAsync(int x, int y)
    {
        await _humanSimulator.Value.MoveMouseAsync(x, y);
        await _humanSimulator.Value.DoubleClickAsync();
    }

    /// <summary>
    /// Drag and drop with human-like movement
    /// </summary>
    public static async Task DragAndDropHumanAsync(int startX, int startY, int endX, int endY)
    {
        await _humanSimulator.Value.DragAndDropAsync(startX, startY, endX, endY);
    }

    /// <summary>
    /// Random delay to simulate thinking
    /// </summary>
    public static async Task ThinkAsync(int minMs = 500, int maxMs = 2000)
    {
        await _humanSimulator.Value.ThinkAsync(minMs, maxMs);
    }
}
