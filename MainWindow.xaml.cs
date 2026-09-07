using System.Runtime.InteropServices;
using Chronowalker.Core.Services;
using Chronowalker.Services;
using Chronowalker.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace Chronowalker;

/// <summary>
/// Hosts the Chronowalker configuration experience.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const int InitialWindowWidth = 920;
    private const int InitialWindowHeight = 760;
    private const int WindowCornerPreferenceAttribute = 33;
    private const int RoundSmallCornerPreference = 3;
    private double _statusInfoBarHeight;

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
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ConfigureCaptionButtons();
        ConfigureWindowCorners();
        AppWindow.SetIcon("Assets/AppIcon.ico");
        SetCompactInitialBounds();
    }

    internal MainViewModel ViewModel { get; }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        RootScrollViewer.UpdateLayout();
        GrowWindowToFitStartupContent();
    }

    private void OnSettingsGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool useTwoColumns = e.NewSize.Width >= 440;
        RightSettingsColumn.Width = useTwoColumns ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(DeadlineSettings, useTwoColumns ? 1 : 0);
        Grid.SetRow(DeadlineSettings, useTwoColumns ? 0 : 1);
    }

    private void OnStatusInfoBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double previousHeight = _statusInfoBarHeight;
        _statusInfoBarHeight = e.NewSize.Height;
        if (previousHeight <= 0 ||
            Math.Abs(previousHeight - e.NewSize.Height) < 0.5 ||
            StatusInfoBar.XamlRoot is null)
        {
            return;
        }

        double rasterizationScale = StatusInfoBar.XamlRoot.RasterizationScale;
        int heightChange = (int)Math.Round(
            (e.NewSize.Height - previousHeight) * rasterizationScale,
            MidpointRounding.AwayFromZero);
        if (heightChange == 0)
        {
            return;
        }

        ResizeWindowHeight(heightChange);
    }

    private void GrowWindowToFitStartupContent()
    {
        if (RootScrollViewer.ScrollableHeight <= 0.5 || RootScrollViewer.XamlRoot is null)
        {
            return;
        }

        int overflowHeight = (int)Math.Ceiling(
            (RootScrollViewer.ScrollableHeight + 2) * RootScrollViewer.XamlRoot.RasterizationScale);
        ResizeWindowHeight(overflowHeight);
    }

    private void ResizeWindowHeight(int heightChange)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        int maximumHeight = Math.Max(1, displayArea.WorkArea.Height - 48);
        SizeInt32 currentSize = AppWindow.Size;
        int targetHeight = Math.Clamp(currentSize.Height + heightChange, 1, maximumHeight);
        if (targetHeight != currentSize.Height)
        {
            AppWindow.Resize(new SizeInt32(currentSize.Width, targetHeight));
        }
    }

    private void SetCompactInitialBounds()
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        RectInt32 workArea = displayArea.WorkArea;
        int width = Math.Min(InitialWindowWidth, Math.Max(1, workArea.Width - 48));
        int height = Math.Min(InitialWindowHeight, Math.Max(1, workArea.Height - 48));
        int x = workArea.X + Math.Max(0, (workArea.Width - width) / 2);
        int y = workArea.Y + Math.Max(0, (workArea.Height - height) / 2);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void ConfigureCaptionButtons()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }

    private void ConfigureWindowCorners()
    {
        nint windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        int cornerPreference = RoundSmallCornerPreference;
        _ = DwmSetWindowAttribute(
            windowHandle,
            WindowCornerPreferenceAttribute,
            ref cornerPreference,
            sizeof(int));
    }
}
