# 📂 Assets/Other - Workflow Templates

Folder này chứa các template images cho workflows tự động.

## 🎯 Workflow Example: Open Excel

### Template Images Cần Thiết

Đặt các file ảnh sau vào folder này (`Assets/Other/`):

1. **`Window_Search_Icon.png`**
   - Icon tìm kiếm của Windows (kính lúp)
   - Vị trí: Taskbar (góc dưới bên trái)
   - Kích thước khuyến nghị: 40x40 px
   - Format: PNG với transparent background

2. **`Excel_Init.png`**
   - Icon hoặc window của Excel sau khi khởi động
   - Có thể là: Excel icon trong taskbar, hoặc một phần của Excel window
   - Kích thước: Tùy vào vùng bạn muốn detect (recommend 100x100 px)
   - Format: PNG

### 📸 Cách Capture Templates

#### Phương pháp 1: Sử dụng Windows Snipping Tool

```
1. Mở Windows Snipping Tool (Win + Shift + S)
2. Chọn vùng cần capture
3. Save as PNG với tên chính xác
4. Copy vào Assets/Other/
```

#### Phương pháp 2: Sử dụng Game Automation UI

```
1. Click nút "Screenshot" trong Control Panel
2. Chọn vùng cần capture
3. Crop và save vào Assets/Other/
```

### 🎨 Tips Để Capture Template Tốt

✅ **DOs:**
- Capture với độ phân giải gốc (100% DPI)
- Chọn vùng có đặc điểm dễ nhận diện
- Tránh vùng có màu gradient phức tạp
- Nên có ít nhất 50x50 pixels
- Save dưới dạng PNG (không JPG)

❌ **DON'Ts:**
- Đừng capture quá lớn (>200x200 px)
- Đừng capture vùng thay đổi thường xuyên (clock, animation)
- Đừng bao gồm shadow hoặc transparent edges
- Đừng capture text có thể thay đổi

### 🔍 Template Matching Threshold

Trong workflow code, threshold = **0.8** (80% match)

- **0.9-1.0**: Rất chính xác, nhưng có thể miss nếu có slight changes
- **0.8-0.9**: Cân bằng tốt (recommended)
- **0.7-0.8**: Linh hoạt hơn, nhưng có thể match nhầm
- **<0.7**: Quá loose, không nên dùng

## 📝 Workflow Structure

```
Assets/Other/
├── Window_Search_Icon.png      (Bước 1: Click vào Search)
├── Excel_Init.png              (Bước 4: Click vào Excel window)
└── README.md                   (File này)
```

## 🚀 Sử dụng Workflow

### Trong Code:

```csharp
// Tạo workflow instance
var workflow = new OpenExcelWorkflow(visionService, inputService);

// Hoặc dùng simplified version với HumanLikeSimulator
var workflowSimple = new OpenExcelWorkflowSimplified(visionService, inputService);

// Execute
var gameContext = new GameContext();
bool success = await workflow.ExecuteAsync(gameContext);

if (success)
{
    Console.WriteLine("✅ Excel đã được mở thành công!");
}
```

### Trong Bot Orchestrator:

```csharp
botOrchestrator.RegisterWorkflow(new OpenExcelWorkflow(visionService, inputService));
botOrchestrator.Start();
```

## 🎬 Flow Hoạt Động

```
Start
  ↓
1. Tìm Window_Search_Icon.png
  ↓ (Di chuyển chuột như người thật - Bezier curve)
2. Click vào Search Icon
  ↓ (Delay 0.5s)
3. Nhập "Excel" (typing speed: 80-150ms/char)
  ↓ (Delay 0.3s)
4. Ấn Enter
  ↓ (Polling mỗi 0.5s, timeout 15s)
5. Chờ Excel_Init.png xuất hiện
  ↓ (Di chuyển chuột như người thật)
6. Click vào Excel window
  ↓
End (Success ✅)
```

## 🛠️ Troubleshooting

### Template Không Được Tìm Thấy?

**1. Kiểm tra độ phân giải:**
```
- Template capture ở 100% DPI
- Game/App cũng chạy ở 100% DPI
- Nếu khác → Re-capture templates
```

