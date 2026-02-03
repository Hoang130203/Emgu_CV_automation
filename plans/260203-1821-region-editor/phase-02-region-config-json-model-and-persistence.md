# Phase 2: Region Config JSON Model and Persistence

**Status**: ⬜ Pending
**Priority**: High

## Overview

Tạo model và service để lưu/load custom region configs từ JSON file, cho phép người dùng configure regions mà không cần modify code.

## Requirements

- JSON schema cho region definitions
- Load/Save từ file (auto-load on startup)
- Support import từ file khác (share configs)
- Thread-safe access

## Files to Create

- `src/Core/GameAutomation.Core.Models/Configuration/RegionConfig.cs`
- `src/Core/GameAutomation.Core.Services/Configuration/RegionConfigService.cs`

## JSON Schema

```json
{
  "version": "1.0",
  "lastModified": "2026-02-03T18:30:00Z",
  "regions": {
    "daily/02_daily_maintale": {
      "startX": 0.25,
      "startY": 0.5,
      "endX": 0.95,
      "endY": 0.9,
      "enabled": true,
      "notes": "User-defined region"
    }
  }
}
```

## Implementation

### RegionConfig.cs
```csharp
public class RegionConfig
{
    public string Version { get; set; } = "1.0";
    public DateTime LastModified { get; set; }
    public Dictionary<string, RegionEntry> Regions { get; set; } = new();
}

public class RegionEntry
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Notes { get; set; }

    public SearchRegion ToSearchRegion() => new(StartX, StartY, EndX, EndY);
}
```

### RegionConfigService.cs
- `LoadAsync()` - Load from default path
- `SaveAsync()` - Save to default path
- `ImportAsync(string path)` - Import from external file
- `ExportAsync(string path)` - Export to file
- `GetRegion(string key)` - Get region for template key
- `SetRegion(string key, SearchRegion region)` - Update region

## Default Path

`{AppData}/GameAutomation/region-config.json` or alongside exe `./config/region-config.json`

## Success Criteria

- [ ] JSON file created/loaded correctly
- [ ] Regions persist across app restarts
- [ ] Import/Export working
- [ ] Thread-safe operations
