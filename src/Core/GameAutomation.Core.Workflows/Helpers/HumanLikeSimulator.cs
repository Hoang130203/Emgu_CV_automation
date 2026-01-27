using GameAutomation.Core.Services.Input;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GameAutomation.Core.Workflows.Helpers
{
    /// <summary>
    /// Helper class để simulate hành động như người thật
    /// Bao gồm: Di chuyển chuột tự nhiên, typing với tốc độ thực tế, random delays
    /// </summary>
    public class HumanLikeSimulator
    {
        private readonly IInputService _inputService;
        private readonly Random _random;

        // Cấu hình mặc định
        public int MinTypingDelayMs { get; set; } = 80;
        public int MaxTypingDelayMs { get; set; } = 150;
        public int MinMouseSteps { get; set; } = 15;
        public int MaxMouseSteps { get; set; } = 25;
        public int MinMouseDelayMs { get; set; } = 5;
        public int MaxMouseDelayMs { get; set; } = 12;

        public HumanLikeSimulator(IInputService inputService)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _random = new Random();
        }

        /// <summary>
        /// Di chuyển chuột theo đường cong Bezier (như người thật)
        /// </summary>
        public async Task MoveMouseAsync(int targetX, int targetY)
        {
            var currentPos = _inputService.GetMousePosition();
            await MoveMouseFromToAsync(currentPos.X, currentPos.Y, targetX, targetY);
        }

        /// <summary>
        /// Di chuyển chuột từ vị trí A đến B với Bezier curve + Easing
        /// </summary>
        public async Task MoveMouseFromToAsync(int startX, int startY, int targetX, int targetY)
        {
            // Tính khoảng cách để điều chỉnh số bước
            double distance = Math.Sqrt(Math.Pow(targetX - startX, 2) + Math.Pow(targetY - startY, 2));
            int steps = (int)Math.Max(MinMouseSteps, Math.Min(MaxMouseSteps, distance / 15));

            // Tạo 2 control points cho Cubic Bezier (độ cong tự nhiên hơn)
            double curvature = Math.Min(distance * 0.3, 150); // Độ cong tỷ lệ với khoảng cách
            int ctrl1X = startX + (int)((targetX - startX) * 0.25) + _random.Next((int)(-curvature), (int)(curvature));
            int ctrl1Y = startY + (int)((targetY - startY) * 0.25) + _random.Next((int)(-curvature), (int)(curvature));
            int ctrl2X = startX + (int)((targetX - startX) * 0.75) + _random.Next((int)(-curvature / 2), (int)(curvature / 2));
            int ctrl2Y = startY + (int)((targetY - startY) * 0.75) + _random.Next((int)(-curvature / 2), (int)(curvature / 2));

            // Di chuyển theo Cubic Bezier curve với Easing
            for (int i = 0; i <= steps; i++)
            {
                // Áp dụng ease-in-out: chậm đầu, nhanh giữa, chậm cuối
                double linearT = (double)i / steps;
                double t = EaseInOutCubic(linearT);

                // Cubic Bezier: B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
                double x = Math.Pow(1 - t, 3) * startX
                         + 3 * Math.Pow(1 - t, 2) * t * ctrl1X
                         + 3 * (1 - t) * Math.Pow(t, 2) * ctrl2X
                         + Math.Pow(t, 3) * targetX;

                double y = Math.Pow(1 - t, 3) * startY
                         + 3 * Math.Pow(1 - t, 2) * t * ctrl1Y
                         + 3 * (1 - t) * Math.Pow(t, 2) * ctrl2Y
                         + Math.Pow(t, 3) * targetY;

                _inputService.MoveMouse((int)x, (int)y);

                // Delay động: chậm hơn ở đầu/cuối, nhanh hơn ở giữa
                int baseDelay = _random.Next(MinMouseDelayMs, MaxMouseDelayMs);
                double speedFactor = 1.0 - Math.Abs(0.5 - linearT) * 0.6; // 0.7 ở đầu/cuối, 1.0 ở giữa
                int delay = (int)(baseDelay / speedFactor);
                await Task.Delay(delay);
            }

            // Đảm bảo đến đúng vị trí
            _inputService.MoveMouse(targetX, targetY);
        }

        /// <summary>
        /// Ease-in-out cubic: chậm ở đầu và cuối, nhanh ở giữa
        /// </summary>
        private static double EaseInOutCubic(double t)
        {
            return t < 0.5
                ? 4 * t * t * t
                : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }

        /// <summary>
        /// Ease-in-out quintic: mượt mà hơn cubic
        /// </summary>
        private static double EaseInOutQuint(double t)
        {
            return t < 0.5
                ? 16 * t * t * t * t * t
                : 1 - Math.Pow(-2 * t + 2, 5) / 2;
        }

        /// <summary>
        /// Click chuột trái với random delay trước và sau
        /// </summary>
        public async Task LeftClickAsync()
        {
            await Task.Delay(_random.Next(50, 150)); // Pause trước khi click
            _inputService.LeftClick();
            await Task.Delay(_random.Next(100, 200)); // Pause sau khi click
        }

        /// <summary>
        /// Click chuột phải với random delay
        /// </summary>
        public async Task RightClickAsync()
        {
            await Task.Delay(_random.Next(50, 150));
            _inputService.RightClick();
            await Task.Delay(_random.Next(100, 200));
        }

        /// <summary>
        /// Double click với timing thực tế
        /// </summary>
        public async Task DoubleClickAsync()
        {
            await Task.Delay(_random.Next(50, 100));
            _inputService.LeftClick();
            await Task.Delay(_random.Next(80, 150)); // Khoảng cách giữa 2 clicks
            _inputService.LeftClick();
            await Task.Delay(_random.Next(100, 200));
        }

        /// <summary>
        /// Nhập text như người thật với tốc độ ngẫu nhiên
        /// </summary>
        public async Task TypeTextAsync(string text)
        {
            foreach (char c in text)
            {
                _inputService.KeyPress(c);

                // Delay ngẫu nhiên (80-150ms = ~400-750 WPM, realistic typing speed)
                int delay = _random.Next(MinTypingDelayMs, MaxTypingDelayMs);

                // Một số ký tự có thể gõ chậm hơn (như shift characters)
                if (char.IsUpper(c) || "!@#$%^&*()_+{}|:\"<>?".Contains(c))
                {
                    delay += _random.Next(20, 50);
                }

                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Gõ text với typing errors (thêm tính tự nhiên)
        /// </summary>
        public async Task TypeTextWithErrorsAsync(string text, double errorRate = 0.05)
        {
            for (int i = 0; i < text.Length; i++)
            {
                // Có tỷ lệ nhỏ gõ sai và sửa lại
                if (_random.NextDouble() < errorRate && i > 0)
                {
                    // Gõ ký tự sai
                    char wrongChar = (char)_random.Next('a', 'z' + 1);
                    _inputService.KeyPress(wrongChar);
                    await Task.Delay(_random.Next(MinTypingDelayMs, MaxTypingDelayMs));

                    // Nhận ra sai và backspace
                    await Task.Delay(_random.Next(200, 400)); // Pause khi nhận ra sai
                    _inputService.KeyPress(VirtualKeyCode.BACK);
                    await Task.Delay(_random.Next(100, 200));
                }

                // Gõ ký tự đúng
                _inputService.KeyPress(text[i]);
                int delay = _random.Next(MinTypingDelayMs, MaxTypingDelayMs);

                if (char.IsUpper(text[i]) || "!@#$%^&*()_+{}|:\"<>?".Contains(text[i]))
                {
                    delay += _random.Next(20, 50);
                }

                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Nhấn phím với delay tự nhiên
        /// </summary>
        public async Task KeyPressAsync(VirtualKeyCode key)
        {
            await Task.Delay(_random.Next(50, 150));
            _inputService.KeyPress(key);
            await Task.Delay(_random.Next(100, 200));
        }

        /// <summary>
        /// Random delay (để tạo cảm giác "suy nghĩ" của người thật)
        /// </summary>
        public async Task ThinkAsync(int minMs = 500, int maxMs = 2000)
        {
            await Task.Delay(_random.Next(minMs, maxMs));
        }

        /// <summary>
        /// Scroll chuột với tốc độ tự nhiên
        /// </summary>
        public async Task ScrollAsync(int amount, int steps = 5)
        {
            int scrollPerStep = amount / steps;
            for (int i = 0; i < steps; i++)
            {
                _inputService.MouseScroll(scrollPerStep);
                await Task.Delay(_random.Next(50, 150));
            }
        }

        /// <summary>
        /// Drag and drop với chuyển động mượt mà
        /// </summary>
        public async Task DragAndDropAsync(int startX, int startY, int endX, int endY)
        {
            // Di chuyển tới vị trí bắt đầu
            await MoveMouseAsync(startX, startY);
            await Task.Delay(_random.Next(100, 200));

            // Giữ chuột trái
            _inputService.MouseDown();
            await Task.Delay(_random.Next(50, 100));

            // Di chuyển tới vị trí kết thúc
            await MoveMouseFromToAsync(startX, startY, endX, endY);
            await Task.Delay(_random.Next(100, 200));

            // Thả chuột
            _inputService.MouseUp();
            await Task.Delay(_random.Next(100, 200));
        }
    }
}
