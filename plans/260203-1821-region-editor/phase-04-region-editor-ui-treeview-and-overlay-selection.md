# Phase 4: Region Editor UI - TreeView and Overlay Selection

**Status**: ⬜ Pending
**Priority**: High

## Overview

Giao diện Region Editor với TreeView hiển thị Templates, cho phép người dùng visual select region bằng cách kéo thả trên game screen.

## Requirements

- TreeView hiển thị cây thư mục Templates (grouped by flow)
- Icon indicator: ✓ có region, ○ chưa có
- Nút "Khoanh vùng" khi click vào ảnh chưa có region
- Ẩn tool window, hiển thị overlay trên game để kéo thả
- Tự động tính toán ratio từ pixel coordinates
- Nút Edit cho regions đã có
- Preview thumbnail của template

## Files to Create

- `src/UI/GameAutomation.UI.WPF/Windows/RegionEditorWindow.xaml`
- `src/UI/GameAutomation.UI.WPF/Windows/RegionEditorWindow.xaml.cs`
- `src/UI/GameAutomation.UI.WPF/ViewModels/RegionEditorViewModel.cs`
- `src/UI/GameAutomation.UI.WPF/Controls/RegionSelectionOverlay.xaml`
- `src/UI/GameAutomation.UI.WPF/Controls/RegionSelectionOverlay.xaml.cs`

## UI Layout

```
┌─────────────────────────────────────────────────────┐
│  Region Editor                              [X]     │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────┬─────────────────────────────┤
│ │ Templates           │  Preview & Config           │
│ │ ─────────────────── │  ─────────────────────────  │
│ │ ▼ daily             │  [Template thumbnail]       │
│ │   ○ 01_openbutton   │                             │
│ │   ✓ 02_maintale     │  Current Region:            │
│ │   ✓ 03_adventure    │  StartX: 0.25  EndX: 0.95   │
│ │   ○ 07_dungoan      │  StartY: 0.50  EndY: 0.90   │
│ │ ▼ camera            │                             │
│ │   ○ 01_menubutton   │  Source: [JSON/Hardcoded]   │
│ │   ○ 02_camerabutton │                             │
│ │ ▼ dungoan           │  [Khoanh vùng] [Edit] [Del] │
│ │   ...               │                             │
│ └─────────────────────┴─────────────────────────────┤
│ [Import JSON] [Export JSON]              [Close]    │
└─────────────────────────────────────────────────────┘
```

## Region Selection Overlay

Khi user click "Khoanh vùng":

1. RegionEditorWindow.Hide()
2. Hiển thị RegionSelectionOverlay (transparent fullscreen)
3. User kéo thả rectangle trên game
4. Calculate ratios: startX/Y, endX/Y từ pixel / screen size
5. Save to RegionConfigService
6. RegionEditorWindow.Show() với updated data

```
┌─────────────────────────────────────────────────────┐
│ [Kéo thả để chọn vùng tìm kiếm]     [ESC để hủy]   │
│                                                     │
│         ┌───────────────────┐                       │
│         │   Dragging rect   │                       │
│         │   (semi-opaque)   │                       │
│         └───────────────────┘                       │
│                                                     │
│ Position: (320, 180) - (960, 540)                   │
│ Ratio: (0.25, 0.25) - (0.75, 0.75)                  │
└─────────────────────────────────────────────────────┘
```

## ViewModel Structure

```csharp
public class RegionEditorViewModel
{
    public ObservableCollection<TemplateGroupViewModel> TemplateGroups { get; }
    public TemplateItemViewModel? SelectedTemplate { get; set; }

    public ICommand SelectRegionCommand { get; }
    public ICommand EditRegionCommand { get; }
    public ICommand DeleteRegionCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
}

public class TemplateGroupViewModel
{
    public string Name { get; }  // "daily", "camera", etc.
    public ObservableCollection<TemplateItemViewModel> Items { get; }
}

public class TemplateItemViewModel
{
    public string Key { get; }           // "daily/02_daily_maintale"
    public string FileName { get; }      // "02_daily_maintale.png"
    public string FullPath { get; }      // Full path to template
    public bool HasRegion { get; }       // Has JSON or hardcoded region
    public string RegionSource { get; }  // "JSON" / "Hardcoded" / "None"
    public SearchRegion? Region { get; }
    public BitmapImage? Thumbnail { get; }
}
```

## Key Implementation Details

### Mouse drag on overlay
```csharp
private Point _startPoint;
private Rectangle _selectionRect;

private void OnMouseDown(object sender, MouseButtonEventArgs e)
{
    _startPoint = e.GetPosition(this);
    _selectionRect.Visibility = Visibility.Visible;
    CaptureMouse();
}

private void OnMouseMove(object sender, MouseEventArgs e)
{
    if (IsMouseCaptured)
    {
        var currentPoint = e.GetPosition(this);
        UpdateSelectionRect(_startPoint, currentPoint);
    }
}

private void OnMouseUp(object sender, MouseButtonEventArgs e)
{
    ReleaseMouseCapture();
    var bounds = GetSelectionBounds();
    var region = CalculateRatios(bounds, Screen.PrimaryScreen.Bounds);
    OnRegionSelected?.Invoke(region);
    Close();
}
```

### Calculate ratios
```csharp
private SearchRegion CalculateRatios(Rect pixelBounds, Size screenSize)
{
    return new SearchRegion(
        pixelBounds.Left / screenSize.Width,
        pixelBounds.Top / screenSize.Height,
        pixelBounds.Right / screenSize.Width,
        pixelBounds.Bottom / screenSize.Height
    );
}
```

## Success Criteria

- [ ] TreeView shows all templates grouped by flow
- [ ] Icons indicate region status (✓/○)
- [ ] "Khoanh vùng" hides window and shows overlay
- [ ] Drag selection works smoothly
- [ ] Ratios calculated correctly
- [ ] Regions saved to JSON
- [ ] Import/Export JSON working
- [ ] Edit existing regions
- [ ] Delete custom regions (revert to hardcoded)
