using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameAutomation.Core.Workflows.Examples;

/// <summary>
/// NTH Game Full Flow Workflow
/// 
/// Combines all steps: Sign-In -> Camera -> MHL -> Combat -> Map -> Daily -> Dungoan -> Signout
/// </summary>
public class NthFullFlowWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly string _templatesFolder;
    private readonly Action<string>? _logger;

    // Excel input parameters
    private readonly string? _sheetName;
    private readonly int _startRow;

    public string Name => "NTH Full Flow";
    public string Description => "Quy trinh day du cho game NTH: Sign-In -> Camera -> MHL -> Combat -> Map -> Daily -> Dungoan -> Signout";

    public NthFullFlowWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string templatesFolder,
        string? sheetName = null,
        int startRow = 1,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _templatesFolder = templatesFolder;
        _sheetName = sheetName;
        _startRow = startRow;
        _logger = logger;
    }

    public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

    public async Task<WorkflowResult> ExecuteAsync(
        GameContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log("========================================");
            Log("[NTH Full Flow] Starting complete game workflow...");
            Log("========================================");

            // ===== STEP 1: Sign-In =====
            Log("\n[STEP 1/9] Sign-In...");
            var signinWorkflow = new NthSigninWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "signin"),
                logger: _logger);

            var signinResult = await signinWorkflow.ExecuteAsync(context, cancellationToken, _sheetName, _startRow);
            if (!signinResult.Success)
            {
                Log($"[NTH Full Flow] Sign-In failed: {signinResult.Message}");
                return signinResult;
            }
            Log("[STEP 1/9] Sign-In completed!");

            // ===== WAIT FOR DAILY OPEN BUTTON =====
            Log("\n[WAITING] Waiting for daily open button to appear (max 60s)...");
            var dailyOpenButtonPath = Path.Combine(_templatesFolder, "nth", "ingame_daily", "01_daily_openbutton.png");
            var dailyButtonFound = await WaitForImageAsync(dailyOpenButtonPath, 60000, cancellationToken);
            if (!dailyButtonFound)
            {
                Log("[NTH Full Flow] Daily open button not found after 60s, continuing anyway...");
            }
            else
            {
                Log("[NTH Full Flow] Daily open button detected, proceeding...");
            }

            // ===== STEP 2: Camera =====
            Log("\n[STEP 2/9] Camera...");
            var cameraWorkflow = new NthLv26To44CameraWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_camera"),
                logger: _logger);

            var cameraResult = await cameraWorkflow.ExecuteAsync(context, cancellationToken);
            if (!cameraResult.Success)
            {
                Log($"[NTH Full Flow] Camera failed: {cameraResult.Message}, continuing...");
            }
            else
            {
                Log("[STEP 2/9] Camera completed!");
            }

            // ===== STEP 3: MHL (Mong Hoa Luc) =====
            Log("\n[STEP 3/9] Mong Hoa Luc...");
            var mhlWorkflow = new NthLv26To44MongHoaLucWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_monghoaluc"),
                logger: _logger);

            var mhlResult = await mhlWorkflow.ExecuteAsync(context, cancellationToken);
            if (!mhlResult.Success)
            {
                Log($"[NTH Full Flow] MHL failed: {mhlResult.Message}, continuing...");
            }
            else
            {
                Log("[STEP 3/9] MHL completed!");
            }

            // ===== STEP 4: Combat =====
            Log("\n[STEP 4/9] Setting Combat...");
            var combatWorkflow = new NthLv26To44SettingCombatWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_settingcombat"),
                logger: _logger);

            var combatResult = await combatWorkflow.ExecuteAsync(context, cancellationToken);
            if (!combatResult.Success)
            {
                Log($"[NTH Full Flow] Combat failed: {combatResult.Message}, continuing...");
            }
            else
            {
                Log("[STEP 4/9] Combat completed!");
            }

            // ===== STEP 5: Map =====
            Log("\n[STEP 5/9] Map Navigation...");
            var mapWorkflow = new NthLv26To44MapWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_map"),
                logger: _logger);

            var mapResult = await mapWorkflow.ExecuteAsync(context, cancellationToken);
            if (!mapResult.Success)
            {
                Log($"[NTH Full Flow] Map failed: {mapResult.Message}, continuing...");
            }
            else
            {
                Log("[STEP 5/9] Map completed!");
            }

            // ===== STEP 6: Daily =====
            Log("\n[STEP 6/9] Daily Quest...");
            var dailyWorkflow = new NthLv26To44DailyWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_daily"),
                logger: _logger);

            var dailyResult = await dailyWorkflow.ExecuteAsync(context, cancellationToken);
            if (!dailyResult.Success)
            {
                Log($"[NTH Full Flow] Daily failed: {dailyResult.Message}, continuing...");
            }
            else
            {
                Log("[STEP 6/9] Daily completed!");
            }

            // ===== STEP 7: Dungoan =====
            Log("\n[STEP 7/9] Du Ngoan...");
            var dungoanWorkflow = new NthDungoanWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "ingame_dungoan"),
                logger: _logger);

            var dungoanResult = await dungoanWorkflow.ExecuteAsync(context, cancellationToken);
            if (!dungoanResult.Success)
            {
                Log($"[NTH Full Flow] Dungoan failed: {dungoanResult.Message}, continuing...");
            }
            else
            {
                Log("[STEP 7/9] Dungoan completed!");
            }

            // ===== STEP 8: Signout =====
            Log("\n[STEP 8/9] Signout...");
            var signoutWorkflow = new NthSignoutWorkflow(
                _visionService,
                _inputService,
                assetsPath: Path.Combine(_templatesFolder, "nth", "signout"),
                logger: _logger);

            var signoutResult = await signoutWorkflow.ExecuteAsync(context, cancellationToken);
            if (!signoutResult.Success)
            {
                Log($"[NTH Full Flow] Signout failed: {signoutResult.Message}");
            }
            else
            {
                Log("[STEP 8/9] Signout completed!");
            }

            Log("\n========================================");
            Log("[NTH Full Flow] ALL STEPS COMPLETED!");
            Log("========================================");

            return new WorkflowResult
            {
                Success = true,
                Message = "Full flow completed successfully"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[NTH Full Flow] Workflow cancelled by user");
            return new WorkflowResult
            {
                Success = false,
                Message = "Workflow cancelled"
            };
        }
        catch (Exception ex)
        {
            Log($"[NTH Full Flow] Error: {ex.Message}");
            return new WorkflowResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private void Log(string message)
    {
        _logger?.Invoke(message);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Wait for an image to appear on screen
    /// </summary>
    private async Task<bool> WaitForImageAsync(
        string templatePath,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(templatePath))
        {
            Log($"[NTH Full Flow] WARNING: Template not found: {templatePath}");
            return false;
        }

        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);
        const double threshold = 0.7;
        const double minScale = 0.5;
        const double maxScale = 1.5;
        const int scaleSteps = 15;

        while (DateTime.Now < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplateMultiScale(
                screenshot,
                templatePath,
                threshold,
                minScale,
                maxScale,
                scaleSteps);

            if (results.Count > 0 && results.Any(r => r.Confidence >= threshold))
            {
                return true;
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }
}