**2. Giảm threshold:**
```csharp
const double MatchThreshold = 0.75; // Thay vì 0.8
```

**3. Check template path:**
```csharp
// Đảm bảo path đúng
var templatePath = Path.Combine(_assetsPath, "Window_Search_Icon.png");
Console.WriteLine($"Looking for: {templatePath}");
Console.WriteLine($"File exists: {File.Exists(templatePath)}");
```

### Workflow Chạy Quá Nhanh/Chậm?

**Điều chỉnh delays trong HumanLikeSimulator:**

```csharp
var simulator = new HumanLikeSimulator(inputService)
{
    MinTypingDelayMs = 50,   // Fast typing
    MaxTypingDelayMs = 100,
    MinMouseSteps = 10,      // Faster mouse
    MaxMouseSteps = 15
};
```

### Mouse Di Chuyển Không Mượt?

**Tăng số bước trong Bezier curve:**

```csharp
var simulator = new HumanLikeSimulator(inputService)
{
    MinMouseSteps = 25,      // More steps = smoother
    MaxMouseSteps = 35
};
```

## 📚 Tài Liệu Thêm

- [OpenExcelWorkflow.cs](../../src/Core/GameAutomation.Core.Workflows/Examples/OpenExcelWorkflow.cs) - Full implementation
- [OpenExcelWorkflowSimplified.cs](../../src/Core/GameAutomation.Core.Workflows/Examples/OpenExcelWorkflowSimplified.cs) - Simplified version
- [HumanLikeSimulator.cs](../../src/Core/GameAutomation.Core.Workflows/Helpers/HumanLikeSimulator.cs) - Helper class

## 🎯 Tạo Workflow Mới

### Template Code:

```csharp
public class MyCustomWorkflow : IWorkflow
{
    private readonly IVisionService _visionService;
    private readonly HumanLikeSimulator _humanSim;
    private readonly string _assetsPath;

    public string Name => "My Custom Workflow";
    public string Description => "Mô tả workflow của bạn";

    public MyCustomWorkflow(IVisionService visionService, IInputService inputService)
    {
        _visionService = visionService;
        _humanSim = new HumanLikeSimulator(inputService);
        _assetsPath = @"C:\Claude\Games\AutoGame\EmguCvNTH\Assets\Other";
    }

    public bool CanExecute(GameContext context) => true;

    public async Task<bool> ExecuteAsync(GameContext context, CancellationToken ct = default)
    {
        try
        {
            // Step 1: Tìm template
            var templatePath = Path.Combine(_assetsPath, "YourTemplate.png");
            var screenshot = _visionService.CaptureScreen();
            var results = _visionService.FindTemplate(screenshot, templatePath, 0.8);

            if (results.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy template!");
                return false;
            }

            // Step 2: Di chuyển và click
            var target = results[0];
            await _humanSim.MoveMouseAsync(target.X, target.Y);
            await _humanSim.LeftClickAsync();

            // Step 3: Nhập text
            await _humanSim.TypeTextAsync("Hello World");
            await _humanSim.KeyPressAsync(VirtualKeyCode.RETURN);

            Console.WriteLine("✅ Workflow hoàn thành!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi: {ex.Message}");
            return false;
        }
    }
}
```

## 💡 Best Practices

1. **Luôn có fallback:** Nếu template không tìm thấy, retry hoặc skip
2. **Logging rõ ràng:** Console.WriteLine cho mọi bước quan trọng
3. **Human-like timing:** Dùng random delays để tránh detection
4. **Error handling:** Wrap trong try-catch với meaningful error messages
5. **Cancellation support:** Respect CancellationToken để user có thể stop
6. **Template versioning:** Nếu UI game thay đổi, cập nhật templates

## 📞 Support

Nếu cần hỗ trợ:
1. Check logs trong Logs tab của UI
2. Verify template files tồn tại và đúng path
3. Test workflow từng bước với breakpoints
4. Check DPI scaling settings của Windows

---

**Happy Automation! 🎮🤖**
