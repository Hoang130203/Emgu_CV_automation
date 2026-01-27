# ===========================================
# GAME AUTOMATION - WPF GUI PROJECT GENERATOR
# ===========================================

Write-Host "Starting project creation..." -ForegroundColor Cyan

$projectName = "GameAutomation"
$rootPath = "C:\Projects\$projectName"

New-Item -ItemType Directory -Force -Path $rootPath | Out-Null
Set-Location $rootPath

# Create solution
dotnet new sln -n $projectName

# Create projects
Write-Host "Creating projects..." -ForegroundColor Yellow
dotnet new classlib -n "$projectName.Core" -f net8.0 -o "src\$projectName.Core"
dotnet new wpf -n "$projectName.UI" -f net8.0-windows -o "src\$projectName.UI"
dotnet sln add "src\$projectName.Core\$projectName.Core.csproj"
dotnet sln add "src\$projectName.UI\$projectName.UI.csproj"

# Create folders
Write-Host "Creating folders..." -ForegroundColor Yellow
$folders = @(
    "assets\templates\login",
    "assets\templates\character",
    "config",
    "data\logs",
    "src\$projectName.Core\Models",
    "src\$projectName.Core\Services"
)
foreach ($f in $folders) { New-Item -ItemType Directory -Force -Path $f | Out-Null }

# Install packages
Write-Host "Installing packages..." -ForegroundColor Yellow
Set-Location "src\$projectName.Core"
dotnet add package Emgu.CV.runtime.windows --version 4.9.0.5494
dotnet add package Emgu.CV --version 4.9.0.5494
dotnet add package EPPlus --version 7.0.5
dotnet add package Serilog --version 3.1.1
dotnet add package System.Drawing.Common --version 8.0.0
Set-Location ..\..

Set-Location "src\$projectName.UI"
dotnet add package MaterialDesignThemes --version 5.0.0
dotnet add reference "..\$projectName.Core\$projectName.Core.csproj"
Set-Location ..\..

# Create MainWindow.xaml
Write-Host "Creating GUI files..." -ForegroundColor Yellow

$xaml = @'
<Window x:Class="GameAutomation.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Game Automation" Height="700" Width="1000"
        WindowStartupLocation="CenterScreen" Background="#1E1E1E">
    <Grid Margin="20">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <Border Grid.Column="0" Background="#252526" CornerRadius="8" Padding="15" Margin="0,0,10,0">
            <StackPanel>
                <TextBlock Text="Configuration" FontSize="18" FontWeight="Bold" Foreground="White" Margin="0,0,0,20"/>
                
                <TextBlock Text="Excel File:" Foreground="Gray" Margin="0,0,0,5"/>
                <Grid Margin="0,0,0,15">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox x:Name="ExcelPath" Text="data/accounts.xlsx" Background="#3C3C3C" Foreground="White" Padding="8"/>
                    <Button Grid.Column="1" Content="..." Width="40" Margin="5,0,0,0" Click="Browse_Click"/>
                </Grid>

                <TextBlock Text="Sheet:" Foreground="Gray" Margin="0,0,0,5"/>
                <TextBox x:Name="SheetNum" Text="2" Background="#3C3C3C" Foreground="White" Padding="8" Margin="0,0,0,15"/>

                <TextBlock Text="Start Row:" Foreground="Gray" Margin="0,0,0,5"/>
                <TextBox x:Name="StartRow" Text="37" Background="#3C3C3C" Foreground="White" Padding="8" Margin="0,0,0,20"/>

                <Button x:Name="BtnStart" Content="START" Height="40" FontSize="14" FontWeight="Bold" Background="#4CAF50" Foreground="White" Click="Start_Click" Margin="0,0,0,10"/>
                <Button x:Name="BtnStop" Content="STOP" Height="40" Background="#F44336" Foreground="White" Click="Stop_Click" IsEnabled="False"/>

                <Separator Margin="0,20,0,20" Background="#3C3C3C"/>
                
                <TextBlock Text="Statistics" FontSize="16" FontWeight="Bold" Foreground="White" Margin="0,0,0,15"/>
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                        <TextBlock Text="Total: " Foreground="Gray" Width="80"/>
                        <TextBlock x:Name="TxtTotal" Text="0" Foreground="White" FontWeight="Bold"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                        <TextBlock Text="Success: " Foreground="Gray" Width="80"/>
                        <TextBlock x:Name="TxtSuccess" Text="0" Foreground="#4CAF50" FontWeight="Bold"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="Failed: " Foreground="Gray" Width="80"/>
                        <TextBlock x:Name="TxtFailed" Text="0" Foreground="#F44336" FontWeight="Bold"/>
                    </StackPanel>
                </StackPanel>
            </StackPanel>
        </Border>

        <Border Grid.Column="1" Background="#252526" CornerRadius="8" Padding="15">
            <DockPanel>
                <TextBlock Text="Live Log" FontSize="18" FontWeight="Bold" Foreground="White" DockPanel.Dock="Top" Margin="0,0,0,15"/>
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <TextBox x:Name="LogBox" Background="#1E1E1E" Foreground="#00FF00" FontFamily="Consolas" FontSize="11" IsReadOnly="True" TextWrapping="Wrap" BorderThickness="0"/>
                </ScrollViewer>
            </DockPanel>
        </Border>
    </Grid>
