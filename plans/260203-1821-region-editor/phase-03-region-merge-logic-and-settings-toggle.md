# Phase 3: Region Merge Logic and Settings Toggle

**Status**: ⬜ Pending
**Priority**: High

## Overview

Implement logic để merge regions từ JSON config với hardcoded registry. Thêm setting toggle cho phép enable/disable region-based search.

## Requirements

- Priority: JSON config > hardcoded in ImageResourceRegistry
- Setting toggle `UseRegionSearch` in BotConfiguration
- When disabled, search full screen for all templates

## Files to Modify

- `src/Core/GameAutomation.Core.Models/Configuration/BotConfiguration.cs`
- `src/Core/GameAutomation.Core.Models/Vision/ImageResourceRegistry.cs`
- `src/UI/GameAutomation.UI.WPF/ViewModels/SettingsViewModel.cs`
- `src/UI/GameAutomation.UI.WPF/SettingsWindow.xaml`

## Implementation

### BotConfiguration.cs
```csharp
// Add new property
public bool UseRegionSearch { get; set; } = true;
```

### ImageResourceRegistry.cs
```csharp
// Add static field for runtime config
private static RegionConfigService? _regionConfigService;

public static void SetRegionConfigService(RegionConfigService service)
    => _regionConfigService = service;

// Modify GetRegionByFileName to check JSON first
public static SearchRegion? GetRegionByFileName(string fileName, bool useRegionSearch = true)
{
    if (!useRegionSearch) return null; // Full screen

    // 1. Check JSON config first (priority)
    var key = GetKeyFromFileName(fileName);
    if (_regionConfigService?.TryGetRegion(key, out var jsonRegion) == true)
        return jsonRegion;

    // 2. Fall back to hardcoded registry
    // ... existing logic ...
}
```

### SettingsViewModel.cs
```csharp
[ObservableProperty]
private bool _useRegionSearch = true;
```

### SettingsWindow.xaml
Add toggle in Detection Settings card.

## Priority Flow

```
User requests region for "daily/02_daily_maintale":
  1. Check UseRegionSearch setting → if false, return null (full screen)
  2. Check RegionConfigService (JSON) → if found & enabled, return it
  3. Check ImageResourceRegistry (hardcoded) → if found, return it
  4. Return null (full screen)
```

## Success Criteria

- [ ] JSON regions override hardcoded
- [ ] UseRegionSearch toggle works
- [ ] Setting persists across restarts
- [ ] UI toggle in Settings window
