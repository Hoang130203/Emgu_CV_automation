# 🎯 Game Automation Workflows

## 📖 Tổng Quan

Workflows là các chuỗi hành động tự động để thực hiện các tác vụ trong game hoặc ứng dụng. Mỗi workflow implement interface `IWorkflow` và có thể:

- Detect các elements trên màn hình (template matching, OCR, color detection)
- Simulate mouse movements và clicks như người thật
- Simulate keyboard typing với tốc độ tự nhiên
- Chờ và polling cho events/conditions
- Log tiến trình và handle errors

## 🏗️ Architecture

```
IWorkflow (Interface)
    ├── Name: Tên workflow
    ├── Description: Mô tả chức năng
    ├── CanExecute(): Check xem có thể chạy không
    └── ExecuteAsync(): Thực thi workflow

Dependencies:
    ├── IVisionService: Template matching, OCR, screen capture
    ├── IInputService: Mouse & keyboard simulation
    └── HumanLikeSimulator: Helper cho human-like interactions
```

## 📁 Cấu Trúc Thư Mục

```
GameAutomation.Core.Workflows/
├── IWorkflow.cs                           # Interface chính
├── Examples/                              # Example workflows
│   ├── OpenExcelWorkflow.cs              # Full implementation với Bezier curve
│   └── OpenExcelWorkflowSimplified.cs    # Simplified version
├── Helpers/
│   └── HumanLikeSimulator.cs             # Human-like simulation helper
└── README.md                              # File này
```

## 🚀 Quick Start

### 1. Tạo Workflow Mới

```csharp
using GameAutomation.Core.Workflows;
using GameAutomation.Core.Workflows.Helpers;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Input;

public class MyGameWorkflow : IWorkflow
{
    private readonly IVisionService _vision;
    private readonly HumanLikeSimulator _human;

    public string Name => "My Game Bot";
    public string Description => "Tự động farm resources";

    public MyGameWorkflow(IVisionService vision, IInputService input)
    {
        _vision = vision;
        _human = new HumanLikeSimulator(input);
    }

    public bool CanExecute(GameContext context)
    {
        // Check game state, resources, conditions...
        return true;
    }

    public async Task<bool> ExecuteAsync(GameContext context, CancellationToken ct = default)
    {
        // Implement your workflow here
        return true;
    }
}
```

### 2. Sử dụng HumanLikeSimulator

```csharp
// Di chuyển chuột với Bezier curve
await _human.MoveMouseAsync(x, y);

// Click với random delays
await _human.LeftClickAsync();
await _human.RightClickAsync();
await _human.DoubleClickAsync();

// Typing như người thật
await _human.TypeTextAsync("Hello World");

// Typing với lỗi chính tả (natural!)
await _human.TypeTextWithErrorsAsync("Hello World", errorRate: 0.05);

// Nhấn phím đặc biệt
await _human.KeyPressAsync(VirtualKeyCode.RETURN);

// Drag and drop
await _human.DragAndDropAsync(startX, startY, endX, endY);

// Scroll mượt mà
await _human.ScrollAsync(amount: -100, steps: 5);

// Pause để "suy nghĩ"
await _human.ThinkAsync(minMs: 500, maxMs: 2000);
```

### 3. Tìm Template trên Màn Hình

```csharp
// Capture screenshot
var screenshot = _vision.CaptureScreen();

// Find template
var templatePath = @"C:\Path\To\Template.png";
var results = _vision.FindTemplate(screenshot, templatePath, threshold: 0.8);

if (results.Count > 0)
{
    var target = results[0]; // First match
    Console.WriteLine($"Found at ({target.X}, {target.Y}) with confidence {target.Confidence}");

    // Move and click
    await _human.MoveMouseAsync(target.X, target.Y);
    await _human.LeftClickAsync();
}
```

### 4. Polling & Waiting

```csharp
// Chờ template xuất hiện (với timeout)
public async Task<DetectionResult?> WaitForTemplateAsync(
    string templatePath,
    int timeoutSeconds,
    CancellationToken ct)
{
    var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

    while (DateTime.Now < endTime)
    {
        ct.ThrowIfCancellationRequested();

        var screenshot = _vision.CaptureScreen();
        var results = _vision.FindTemplate(screenshot, templatePath, 0.8);

        if (results.Count > 0)
            return results[0];

        await Task.Delay(500, ct); // Poll mỗi 0.5s
    }

    return null; // Timeout
}
```

## 🎓 Examples

### Example 1: Click vào Button