</Window>
'@

$xaml | Out-File "src\$projectName.UI\MainWindow.xaml" -Encoding UTF8

# Create MainWindow.xaml.cs
$cs = @'
using System.Windows;
using Microsoft.Win32;

namespace GameAutomation.UI;

public partial class MainWindow : Window
{
    private bool _running = false;

    public MainWindow()
    {
        InitializeComponent();
        Log("Application ready");
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Excel|*.xlsx" };
        if (dlg.ShowDialog() == true)
            ExcelPath.Text = dlg.FileName;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        _running = true;
        BtnStart.IsEnabled = false;
        BtnStop.IsEnabled = true;

        Log("Bot started");
        Log($"Excel: {ExcelPath.Text}");
        Log($"Sheet: {SheetNum.Text}, Row: {StartRow.Text}");
        
        await Task.Delay(2000);
        Log("Ready to process accounts");
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _running = false;
        BtnStart.IsEnabled = true;
        BtnStop.IsEnabled = false;
        Log("Bot stopped");
    }

    private void Log(string msg)
    {
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            LogBox.ScrollToEnd();
        });
    }
}
'@

$cs | Out-File "src\$projectName.UI\MainWindow.xaml.cs" -Encoding UTF8

# Create App.xaml
$appXaml = @'
<Application x:Class="GameAutomation.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
    </Application.Resources>
</Application>
'@

$appXaml | Out-File "src\$projectName.UI\App.xaml" -Encoding UTF8

# Create Models
Write-Host "Creating models..." -ForegroundColor Yellow

$accountModel = @'
namespace GameAutomation.Core.Models;

public class AccountInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Status { get; set; } = "";
    public int Level { get; set; }
}
'@

$accountModel | Out-File "src\$projectName.Core\Models\AccountInfo.cs" -Encoding UTF8

$configModel = @'
namespace GameAutomation.Core.Models;

public class BotConfig
{
    public string ExcelPath { get; set; } = "data/accounts.xlsx";
    public string AssetsPath { get; set; } = "assets/templates";
}
'@

$configModel | Out-File "src\$projectName.Core\Models\BotConfig.cs" -Encoding UTF8

# Build
Write-Host "Building..." -ForegroundColor Yellow
dotnet restore
dotnet build

Write-Host ""
Write-Host "SUCCESS! Project created at: $rootPath" -ForegroundColor Green
Write-Host ""
Write-Host "To run GUI:" -ForegroundColor Cyan
Write-Host "  cd $rootPath" -ForegroundColor White
Write-Host "  dotnet run --project src\GameAutomation.UI" -ForegroundColor White
Write-Host ""

explorer $rootPath