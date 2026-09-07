using System.Globalization;
using Chronowalker.Core.Models;
using Chronowalker.Core.Services;
using Chronowalker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Chronowalker.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
    private readonly Func<GamePlatform> _detectPlatform;
    private readonly GameIniService _gameIniService;
    private readonly SettingsService _settingsService;
    private readonly IStringLocalizer _strings;
    private GamePlatform _detectedPlatform = GamePlatform.Unknown;
    private bool _isInitialized;

    public MainViewModel(
        GameIniService gameIniService,
        SettingsService settingsService,
        IStringLocalizer strings,
        Func<GamePlatform> detectPlatform)
    {
        _gameIniService = gameIniService;
        _settingsService = settingsService;
        _strings = strings;
        _detectPlatform = detectPlatform;

        SegmentChoices =
        [
            new(_strings.Get("SegmentOption8"), 8),
            new(_strings.Get("SegmentOption12"), 12),
            new(_strings.Get("SegmentOption16"), 16),
            new(_strings.Get("SegmentOption18"), 18),
            new(_strings.Get("SegmentOption26"), 26),
            new(_strings.Get("SegmentOption32"), 32),
        ];
        DeadlineChoices =
        [
            new(_strings.Get("DeadlineOption30"), 30),
            new(_strings.Get("DeadlineOption45"), 45),
            new(_strings.Get("DeadlineOption60"), 60),
            new(_strings.Get("DeadlineOption80"), 80),
            new(_strings.Get("DeadlineOption100"), 100),
            new(_strings.Get("DeadlineOptionYear"), 365),
            new(_strings.Get("DeadlineOptionUnlimited"), 9999),
            new(_strings.Get("DeadlineOptionCustom"), null),
        ];
        PlatformChoices =
        [
            new(_strings.Get("PlatformOptionAuto"), PlatformChoice.Auto),
            new(_strings.Get("PlatformOptionSteamGog"), PlatformChoice.SteamOrGog),
            new(_strings.Get("PlatformOptionXbox"), PlatformChoice.XboxPc),
        ];

        SelectedSegment = SegmentChoices.Single(static choice => choice.Value == 18);
        SelectedDeadline = DeadlineChoices.Single(static choice => choice.Value == 365);
        SelectedPlatform = PlatformChoices.Single(static choice => choice.Value == PlatformChoice.Auto);
        DetectedPlatformLabel = _strings.Get("DetectedUnknown");
        StatusTitle = _strings.Get("StatusReadyTitle");
        StatusMessage = _strings.Get("StatusReadyMessage");
    }

    [ObservableProperty]
    public partial double CustomDeadlineValue { get; set; } = 366;

    [ObservableProperty]
    public partial string DetectedPlatformLabel { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsExperimentalAcknowledged { get; set; }

    [ObservableProperty]
    public partial ChoiceItem<int?> SelectedDeadline { get; set; }

    [ObservableProperty]
    public partial ChoiceItem<PlatformChoice> SelectedPlatform { get; set; }

    [ObservableProperty]
    public partial ChoiceItem<int> SelectedSegment { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial string StatusTitle { get; set; }

    public IReadOnlyList<ChoiceItem<int?>> DeadlineChoices { get; }

    public IReadOnlyList<ChoiceItem<PlatformChoice>> PlatformChoices { get; }

    public IReadOnlyList<ChoiceItem<int>> SegmentChoices { get; }

    public Visibility BusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public bool CanInstall => IsNotBusy && (!IsExperimental || IsExperimentalAcknowledged);

    public string ConfigPath
    {
        get
        {
            GamePlatform? platform = ResolvePlatform(false);
            return platform.HasValue
                ? _gameIniService.GetConfigPath(platform.Value)
                : _strings.Get("ConfigPathUnavailable");
        }
    }

    public Visibility CustomDeadlineVisibility => IsCustomDeadline ? Visibility.Visible : Visibility.Collapsed;

    public string DeadlinePreview
    {
        get
        {
            if (!IsValidCustomDeadline(out int storedDays))
            {
                return _strings.Get("CustomDeadlineInvalidPreview");
            }

            return _strings.Format("CustomDeadlinePreview", storedDays - 1, storedDays);
        }
    }

    public bool IsCustomDeadline => SelectedDeadline.Value is null;

    public bool IsExperimental => SelectedSegment.Value > 18;

    public Visibility ExperimentalVisibility => IsExperimental ? Visibility.Visible : Visibility.Collapsed;

    public bool IsNotBusy => !IsBusy;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        SetBusy(true);
        try
        {
            ChronowalkerSettings settings = await _settingsService.LoadAsync();
            SelectedSegment = SegmentChoices.FirstOrDefault(choice => choice.Value == settings.Segments) ?? SegmentChoices[3];
            SelectedDeadline = settings.IsCustomDeadline
                ? DeadlineChoices[^1]
                : DeadlineChoices.FirstOrDefault(choice => choice.Value == settings.Days) ?? DeadlineChoices[^1];
            SelectedPlatform = PlatformChoices.FirstOrDefault(choice => choice.Value == settings.PlatformChoice) ?? PlatformChoices[0];
            CustomDeadlineValue = settings.Days;
            await RefreshDetectionCoreAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    public Task SaveSettingsAsync()
    {
        int days = IsCustomDeadline && IsValidCustomDeadline(out int customDays)
            ? customDays
            : SelectedDeadline.Value ?? 366;
        var settings = new ChronowalkerSettings
        {
            Segments = SelectedSegment.Value,
            Days = days,
            IsCustomDeadline = IsCustomDeadline,
            PlatformChoice = SelectedPlatform.Value,
        };
        return _settingsService.SaveAsync(settings);
    }

    partial void OnCustomDeadlineValueChanged(double value)
    {
        OnPropertyChanged(nameof(DeadlinePreview));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(BusyVisibility));
    }

    partial void OnIsExperimentalAcknowledgedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
    }

    partial void OnSelectedDeadlineChanged(ChoiceItem<int?> value)
    {
        OnPropertyChanged(nameof(IsCustomDeadline));
        OnPropertyChanged(nameof(CustomDeadlineVisibility));
        OnPropertyChanged(nameof(DeadlinePreview));
    }

    partial void OnSelectedPlatformChanged(ChoiceItem<PlatformChoice> value)
    {
        OnPropertyChanged(nameof(ConfigPath));
    }

    partial void OnSelectedSegmentChanged(ChoiceItem<int> value)
    {
        IsExperimentalAcknowledged = false;
        OnPropertyChanged(nameof(IsExperimental));
        OnPropertyChanged(nameof(ExperimentalVisibility));
        OnPropertyChanged(nameof(CanInstall));
    }

    [RelayCommand]
    private async Task ApplyRecommendedAsync()
    {
        SelectedSegment = SegmentChoices.Single(static choice => choice.Value == 18);
        SelectedDeadline = DeadlineChoices.Single(static choice => choice.Value is null);
        CustomDeadlineValue = 366;
        await SaveSettingsAsync();
        SetStatus(
            InfoBarSeverity.Success,
            _strings.Get("StatusRecommendedTitle"),
            _strings.Get("StatusRecommendedMessage"));
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!TryCreateOptions(out ChronowalkerOptions? options))
        {
            return;
        }

        SetBusy(true);
        try
        {
            await SaveSettingsAsync();
            string path = await _gameIniService.ExportAsync(options);
            SetStatus(
                InfoBarSeverity.Success,
                _strings.Get("StatusExportedTitle"),
                _strings.Format("StatusExportedMessage", path));
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            SetOperationFailure();
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        if (!TryCreateOptions(out ChronowalkerOptions? options))
        {
            return;
        }

        SetBusy(true);
        try
        {
            await SaveSettingsAsync();
            OperationResult result = await _gameIniService.ApplyAsync(options);
            SetStatus(
                InfoBarSeverity.Success,
                _strings.Get("StatusInstalledTitle"),
                _strings.Format("StatusInstalledMessage", result.ConfigPath));
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            SetOperationFailure();
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task RefreshDetectionAsync()
    {
        SetBusy(true);
        try
        {
            await RefreshDetectionCoreAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        GamePlatform? platform = ResolvePlatform(true);
        if (!platform.HasValue)
        {
            return;
        }

        SetBusy(true);
        try
        {
            OperationResult result = await _gameIniService.UninstallAsync(platform.Value);
            string titleKey = result.Changed ? "StatusUninstalledTitle" : "StatusNothingToUndoTitle";
            string messageKey = result.Changed ? "StatusUninstalledMessage" : "StatusNothingToUndoMessage";
            SetStatus(
                result.Changed ? InfoBarSeverity.Success : InfoBarSeverity.Informational,
                _strings.Get(titleKey),
                _strings.Format(messageKey, result.ConfigPath));
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            SetOperationFailure();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshDetectionCoreAsync()
    {
        _detectedPlatform = await Task.Run(_detectPlatform);
        DetectedPlatformLabel = _detectedPlatform switch
        {
            GamePlatform.SteamOrGog => _strings.Get("DetectedSteamGog"),
            GamePlatform.XboxPc => _strings.Get("DetectedXbox"),
            GamePlatform.Ambiguous => _strings.Get("DetectedAmbiguous"),
            _ => _strings.Get("DetectedUnknown"),
        };
        OnPropertyChanged(nameof(ConfigPath));
        SetStatus(
            _detectedPlatform is GamePlatform.SteamOrGog or GamePlatform.XboxPc
                ? InfoBarSeverity.Success
                : InfoBarSeverity.Warning,
            _strings.Get("StatusDetectionTitle"),
            DetectedPlatformLabel);
    }

    private GamePlatform? ResolvePlatform(bool showError)
    {
        GamePlatform? platform = SelectedPlatform.Value switch
        {
            PlatformChoice.SteamOrGog => GamePlatform.SteamOrGog,
            PlatformChoice.XboxPc => GamePlatform.XboxPc,
            PlatformChoice.Auto when _detectedPlatform == GamePlatform.SteamOrGog => GamePlatform.SteamOrGog,
            PlatformChoice.Auto when _detectedPlatform == GamePlatform.XboxPc => GamePlatform.XboxPc,
            _ => null,
        };

        if (!platform.HasValue && showError)
        {
            SetStatus(
                InfoBarSeverity.Warning,
                _strings.Get("StatusChoosePlatformTitle"),
                _strings.Get("StatusChoosePlatformMessage"));
        }

        return platform;
    }

    private void SetBusy(bool isBusy)
    {
        IsBusy = isBusy;
    }

    private void SetOperationFailure()
    {
        SetStatus(
            InfoBarSeverity.Error,
            _strings.Get("StatusFailedTitle"),
            _strings.Get("StatusFailedMessage"));
    }

    private void SetStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusSeverity = severity;
        StatusTitle = title;
        StatusMessage = message;
    }

    private bool IsValidCustomDeadline(out int days)
    {
        bool valid = !double.IsNaN(CustomDeadlineValue) &&
            CustomDeadlineValue >= 1 &&
            CustomDeadlineValue <= int.MaxValue &&
            Math.Truncate(CustomDeadlineValue) == CustomDeadlineValue;
        days = valid ? Convert.ToInt32(CustomDeadlineValue, CultureInfo.InvariantCulture) : 0;
        return valid;
    }

    private bool TryCreateOptions(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChronowalkerOptions? options)
    {
        options = null;
        GamePlatform? platform = ResolvePlatform(true);
        if (!platform.HasValue)
        {
            return false;
        }

        int days;
        if (IsCustomDeadline)
        {
            if (!IsValidCustomDeadline(out days))
            {
                SetStatus(
                    InfoBarSeverity.Warning,
                    _strings.Get("StatusInvalidDeadlineTitle"),
                    _strings.Get("StatusInvalidDeadlineMessage"));
                return false;
            }
        }
        else
        {
            days = SelectedDeadline.Value!.Value;
        }

        options = new ChronowalkerOptions(SelectedSegment.Value, days, IsCustomDeadline, platform.Value);
        return true;
    }
}