```csharp
public async Task<bool> ClickButtonAsync(string buttonTemplatePath)
{
    // Find button
    var screenshot = _vision.CaptureScreen();
    var results = _vision.FindTemplate(screenshot, buttonTemplatePath, 0.8);

    if (results.Count == 0)
    {
        Console.WriteLine("❌ Button không tìm thấy!");
        return false;
    }

    // Move and click
    var button = results[0];
    await _human.MoveMouseAsync(button.X, button.Y);
    await _human.LeftClickAsync();

    Console.WriteLine("✅ Đã click button!");
    return true;
}
```

### Example 2: Fill Form

```csharp
public async Task<bool> FillFormAsync()
{
    // Click vào Name field
    await ClickButtonAsync("Name_Field.png");
    await _human.TypeTextAsync("John Doe");
    await Task.Delay(200);

    // Tab tới Email field
    await _human.KeyPressAsync(VirtualKeyCode.TAB);
    await _human.TypeTextAsync("john@example.com");
    await Task.Delay(200);

    // Tab tới Password field
    await _human.KeyPressAsync(VirtualKeyCode.TAB);
    await _human.TypeTextAsync("SecurePass123");
    await Task.Delay(200);

    // Submit
    await _human.KeyPressAsync(VirtualKeyCode.RETURN);

    return true;
}
```

### Example 3: Loop Until Condition

```csharp
public async Task<bool> FarmResourcesAsync(CancellationToken ct)
{
    int resourcesCollected = 0;

    while (resourcesCollected < 100)
    {
        ct.ThrowIfCancellationRequested();

        // Tìm resource icon
        var screenshot = _vision.CaptureScreen();
        var resources = _vision.FindTemplate(screenshot, "Resource.png", 0.8);

        if (resources.Count == 0)
        {
            Console.WriteLine("⏳ Chờ resources respawn...");
            await Task.Delay(5000, ct);
            continue;
        }

        // Click vào resource đầu tiên
        var resource = resources[0];
        await _human.MoveMouseAsync(resource.X, resource.Y);
        await _human.LeftClickAsync();

        resourcesCollected++;
        Console.WriteLine($"📦 Collected {resourcesCollected}/100");

        // Random delay giữa các lần collect
        await _human.ThinkAsync(1000, 3000);
    }

    Console.WriteLine("✅ Đã farm đủ 100 resources!");
    return true;
}
```

### Example 4: Retry with Exponential Backoff

```csharp
public async Task<bool> ClickWithRetryAsync(string templatePath, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        var screenshot = _vision.CaptureScreen();
        var results = _vision.FindTemplate(screenshot, templatePath, 0.8);

        if (results.Count > 0)
        {
            var target = results[0];
            await _human.MoveMouseAsync(target.X, target.Y);
            await _human.LeftClickAsync();
            return true;
        }

        if (attempt < maxRetries)
        {
            int delayMs = (int)Math.Pow(2, attempt) * 1000; // 2s, 4s, 8s
            Console.WriteLine($"⚠️ Thử lại sau {delayMs}ms... (Attempt {attempt}/{maxRetries})");
            await Task.Delay(delayMs);
        }
    }

    Console.WriteLine("❌ Không tìm thấy template sau {maxRetries} lần thử!");
    return false;
}
```

## 🎨 HumanLikeSimulator Configuration

Tùy chỉnh tốc độ và timing:

```csharp
var simulator = new HumanLikeSimulator(inputService)
{
    // Typing speed
    MinTypingDelayMs = 80,      // Slower: 120, Faster: 50
    MaxTypingDelayMs = 150,     // Slower: 200, Faster: 100

    // Mouse movement
    MinMouseSteps = 15,         // Smoother: 25, Faster: 10
    MaxMouseSteps = 25,         // Smoother: 35, Faster: 15
    MinMouseDelayMs = 5,        // Slower: 10, Faster: 3
    MaxMouseDelayMs = 12        // Slower: 20, Faster: 8
};
```

**Profiles:**

```csharp
// Profile 1: Fast & Efficient (Bot-like)
MinTypingDelayMs = 30, MaxTypingDelayMs = 60
MinMouseSteps = 8, MaxMouseSteps = 12

// Profile 2: Normal Human
MinTypingDelayMs = 80, MaxTypingDelayMs = 150
MinMouseSteps = 15, MaxMouseSteps = 25

// Profile 3: Careful Human
MinTypingDelayMs = 120, MaxTypingDelayMs = 200
MinMouseSteps = 25, MaxMouseSteps = 35
```

## 🔍 Template Matching Tips

### 1. Threshold Selection

```
0.95-1.0  = Pixel-perfect match (quá strict)
0.85-0.95 = Very good match (recommended cho UI icons)
0.75-0.85 = Good match (recommended cho game elements)
0.65-0.75 = Loose match (có thể false positives)
<0.65     = Too loose (không nên dùng)
```

### 2. Template Quality

