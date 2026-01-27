using System.Drawing;
using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;

namespace GameAutomation.Core.Workflows.Actions;

/// <summary>
/// Action to find a template on screen and click at its center
/// </summary>
public class FindAndClickAction
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;

    public FindAndClickAction(IVisionService visionService, IInputService inputService)
    {
        _visionService = visionService;
        _inputService = inputService;
    }

    /// <summary>
    /// Find template and click at center
    /// </summary>
    /// <param name="templatePath">Path to template image</param>
    /// <param name="threshold">Match threshold (0.0 to 1.0)</param>
    /// <param name="delayBeforeClick">Delay in ms before clicking</param>
    /// <returns>Detection result if found, null if not found</returns>
    public DetectionResult? Execute(string templatePath, double threshold = 0.8, int delayBeforeClick = 100)
    {
        using var screenshot = _visionService.CaptureScreen();
        var results = _visionService.FindTemplate(screenshot, templatePath, threshold);

        if (results.Count == 0)
            return null;

        var best = results[0];
        int centerX = best.X + best.Width / 2;
        int centerY = best.Y + best.Height / 2;

        _inputService.MoveMouse(centerX, centerY);

        if (delayBeforeClick > 0)
            Thread.Sleep(delayBeforeClick);

        _inputService.LeftClick();

        return best;
    }

    /// <summary>
    /// Find template and click at center (async)
    /// </summary>
    public async Task<DetectionResult?> ExecuteAsync(string templatePath, double threshold = 0.8, int delayBeforeClick = 100)
    {
        return await Task.Run(() => Execute(templatePath, threshold, delayBeforeClick));
    }

    /// <summary>
    /// Find template, click and wait for another template to appear
    /// </summary>
    public DetectionResult? ExecuteAndWaitFor(
        string clickTemplatePath,
        string waitForTemplatePath,
        double threshold = 0.8,
        int timeoutMs = 5000,
        int checkIntervalMs = 200)
    {
        var clickResult = Execute(clickTemplatePath, threshold);
        if (clickResult == null)
            return null;

        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            using var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplate(screenshot, waitForTemplatePath, threshold);

            if (results.Count > 0)
                return results[0];

            Thread.Sleep(checkIntervalMs);
        }

        return null; // Timeout - wait template not found
    }

    /// <summary>
    /// Find all matches of template and click on each one
    /// </summary>
    public List<DetectionResult> ExecuteAll(string templatePath, double threshold = 0.8, int delayBetweenClicks = 200)
    {
        using var screenshot = _visionService.CaptureScreen();
        var results = _visionService.FindTemplate(screenshot, templatePath, threshold);

        foreach (var result in results)
        {
            int centerX = result.X + result.Width / 2;
            int centerY = result.Y + result.Height / 2;

            _inputService.MoveMouse(centerX, centerY);
            Thread.Sleep(50);
            _inputService.LeftClick();
            Thread.Sleep(delayBetweenClicks);
        }

        return results;
    }
}
