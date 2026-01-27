using System.Drawing;
using System.Drawing.Imaging;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Workflows.Actions;
using static GameAutomation.Core.Workflows.Actions.AutomationActions;

Console.WriteLine("=== Game Automation - Vision Service Test ===\n");

// Setup templates folder
var templatesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
if (!Directory.Exists(templatesFolder))
    Directory.CreateDirectory(templatesFolder);

// Test 1: Screen Capture
Console.WriteLine("[1] Testing Screen Capture...");
try
{
    using var screenshot = CaptureScreen();
    var screenshotPath = Path.Combine(Environment.CurrentDirectory, "screenshot.png");
    screenshot.Save(screenshotPath, ImageFormat.Png);
    Console.WriteLine($"    Screenshot saved: {screenshotPath}");
    Console.WriteLine($"    Size: {screenshot.Width}x{screenshot.Height}");
}
catch (Exception ex)
{
    Console.WriteLine($"    Error: {ex.Message}");
}

// Test 2: Template Matching + Human-like Click
Console.WriteLine("\n[2] Testing Find & Click (Human-like)...");
var templatePath = Path.Combine(templatesFolder, "template.png");

if (File.Exists(templatePath))
{
    try
    {
        // Sử dụng human-like movement: Bezier curve + ease-in-out
        var result = await FindAndClickHumanAsync(templatePath, threshold: 0.8);

        if (result != null)
        {
            Console.WriteLine($"    Found and clicked at: ({result.X + result.Width/2}, {result.Y + result.Height/2})");
            Console.WriteLine($"    Confidence: {result.Confidence:P1}");
        }
        else
        {
            Console.WriteLine("    Template not found.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    Error: {ex.Message}");
    }
}
else
{
    Console.WriteLine($"    Skipped - Create '{templatePath}' to test.");
}

// Test 3: Open Excel Flow
Console.WriteLine("\n[3] Opening Excel...");
var excelInitPath = Path.Combine(templatesFolder, "Excel_Init.png");

try
{
    // Chờ một chút sau khi click vào search
    await ThinkAsync(300, 500);

    // Gõ "Excel" như người thật
    Console.WriteLine("    Typing 'Excel'...");
    await TypeHumanAsync("Excel");

    // Chờ một chút rồi nhấn Enter
    await ThinkAsync(200, 400);
    Console.WriteLine("    Pressing Enter...");
    Input.KeyPress(VirtualKeyCode.RETURN);

    // Chờ Excel xuất hiện (tối đa 15 giây)
    Console.WriteLine("    Waiting for Excel to open...");
    var excelResult = WaitForTemplate(excelInitPath, threshold: 0.7, timeoutMs: 15000, checkIntervalMs: 500);

    if (excelResult != null)
    {
        Console.WriteLine($"    Excel found at: ({excelResult.X}, {excelResult.Y})");
        Console.WriteLine("    Moving to Excel and clicking...");

        // Di chuyển tới và click như người thật
        await FindAndClickHumanAsync(excelInitPath, threshold: 0.7);
        Console.WriteLine("    Done!");
    }
    else
    {
        Console.WriteLine("    Excel_Init.png not found (timeout).");
        Console.WriteLine($"    Make sure '{excelInitPath}' exists in Templates folder.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"    Error: {ex.Message}");
}

// Test 4: Close License Popup
Console.WriteLine("\n[4] Handling License Popup...");
var licensePopupPath = Path.Combine(templatesFolder, "Excel_license_popup.png");
var licenseClosePath = Path.Combine(templatesFolder, "Excel_license_close.png");

try
{
    // Chờ popup license xuất hiện (tối đa 10 giây)
    Console.WriteLine("    Waiting for license popup...");
    var popupResult = WaitForTemplate(licensePopupPath, threshold: 0.7, timeoutMs: 10000, checkIntervalMs: 300);

    if (popupResult != null)
    {
        Console.WriteLine($"    License popup found!");
        await ThinkAsync(200, 400);

        // Tìm và click nút close
        Console.WriteLine("    Looking for close button...");
        var closeResult = await FindAndClickHumanAsync(licenseClosePath, threshold: 0.7);

        if (closeResult != null)
        {
            Console.WriteLine($"    Clicked close button at: ({closeResult.X + closeResult.Width/2}, {closeResult.Y + closeResult.Height/2})");
            Console.WriteLine("    License popup closed!");
        }
        else
        {
            Console.WriteLine("    Close button not found.");
        }
    }
    else
    {
        Console.WriteLine("    No license popup appeared (skipped).");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"    Error: {ex.Message}");
}

// Test 5: Color Detection
Console.WriteLine("\n[5] Testing Color Detection...");
try
{
    using var screenshot = CaptureScreen();
    var found = Vision.DetectColor(screenshot, 0, 0, 100, 100, Color.Red, tolerance: 30);
    Console.WriteLine($"    Red color in top-left 100x100: {(found ? "Found" : "Not found")}");
}
catch (Exception ex)
{
    Console.WriteLine($"    Error: {ex.Message}");
}

Console.WriteLine("\n=== Test Complete ===");
