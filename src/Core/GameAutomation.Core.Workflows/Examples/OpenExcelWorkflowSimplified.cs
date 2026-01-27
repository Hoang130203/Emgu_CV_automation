using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Workflows.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameAutomation.Core.Workflows.Examples
{
    /// <summary>
    /// Simplified version sử dụng HumanLikeSimulator helper
    /// Flow: Tìm search icon -> Click -> Nhập "Excel" -> Enter -> Tìm Excel_Init.png -> Click
    /// </summary>
    public class OpenExcelWorkflowSimplified : IWorkflow
    {
        private readonly IVisionService _visionService;
        private readonly HumanLikeSimulator _humanSim;
        private readonly string _assetsPath;

        private const string WindowSearchIconTemplate = "Window_Search_Icon.png";
        private const string ExcelInitTemplate = "Excel_Init.png";
        private const double MatchThreshold = 0.8;

        public string Name => "Open Excel (Simplified)";
        public string Description => "Mở Excel với HumanLikeSimulator helper";

        public OpenExcelWorkflowSimplified(
            IVisionService visionService,
            IInputService inputService,
            string assetsPath = @"C:\Claude\Games\AutoGame\EmguCvNTH\Assets\Other")
        {
            _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
            _humanSim = new HumanLikeSimulator(inputService);
            _assetsPath = assetsPath;
        }

        public Task<bool> CanExecuteAsync(GameContext context) => Task.FromResult(true);

        public async Task<WorkflowResult> ExecuteAsync(GameContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("🚀 [OpenExcel] Bắt đầu workflow...");

                // Step 1: Tìm và click vào Windows Search Icon
                var searchIcon = await FindAndClickTemplateAsync(WindowSearchIconTemplate, "Windows Search Icon", cancellationToken);
                if (!searchIcon)
                {
                    return new WorkflowResult
                    {
                        Success = false,
                        Message = "Không tìm thấy Windows Search Icon"
                    };
                }

                // Step 2: Chờ 0.5 giây
                await Task.Delay(500, cancellationToken);

                // Step 3: Nhập "Excel" và Enter
                Console.WriteLine("⌨️  [OpenExcel] Nhập 'Excel'...");
                await _humanSim.TypeTextAsync("Excel");
                await _humanSim.KeyPressAsync(VirtualKeyCode.RETURN);

                // Step 4: Chờ Excel khởi động và click
                var excelInit = await FindAndClickTemplateAsync(
                    ExcelInitTemplate,
                    "Excel Window",
                    cancellationToken,
                    waitTimeoutSeconds: 15);

                if (!excelInit)
                {
                    return new WorkflowResult
                    {
                        Success = false,
                        Message = "Timeout: Excel không khởi động"
                    };
                }

                Console.WriteLine("✅ [OpenExcel] Workflow hoàn thành!");
                return new WorkflowResult
                {
                    Success = true,
                    Message = "Excel đã được mở thành công!"
                };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("⚠️  [OpenExcel] Workflow bị hủy");
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Workflow bị hủy bởi user"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [OpenExcel] Lỗi: {ex.Message}");
                return new WorkflowResult
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        #region Helper Methods

        private async Task<bool> FindAndClickTemplateAsync(
            string templateFileName,
            string displayName,
            CancellationToken cancellationToken,
            int waitTimeoutSeconds = 5)
        {
            Console.WriteLine($"🔍 [OpenExcel] Tìm {displayName}...");

            var templatePath = Path.Combine(_assetsPath, templateFileName);
            var result = await WaitForTemplateAsync(templatePath, waitTimeoutSeconds, cancellationToken);

            if (result == null)
            {
                Console.WriteLine($"❌ [OpenExcel] Không tìm thấy {displayName}!");
                return false;
            }

            Console.WriteLine($"✓ [OpenExcel] Tìm thấy {displayName} tại ({result.X}, {result.Y})");

            // Di chuyển và click
            await _humanSim.MoveMouseAsync(result.X, result.Y);
            await _humanSim.LeftClickAsync();

            return true;
        }

        private async Task<DetectionResult?> WaitForTemplateAsync(
            string templatePath,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

            while (DateTime.Now < endTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var screenshot = _visionService.CaptureScreen();
                var results = _visionService.FindTemplate(screenshot, templatePath, MatchThreshold);

                if (results.Count > 0)
                    return results[0];

                await Task.Delay(500, cancellationToken);
            }

            return null;
        }

        #endregion
    }
}
