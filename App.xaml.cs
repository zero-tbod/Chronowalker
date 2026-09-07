using Microsoft.UI.Xaml;

namespace Chronowalker;

/// <summary>
/// Provides application-specific startup behavior.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
