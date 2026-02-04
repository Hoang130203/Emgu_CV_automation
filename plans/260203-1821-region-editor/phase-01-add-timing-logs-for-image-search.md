# Phase 1: Add Timing Logs for Image Search

**Status**: ⬜ Pending
**Priority**: High

## Overview

Thêm logging để đo thời gian mỗi lần tìm kiếm ảnh trong Daily workflow, giúp debug và optimize performance.

## Requirements

- Log thời gian bắt đầu, kết thúc, và duration cho mỗi lần search
- Format: `[TIMING] FindTemplate {templateName}: {duration}ms (found: {true/false})`
- Không ảnh hưởng performance đáng kể

## Files to Modify

- `src/Core/GameAutomation.Core.Workflows/Examples/NthLv26To44DailyWorkflow.cs`

## Implementation

1. Add `Stopwatch` to `FindMultiScaleAsync` method
2. Log timing info before return
3. Include region info in log if applicable

## Code Changes

```csharp
// In FindMultiScaleAsync method
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... existing search logic ...
stopwatch.Stop();
Log($"[TIMING] FindTemplate {templateFileName}: {stopwatch.ElapsedMilliseconds}ms (found: {result != null})");
```

## Success Criteria

- [ ] Timing logs appear in Logs tab
- [ ] Duration accurate to millisecond
- [ ] No noticeable performance impact
