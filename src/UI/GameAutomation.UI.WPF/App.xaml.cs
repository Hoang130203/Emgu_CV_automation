using Microsoft.Extensions.DependencyInjection;
using GameAutomation.Core.Models.Configuration;
using GameAutomation.Core.Services.Configuration;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Services.Excel;
using GameAutomation.UI.WPF.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace GameAutomation.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the current <see cref="App"/> instance in use
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; private set; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<BotConfiguration>();
        services.AddSingleton<RegionConfigService>();
        
        services.AddTransient<IVisionService, VisionService>();
        services.AddTransient<IInputService, InputService>();
        services.AddTransient<IExcelService, ExcelService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Windows (Optional, if we want to resolve them via DI)
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Resolve the Main Window and show it
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}

