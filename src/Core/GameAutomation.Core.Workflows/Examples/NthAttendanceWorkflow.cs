using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Services.GoogleSheets;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GameAutomation.Core.Models.Vision.ImageResourceRegistry;

namespace GameAutomation.Core.Workflows.Examples;

/// <summary>
/// NTH Attendance (Diem Danh) Workflow
/// Simplified flow: Sign-in -> Wait for menu button -> Sign-out -> Mark attendance in Google Sheets
/// Loops through rows from Google Sheets, skipping already-attended rows.
/// </summary>
public class NthAttendanceWorkflow
{
    private readonly IVisionService _visionService;
    private readonly IInputService _inputService;
    private readonly string _templatesFolder;
    private readonly GoogleSheetsService _sheetsService;
    private readonly string _spreadsheetId;
    private readonly Action<string>? _logger;

    // Base date for column calculation: 12/2/2026 = Column G (index 6)
    private static readonly DateTime BaseDate = new(2026, 2, 12);
    private const int BaseColumnIndex = 6; // G = index 6 (0-based)

    // Menu button template (indicator that login succeeded and game loaded)
    private const string MenuButtonTemplate = "01_menubutton.png";

    // Multi-scale settings
    private const double MatchThreshold = 0.7;
    private const double MinScale = 0.5;
    private const double MaxScale = 1.5;
    private const int ScaleSteps = 15;

    public NthAttendanceWorkflow(
        IVisionService visionService,
        IInputService inputService,
        string templatesFolder,
        GoogleSheetsService sheetsService,
        string spreadsheetId,
        Action<string>? logger = null)
    {
        _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _templatesFolder = templatesFolder;
        _sheetsService = sheetsService ?? throw new ArgumentNullException(nameof(sheetsService));
        _spreadsheetId = spreadsheetId;
        _logger = logger;
    }

    /// <summary>
    /// Get the effective date (if before 5 AM, use yesterday)
    /// </summary>
    public static DateTime GetEffectiveDate()
    {
        return DateTime.Now.Hour < 5
            ? DateTime.Today.AddDays(-1)
            : DateTime.Today;
    }

    /// <summary>
    /// Calculate the column index for today's date
    /// </summary>
    public static int GetDateColumnIndex()
    {
        var effectiveDate = GetEffectiveDate();
        return BaseColumnIndex + (effectiveDate - BaseDate).Days;
    }

    /// <summary>
    /// Execute the attendance workflow
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        string sheetName,
        int startRow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveDate = GetEffectiveDate();
            var dateColIndex = GetDateColumnIndex();
            var dateColLetter = GoogleSheetsService.ColumnIndexToLetter(dateColIndex);

            Log($"[Diem Danh] Effective date: {effectiveDate:dd/MM/yyyy}");
            Log($"[Diem Danh] Date column: {dateColLetter} (index {dateColIndex})");
            Log($"[Diem Danh] Starting from row {startRow}, sheet: {sheetName}");

            int currentRow = startRow;
            int attendedCount = 0;
            int skippedCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                Log($"\n========== ROW {currentRow} ==========");

