using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Models.GameState;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Input;
using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace GameAutomation.Core.Workflows.Examples
{
    /// <summary>
    /// Example workflow: Mở Excel từ Windows Search
    /// Flow: Tìm search icon -> Click -> Nhập "Excel" -> Enter -> Tìm Excel_Init.png -> Click
    /// </summary>
    public class OpenExcelWorkflow : IWorkflow
    {
        private readonly IVisionService _visionService;
        private readonly IInputService _inputService;
        private readonly string _assetsPath;

        // Các template images (đặt trong Assets/Other/)
        private const string WindowSearchIconTemplate = "Window_Search_Icon.png";
        private const string ExcelInitTemplate = "Excel_Init.png";

        // Threshold cho template matching
        private const double MatchThreshold = 0.8;

        public string Name => "Open Excel Workflow";
        public string Description => "Mở Excel từ Windows Search với human-like interactions";

        public OpenExcelWorkflow(
            IVisionService visionService,
            IInputService inputService,
            string assetsPath = @"C:\Claude\Games\AutoGame\EmguCvNTH\Assets\Other")
        {
            _visionService = visionService ?? throw new ArgumentNullException(nameof(visionService));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _assetsPath = assetsPath;
        }

        public Task<bool> CanExecuteAsync(GameContext context)
        {
            // Workflow này có thể chạy bất cứ lúc nào
            return Task.FromResult(true);
        }

        public async Task<WorkflowResult> ExecuteAsync(GameContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("[OpenExcel] Bắt đầu workflow mở Excel...");

                // Bước 1: Tìm Windows Search Icon
                Console.WriteLine("[OpenExcel] Bước 1: Tìm Windows Search Icon...");
                var searchIconPath = Path.Combine(_assetsPath, WindowSearchIconTemplate);
                var searchIconResult = await FindTemplateWithRetryAsync(searchIconPath, maxRetries: 3, cancellationToken);

                if (searchIconResult == null)
                {
                    Console.WriteLine("[OpenExcel] ❌ Không tìm thấy Windows Search Icon!");
                    return new WorkflowResult
                    {
                        Success = false,
                        Message = "Không tìm thấy Windows Search Icon"
                    };
                }

                Console.WriteLine($"[OpenExcel] ✓ Tìm thấy Search Icon tại ({searchIconResult.X}, {searchIconResult.Y})");

                // Bước 2: Di chuyển chuột như người thật và click vào Search Icon
                Console.WriteLine("[OpenExcel] Bước 2: Di chuyển chuột tới Search Icon...");
                await MoveMouseHumanLikeAsync(searchIconResult.X, searchIconResult.Y);
                await Task.Delay(200, cancellationToken); // Pause nhẹ trước khi click

                _inputService.LeftClick();
                Console.WriteLine("[OpenExcel] ✓ Đã click vào Search Icon");

                // Bước 3: Chờ 0.5 giây
                await Task.Delay(500, cancellationToken);

                // Bước 4: Nhập "Excel" như người thật (từng ký tự với delay)
                Console.WriteLine("[OpenExcel] Bước 3: Nhập 'Excel'...");
                await TypeTextHumanLikeAsync("Excel", cancellationToken);
                Console.WriteLine("[OpenExcel] ✓ Đã nhập 'Excel'");

                // Bước 5: Ấn Enter
                await Task.Delay(300, cancellationToken); // Pause nhẹ trước khi Enter
                _inputService.KeyPress(VirtualKeyCode.RETURN);
                Console.WriteLine("[OpenExcel] ✓ Đã ấn Enter");

                // Bước 6: Chờ Excel_Init.png xuất hiện (polling với timeout)
                Console.WriteLine("[OpenExcel] Bước 4: Chờ Excel khởi động...");
                var excelInitPath = Path.Combine(_assetsPath, ExcelInitTemplate);
                var excelInitResult = await WaitForTemplateAsync(
                    excelInitPath,
                    timeoutSeconds: 15,
                    pollIntervalMs: 500,
                    cancellationToken);

                if (excelInitResult == null)
                {
                    Console.WriteLine("[OpenExcel] ❌ Timeout: Excel không khởi động trong 15 giây!");
                    return new WorkflowResult
                    {
                        Success = false,
                        Message = "Timeout: Excel không khởi động trong 15 giây"
                    };
                }

                Console.WriteLine($"[OpenExcel] ✓ Excel đã khởi động! Tìm thấy tại ({excelInitResult.X}, {excelInitResult.Y})");

                // Bước 7: Di chuyển chuột như người thật và click vào Excel_Init
                Console.WriteLine("[OpenExcel] Bước 5: Click vào Excel window...");
                await MoveMouseHumanLikeAsync(excelInitResult.X, excelInitResult.Y);
                await Task.Delay(200, cancellationToken);

                _inputService.LeftClick();
                Console.WriteLine("[OpenExcel] ✓ Đã click vào Excel window");

                Console.WriteLine("[OpenExcel] ✅ Workflow hoàn thành thành công!");
                return new WorkflowResult
                {
                    Success = true,
                    Message = "Excel đã được mở thành công!"
                };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[OpenExcel] ⚠️ Workflow bị hủy bởi user");
                return new WorkflowResult
                {
                    Success = false,
                    Message = "Workflow bị hủy bởi user"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenExcel] ❌ Lỗi: {ex.Message}");
                return new WorkflowResult
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        #region Human-Like Simulation Helpers

        /// <summary>
        /// Di chuyển chuột như người thật với Bezier curve
        /// </summary>
        private async Task MoveMouseHumanLikeAsync(int targetX, int targetY)
        {
            var currentPos = _inputService.GetMousePosition();
            int startX = currentPos.X;
            int startY = currentPos.Y;

            // Tạo một điểm control ngẫu nhiên cho Bezier curve
            var random = new Random();
            int controlX = (startX + targetX) / 2 + random.Next(-50, 50);
            int controlY = (startY + targetY) / 2 + random.Next(-50, 50);

            // Di chuyển qua 20 bước để tạo chuyển động mượt mà
            int steps = 20;
            int delayPerStep = random.Next(8, 15); // 8-15ms mỗi bước

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;

                // Quadratic Bezier curve: B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
                double x = Math.Pow(1 - t, 2) * startX + 2 * (1 - t) * t * controlX + Math.Pow(t, 2) * targetX;
                double y = Math.Pow(1 - t, 2) * startY + 2 * (1 - t) * t * controlY + Math.Pow(t, 2) * targetY;

                _inputService.MoveMouse((int)x, (int)y);
                await Task.Delay(delayPerStep);
            }

            // Đảm bảo đến đúng vị trí cuối
            _inputService.MoveMouse(targetX, targetY);
        }

        /// <summary>
        /// Nhập text như người thật với delay ngẫu nhiên giữa các ký tự
        /// </summary>
        private async Task TypeTextHumanLikeAsync(string text, CancellationToken cancellationToken)
        {
            var random = new Random();

            foreach (char c in text)
            {
                _inputService.KeyPress(c);

                // Delay ngẫu nhiên giữa 80-150ms (tốc độ đánh máy thực tế)
                int delay = random.Next(80, 150);
                await Task.Delay(delay, cancellationToken);
            }
        }

        /// <summary>
        /// Tìm template với retry mechanism
        /// </summary>
        private async Task<DetectionResult?> FindTemplateWithRetryAsync(
            string templatePath,
            int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var screenshot = _visionService.CaptureScreen();
                var results = _visionService.FindTemplate(screenshot, templatePath, MatchThreshold);

                if (results.Count > 0)
                {
                    // Trả về kết quả có confidence cao nhất
                    return results[0];
                }

                if (attempt < maxRetries)
                {
                    Console.WriteLine($"[OpenExcel] Thử lại lần {attempt}/{maxRetries}...");
                    await Task.Delay(1000, cancellationToken);
                }
            }

            return null;
        }

        /// <summary>
        /// Chờ cho đến khi template xuất hiện (với timeout)
        /// </summary>
        private async Task<DetectionResult?> WaitForTemplateAsync(
            string templatePath,
            int timeoutSeconds,
            int pollIntervalMs = 500,
            CancellationToken cancellationToken = default)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

            while (DateTime.Now < endTime)
            {
                var screenshot = _visionService.CaptureScreen();
                var results = _visionService.FindTemplate(screenshot, templatePath, MatchThreshold);

                if (results.Count > 0)
                {
                    return results[0];
                }

                await Task.Delay(pollIntervalMs, cancellationToken);
            }

            return null;
        }

        #endregion
    }
}
