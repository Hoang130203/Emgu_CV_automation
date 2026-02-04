using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Services.Configuration;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace GameAutomation.UI.WPF.ViewModels;

public partial class RegionEditorViewModel : ObservableObject
{
    private readonly RegionConfigService _configService;
    private readonly string _templatesPath;
    private readonly Action? _closeAction;
    private readonly Func<string, Action<SearchRegion>, Task>? _selectRegionFunc;

    [ObservableProperty]
    private ObservableCollection<TemplateGroupViewModel> _templateGroups = new();

    [ObservableProperty]
    private TemplateItemViewModel? _selectedTemplate;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private string _regionInfo = "No region defined";

    [ObservableProperty]
    private string _regionSource = "None";

    [ObservableProperty]
    private bool _hasRegion;

    [ObservableProperty]
    private bool _canDeleteRegion;

    public RegionEditorViewModel(
        RegionConfigService configService,
        string? templatesPath = null,
        Action? closeAction = null,
        Func<string, Action<SearchRegion>, Task>? selectRegionFunc = null)
    {
        _configService = configService;
        _templatesPath = templatesPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        _closeAction = closeAction;
        _selectRegionFunc = selectRegionFunc;

        LoadTemplates();
    }

    private void LoadTemplates()
    {
        TemplateGroups.Clear();

        // Group templates from ImageResourceRegistry
        var groups = ImageResourceRegistry.Resources
            .GroupBy(r => r.Key.Split('/')[0])
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var groupVm = new TemplateGroupViewModel { Name = group.Key };

            foreach (var resource in group.OrderBy(r => r.Key))
            {
                var fullPath = Path.Combine(_templatesPath, resource.Value.RelativePath);
                var hasJsonRegion = _configService.HasCustomRegion(resource.Key);
                var hasHardcodedRegion = resource.Value.Region != null;

                groupVm.Items.Add(new TemplateItemViewModel
                {
                    Key = resource.Key,
                    FileName = Path.GetFileName(resource.Value.RelativePath),
                    FullPath = fullPath,
                    HasRegion = hasJsonRegion || hasHardcodedRegion,
                    RegionSource = hasJsonRegion ? "JSON" : (hasHardcodedRegion ? "Hardcoded" : "None"),
                    Region = hasJsonRegion
                        ? _configService.GetRegionEntry(resource.Key)?.ToSearchRegion()
                        : resource.Value.Region
                });
            }

            TemplateGroups.Add(groupVm);
        }
    }

    partial void OnSelectedTemplateChanged(TemplateItemViewModel? value)
    {
        if (value == null)
        {
            PreviewImage = null;
            RegionInfo = "No template selected";
            RegionSource = "None";
            HasRegion = false;
            CanDeleteRegion = false;
            return;
        }

        // Load preview image
        try
        {
            if (File.Exists(value.FullPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(value.FullPath);
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage = bitmap;
            }
            else
            {
                PreviewImage = null;
            }
        }
        catch
        {
            PreviewImage = null;
        }

        // Update region info
        HasRegion = value.HasRegion;
        RegionSource = value.RegionSource;
        CanDeleteRegion = value.RegionSource == "JSON";

        if (value.Region != null)
        {
            var r = value.Region;
            RegionInfo = $"Start: ({r.StartX:F2}, {r.StartY:F2})\nEnd: ({r.EndX:F2}, {r.EndY:F2})";
        }
        else
        {
            RegionInfo = "Full screen (no region)";
        }
    }

    [RelayCommand]
    private async Task SelectRegionAsync()
    {
        if (SelectedTemplate == null || _selectRegionFunc == null)
            return;

        var templateKey = SelectedTemplate.Key;

        await _selectRegionFunc(templateKey, region =>
        {
            // Save to config service
            _configService.SetRegion(templateKey, region, "User-defined via Region Editor");
            _ = _configService.SaveAsync();

            // Reload templates to reflect changes
            LoadTemplates();

            // Re-select the template
            foreach (var group in TemplateGroups)
            {
                var item = group.Items.FirstOrDefault(i => i.Key == templateKey);
                if (item != null)
                {
                    SelectedTemplate = item;
                    break;
                }
            }
        });
    }

    [RelayCommand]
    private void EditRegion()
    {
        // Same as select region but for editing
        SelectRegionCommand.Execute(null);
    }

    [RelayCommand]
    private async Task DeleteRegionAsync()
    {
        if (SelectedTemplate == null || SelectedTemplate.RegionSource != "JSON")
            return;

        var templateKey = SelectedTemplate.Key;
        _configService.RemoveRegion(templateKey);
        await _configService.SaveAsync();

        // Reload
        LoadTemplates();

        // Re-select
        foreach (var group in TemplateGroups)
        {
            var item = group.Items.FirstOrDefault(i => i.Key == templateKey);
            if (item != null)
            {
                SelectedTemplate = item;
                break;
            }
        }
    }

    [RelayCommand]
    private async Task ImportJsonAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Region Config"
        };

        if (dialog.ShowDialog() == true)
        {
            var success = await _configService.ImportAsync(dialog.FileName);
            if (success)
            {
                LoadTemplates();
            }
        }
    }

    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Export Region Config",
            FileName = "region-config.json"
        };

        if (dialog.ShowDialog() == true)
        {
            await _configService.ExportAsync(dialog.FileName);
        }
    }

    [RelayCommand]
    private void Close()
    {
        _closeAction?.Invoke();
    }

    public void Refresh() => LoadTemplates();
}

public partial class TemplateGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TemplateItemViewModel> _items = new();

    [ObservableProperty]
    private bool _isExpanded = true;
}

public partial class TemplateItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _hasRegion;

    [ObservableProperty]
    private string _regionSource = "None";

    [ObservableProperty]
    private SearchRegion? _region;

    public string StatusIcon => HasRegion ? "✓" : "○";
    public string StatusColor => HasRegion ? "#27ae60" : "#95a5a6";
}
