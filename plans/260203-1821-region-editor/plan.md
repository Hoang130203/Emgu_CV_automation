# Plan: Region Editor & Search Timing

**Date**: 2026-02-03
**Branch**: dev
**Status**: Planning

## Summary

Thêm logging timing cho image search và tính năng Region Editor cho phép người dùng visual configure search regions cho templates.

## Phases

| # | Phase | Status | Description |
|---|-------|--------|-------------|
| 1 | Logging Timing | ✅ Done | Add timing logs for image search in Daily workflow |
| 2 | Region Config Model | ✅ Done | JSON persistence for custom regions |
| 3 | Region Merge Logic | ✅ Done | Priority: JSON > hardcoded, settings toggle |
| 4 | Region Editor UI | ✅ Done | TreeView + overlay region selection |
| 5 | Integration & Testing | ✅ Done | Wire up components, manual test |

## Key Files

- `NthLv26To44DailyWorkflow.cs` - Add timing logs
- `BotConfiguration.cs` - Add `UseRegionSearch` setting
- `ImageResourceRegistry.cs` - Add runtime region override
- `RegionConfig.cs` (new) - JSON model for regions
- `RegionEditorWindow.xaml` (new) - Region editor UI

## Dependencies

- EmguCV for template matching (existing)
- CommunityToolkit.Mvvm (existing)
- System.Text.Json (existing in .NET)

## Notes

- Ratio-based coordinates (0.0-1.0) for resolution independence
- JSON config stored in app data folder or alongside Templates
- Priority: JSON config > hardcoded registry
