using Chronowalker.Core.Services;
using Chronowalker.Services;
using Chronowalker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Chronowalker;

/// <summary>
/// Hosts the Chronowalker configuration experience.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var strings = new WinUiStringLocalizer();
        var gameIniService = new GameIniService(localAppDataPath, desktopPath);
        var settingsService = new SettingsService(localAppDataPath);
        ViewModel = new MainViewModel(
            gameIniService,
            settingsService,
            strings,
            () => new GamePlatformDetector(localAppDataPath, WindowsGameLocator.GetWindowsInstallMarkers()).Detect());

        InitializeComponent();
        Title = strings.Get("WindowTitle");
        AppWindow.SetIcon("Assets/AppIcon.ico");
    }

    internal MainViewModel ViewModel { get; }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void OnSettingsGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool useTwoColumns = e.NewSize.Width >= 440;
        RightSettingsColumn.Width = useTwoColumns ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(DeadlineSettings, useTwoColumns ? 1 : 0);
        Grid.SetRow(DeadlineSettings, useTwoColumns ? 0 : 1);
    }
}
