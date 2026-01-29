using GameAutomation.Core.Models.Configuration;
using GameAutomation.UI.WPF.ViewModels;
using System.Windows;

namespace GameAutomation.UI.WPF;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly BotConfiguration _configuration;

    public SettingsWindow(BotConfiguration configuration)
    {
        InitializeComponent();

        _configuration = configuration;
        _viewModel = new SettingsViewModel(CloseWithResult);
        _viewModel.LoadFromConfiguration(configuration);

        DataContext = _viewModel;

        // Set API key to password box (PasswordBox doesn't support binding)
        ApiKeyBox.Password = _viewModel.ApiKey;
        ApiKeyBox.PasswordChanged += (s, e) => _viewModel.ApiKey = ApiKeyBox.Password;
    }

    private void CloseWithResult(bool save)
    {
        if (save)
        {
            _viewModel.SaveToConfiguration(_configuration);
            DialogResult = true;
        }
        else
        {
            DialogResult = false;
        }
        Close();
    }
}
