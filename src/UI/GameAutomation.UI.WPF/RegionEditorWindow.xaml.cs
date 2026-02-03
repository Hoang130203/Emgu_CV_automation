using System.Windows;
using System.Windows.Input;
using GameAutomation.Core.Models.Vision;
using GameAutomation.Core.Services.Configuration;
using GameAutomation.UI.WPF.ViewModels;

namespace GameAutomation.UI.WPF;

public partial class RegionEditorWindow : Window
{
    private readonly RegionEditorViewModel _viewModel;

    public RegionEditorWindow(RegionConfigService configService, string? templatesPath = null)
    {
        InitializeComponent();

        _viewModel = new RegionEditorViewModel(
            configService,
            templatesPath,
            closeAction: () => Close(),
            selectRegionFunc: SelectRegionAsync
        );

        DataContext = _viewModel;
    }

    private void TemplateItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TemplateItemViewModel item)
        {
            _viewModel.SelectedTemplate = item;
        }
    }

    private async Task SelectRegionAsync(string templateKey, Action<SearchRegion> onRegionSelected)
    {
        // Hide this window
        Hide();

        // Small delay to ensure window is hidden
        await Task.Delay(200);

        // Show overlay window
        var overlay = new RegionSelectionOverlay();
        overlay.RegionSelected += (s, region) =>
        {
            onRegionSelected(region);
        };

        overlay.ShowDialog();

        // Show this window again
        Show();
        Activate();
    }
}
