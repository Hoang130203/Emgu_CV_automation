# Phase 5: Integration Wiring and Manual Testing

**Status**: ⬜ Pending
**Priority**: High

## Overview

Wire up all components, add entry point in MainWindow, manual testing.

## Requirements

- Button in MainWindow to open Region Editor
- Initialize RegionConfigService on app startup
- Connect workflows to use merged regions
- Manual test all features

## Files to Modify

- `src/UI/GameAutomation.UI.WPF/MainWindow.xaml` - Add Region Editor button
- `src/UI/GameAutomation.UI.WPF/ViewModels/MainViewModel.cs` - Add command
- `src/UI/GameAutomation.UI.WPF/App.xaml.cs` - Initialize services

## Implementation

### MainWindow.xaml
Add button in left panel (near Settings button):
```xml
<Button Content="📍 Region Editor"
        Command="{Binding OpenRegionEditorCommand}"
        Style="{StaticResource ActionButtonStyle}"/>
```

### MainViewModel.cs
```csharp
[RelayCommand]
private void OpenRegionEditor()
{
    var window = new RegionEditorWindow(_regionConfigService);
    window.Owner = Application.Current.MainWindow;
    window.ShowDialog();
}
```

### App.xaml.cs
```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Initialize RegionConfigService
    var regionConfigService = new RegionConfigService();
    await regionConfigService.LoadAsync();

    // Set for ImageResourceRegistry
    ImageResourceRegistry.SetRegionConfigService(regionConfigService);

    // Pass to MainViewModel...
}
```

## Testing Checklist

### Phase 1 - Timing Logs
- [ ] Run Daily workflow
- [ ] Check Logs tab for `[TIMING]` entries
- [ ] Verify millisecond accuracy

### Phase 2 - Region Config
- [ ] App creates region-config.json on first run
- [ ] JSON is valid and readable
- [ ] Config persists after restart

### Phase 3 - Merge Logic
- [ ] Disable UseRegionSearch → full screen search
- [ ] Add JSON region → overrides hardcoded
- [ ] Remove JSON region → falls back to hardcoded

### Phase 4 - Region Editor UI
- [ ] Button opens Region Editor window
- [ ] TreeView shows all templates
- [ ] Icons show correct status
- [ ] Click template shows preview
- [ ] "Khoanh vùng" hides window
- [ ] Overlay appears fullscreen
- [ ] Drag selection works
- [ ] Region saved after selection
- [ ] Edit button allows modification
- [ ] Delete removes JSON entry
- [ ] Import loads external JSON
- [ ] Export saves to chosen location

### Phase 5 - Integration
- [ ] Daily workflow uses JSON regions
- [ ] Settings toggle works
- [ ] No crashes or exceptions
- [ ] Performance acceptable

## Success Criteria

- [ ] All phases integrated
- [ ] All tests pass
- [ ] No regressions in existing functionality
- [ ] Clean code, no unused imports