✅ **Good Templates:**
- Unique và dễ phân biệt
- 50x50 đến 200x200 pixels
- High contrast
- Không bị ảnh hưởng bởi animations
- PNG format với transparent background (nếu cần)

❌ **Bad Templates:**
- Text có thể thay đổi (số, thời gian)
- Gradients phức tạp
- Quá nhỏ (<30x30 px)
- Quá lớn (>300x300 px)
- Có shadows hoặc reflections

### 3. Multi-Scale Matching

Nếu game có thể scale UI:

```csharp
var scales = new[] { 0.8, 1.0, 1.2 };

foreach (var scale in scales)
{
    var scaledTemplate = ScaleImage(template, scale);
    var results = _vision.FindTemplate(screenshot, scaledTemplate, 0.8);

    if (results.Count > 0)
    {
        // Found at this scale!
        break;
    }
}
```

## 📊 Error Handling & Logging

### Best Practices:

```csharp
public async Task<bool> ExecuteAsync(GameContext context, CancellationToken ct = default)
{
    try
    {
        Console.WriteLine($"🚀 [{Name}] Starting workflow...");

        // Step 1
        Console.WriteLine($"📍 [{Name}] Step 1: Finding button...");
        var found = await FindButtonAsync();
        if (!found)
        {
            Console.WriteLine($"❌ [{Name}] Failed at Step 1");
            return false;
        }
        Console.WriteLine($"✅ [{Name}] Step 1 completed");

        // Step 2
        Console.WriteLine($"📍 [{Name}] Step 2: Clicking button...");
        await ClickButtonAsync();
        Console.WriteLine($"✅ [{Name}] Step 2 completed");

        Console.WriteLine($"✅ [{Name}] Workflow completed successfully!");
        return true;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"⚠️ [{Name}] Workflow cancelled by user");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [{Name}] Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return false;
    }
}
```

## 🎯 Advanced Patterns

### Pattern 1: State Machine Workflow

```csharp
enum WorkflowState { FindEnemy, Attack, Loot, Return }

private WorkflowState _currentState = WorkflowState.FindEnemy;

public async Task<bool> ExecuteAsync(GameContext context, CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        switch (_currentState)
        {
            case WorkflowState.FindEnemy:
                if (await FindEnemyAsync())
                    _currentState = WorkflowState.Attack;
                break;

            case WorkflowState.Attack:
                if (await AttackEnemyAsync())
                    _currentState = WorkflowState.Loot;
                break;

            case WorkflowState.Loot:
                await LootAsync();
                _currentState = WorkflowState.Return;
                break;

            case WorkflowState.Return:
                await ReturnToBaseAsync();
                _currentState = WorkflowState.FindEnemy;
                break;
        }
    }

    return true;
}
```

### Pattern 2: Conditional Branching

```csharp
public async Task<bool> ExecuteAsync(GameContext context, CancellationToken ct)
{
    // Check health
    if (await IsHealthLowAsync())
    {
        Console.WriteLine("⚕️ Health low, healing...");
        await HealAsync();
    }

    // Check mana
    if (await IsManaLowAsync())
    {
        Console.WriteLine("💙 Mana low, restoring...");
        await RestoreManaAsync();
    }

    // Main action
    return await PerformMainActionAsync();
}
```

### Pattern 3: Parallel Tasks

```csharp
public async Task<bool> ExecuteAsync(GameContext context, CancellationToken ct)
{
    var task1 = CollectResourceAAsync();
    var task2 = CollectResourceBAsync();
    var task3 = MonitorEnemiesAsync();

    await Task.WhenAll(task1, task2, task3);

    return true;
}
```

## 📚 See Also

- [OpenExcelWorkflow.cs](./Examples/OpenExcelWorkflow.cs) - Full example với Bezier curve
- [OpenExcelWorkflowSimplified.cs](./Examples/OpenExcelWorkflowSimplified.cs) - Simplified version
- [HumanLikeSimulator.cs](./Helpers/HumanLikeSimulator.cs) - Helper class
- [Assets/Other/README.md](../../../Assets/Other/README.md) - Template guide

## 💡 Tips & Tricks

1. **Luôn test workflow từng bước** trước khi chạy full automation
2. **Sử dụng screenshots để debug** - save screenshots khi template không tìm thấy
3. **Add delays hợp lý** - quá nhanh sẽ bị detect, quá chậm sẽ inefficient
4. **Respect CancellationToken** - cho phép user stop workflow bất cứ lúc nào
5. **Log chi tiết** - giúp debug khi có lỗi
6. **Handle edge cases** - game lag, network disconnect, unexpected popups
7. **Use random delays** - tránh pattern detection
8. **Test với different resolutions** - đảm bảo templates work ở nhiều DPI settings

---

**Happy Automating! 🤖🎮**