                // Step 1: Check if already attended
                var cellValue = await _sheetsService.ReadCellAsync(
                    _spreadsheetId, sheetName, currentRow, dateColIndex);

                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    Log($"[Diem Danh] Row {currentRow}: Already attended ({cellValue}), skipping.");
                    skippedCount++;
                    currentRow++;
                    continue;
                }

                // Step 2: Read credentials
                var credentials = await _sheetsService.ReadCredentialsAsync(
                    _spreadsheetId, sheetName, currentRow);

                if (credentials == null)
                {
                    Log($"[Diem Danh] Row {currentRow}: No credentials found. End of data.");
                    break;
                }

                var (username, password) = credentials.Value;
                Log($"[Diem Danh] Row {currentRow}: Logging in as {username}...");

                // Step 2.5: Mark as "dang diem danh" to prevent other processes
                try
                {
                    await _sheetsService.WriteCellAsync(
                        _spreadsheetId, sheetName, currentRow, dateColIndex,
                        "dang diem danh");
                    Log($"[Diem Danh] Row {currentRow}: Marked 'dang diem danh' in column {dateColLetter}.");
                }
                catch (Exception ex)
                {
                    Log($"[Diem Danh] Row {currentRow}: Failed to mark in-progress: {ex.Message}");
                }

                // Step 3: Sign-in
                var signinWorkflow = new NthSigninWorkflow(
                    _visionService,
                    _inputService,
                    assetsPath: Path.Combine(_templatesFolder, "nth", "signin"),
                    logger: _logger);

                var context = new GameContext { GameName = "NTH Game" };

                // We need to write credentials to Excel for signin to read
                // Instead, we use the signin workflow with Google Sheets credentials
                var signinResult = await signinWorkflow.ExecuteAsync(
                    context, cancellationToken, sheetName, currentRow - 1);
                // Note: NthSigninWorkflow uses 0-indexed internally (ExcelService)
                // But for Google Sheets attendance we pass row-1 since signin expects data row number

                if (!signinResult.Success)
                {
                    Log($"[Diem Danh] Row {currentRow}: Sign-in failed: {signinResult.Message}");
                    currentRow++;
                    continue;
                }

                // Step 4: Wait for menu button (max 60s)
                Log($"[Diem Danh] Row {currentRow}: Waiting for menu button (max 60s)...");
                var menuButtonFound = await WaitForMenuButtonAsync(60000, cancellationToken);

                if (!menuButtonFound)
                {
                    Log($"[Diem Danh] Row {currentRow}: Menu button not found after 60s, continuing anyway...");
                }
                else
                {
                    Log($"[Diem Danh] Row {currentRow}: Menu button detected! Login confirmed.");
                }

                // Step 5: Sign-out
                Log($"[Diem Danh] Row {currentRow}: Signing out...");
                var signoutWorkflow = new NthSignoutWorkflow(
                    _visionService,
                    _inputService,
                    assetsPath: Path.Combine(_templatesFolder, "nth", "signout"),
                    logger: _logger);

                var signoutResult = await signoutWorkflow.ExecuteAsync(context, cancellationToken);

                if (!signoutResult.Success)
                {
                    Log($"[Diem Danh] Row {currentRow}: Sign-out failed: {signoutResult.Message}");
                }

                // Step 6: Mark attendance in Google Sheets
                try
                {
                    await _sheetsService.WriteCellAsync(
                        _spreadsheetId, sheetName, currentRow, dateColIndex,
                        "da diem danh");
                    Log($"[Diem Danh] Row {currentRow}: Marked attendance in column {dateColLetter}.");
                    attendedCount++;
                }
                catch (Exception ex)
                {
                    Log($"[Diem Danh] Row {currentRow}: Failed to write to sheet: {ex.Message}");
                }

                currentRow++;
                await Task.Delay(2000, cancellationToken);
            }

            Log($"\n[Diem Danh] Completed! Attended: {attendedCount}, Skipped: {skippedCount}");
            return new WorkflowResult
            {
                Success = true,
                Message = $"Attendance complete. Attended: {attendedCount}, Skipped: {skippedCount}"
            };
        }
        catch (OperationCanceledException)
        {
            Log("[Diem Danh] Cancelled by user.");
            return new WorkflowResult { Success = false, Message = "Cancelled" };
        }
        catch (Exception ex)
        {
            Log($"[Diem Danh] Error: {ex.Message}");
            return new WorkflowResult { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Wait for the menu button to appear on screen (indicates game has loaded)
    /// </summary>
    private async Task<bool> WaitForMenuButtonAsync(
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(_templatesFolder, "nth", "ingame_camera", MenuButtonTemplate);
        if (!File.Exists(templatePath))
        {
            Log($"[Diem Danh] WARNING: Menu button template not found: {templatePath}");
            return false;
        }

        var endTime = DateTime.Now.AddMilliseconds(timeoutMs);
        var templateKey = $"ingame_camera/{Path.GetFileNameWithoutExtension(MenuButtonTemplate)}";
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

            if (results.Count > 0 && results.Any(r => r.Confidence >= MatchThreshold))
            {
                return true;
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    private void Log(string message)
    {
        _logger?.Invoke(message);
        Console.WriteLine(message);
    }
}
