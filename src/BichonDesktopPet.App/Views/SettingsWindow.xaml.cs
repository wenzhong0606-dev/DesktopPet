using System;
using System.Windows;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.App.Views;

public partial class SettingsWindow : Window
{
    private readonly bool _musicAvailable;
    private readonly Func<PetSettings, (PetSettings Settings, bool Saved)> _applySettings;
    private readonly Func<bool> _resetWindowPosition;
    private PetSettings _currentSettings;
    private bool _updatingControls;

    public SettingsWindow(
        PetSettings currentSettings,
        bool musicAvailable,
        Func<PetSettings, (PetSettings Settings, bool Saved)> applySettings,
        Func<bool> resetWindowPosition)
    {
        InitializeComponent();

        _currentSettings = Copy(currentSettings);
        _musicAvailable = musicAvailable;
        _applySettings = applySettings;
        _resetWindowPosition = resetWindowPosition;

        MusicCheckBox.IsEnabled = _musicAvailable;
        MusicAvailabilityText.Text = _musicAvailable
            ? "音乐素材已找到。"
            : "尚未添加 assets/audio/holiday-ambient.mp3，音乐功能暂不可用。";

        RefreshControls();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshControls();
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        var pendingLocationText = PreferredLocationTextBox.Text;
        var proposed = Copy(_currentSettings);
        proposed.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        proposed.SnowEnabled = SnowCheckBox.IsChecked == true;
        proposed.DecorativeLightsEnabled = LightsCheckBox.IsChecked == true;
        proposed.MusicEnabled = _musicAvailable && MusicCheckBox.IsChecked == true;
        proposed.ActivityAwarenessEnabled = ActivityAwarenessCheckBox.IsChecked == true;
        proposed.AutomaticIpLocationEnabled = AutomaticIpLocationCheckBox.IsChecked == true;
        proposed.WeatherRemindersEnabled = WeatherRemindersCheckBox.IsChecked == true;

        var result = _applySettings(proposed);
        _currentSettings = result.Settings;
        RefreshControls();
        if (!string.Equals(pendingLocationText, _currentSettings.PreferredLocation, StringComparison.Ordinal))
        {
            PreferredLocationTextBox.Text = pendingLocationText;
        }

        StatusText.Text = !result.Saved
            ? "设置已应用，但保存失败；请检查本地日志。"
            : proposed.MusicEnabled && !_currentSettings.MusicEnabled
                ? "音乐无法启动，设置已恢复为关闭。"
                : "设置已立即应用并保存。";
    }

    private void ApplyLocation_Click(object sender, RoutedEventArgs e)
    {
        var proposed = Copy(_currentSettings);
        proposed.PreferredLocation = PreferredLocationTextBox.Text.Trim();
        var result = _applySettings(proposed);
        _currentSettings = result.Settings;
        RefreshControls();
        StatusText.Text = !result.Saved
            ? "关注城市已应用，但保存失败；请检查本地日志。"
            : string.IsNullOrWhiteSpace(_currentSettings.PreferredLocation)
                ? "已清除手动关注城市；若启用 IP 定位，将尝试使用可信网络位置。"
                : "关注城市已保存；手动城市优先于 IP 自动定位。";
    }

    private void ResetPosition_Click(object sender, RoutedEventArgs e)
    {
        var saved = _resetWindowPosition();
        StatusText.Text = saved
            ? "桌宠已移动到默认位置。"
            : "位置已调整，但保存失败；请检查本地日志。";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RefreshControls()
    {
        _updatingControls = true;
        try
        {
            AlwaysOnTopCheckBox.IsChecked = _currentSettings.AlwaysOnTop;
            SnowCheckBox.IsChecked = _currentSettings.SnowEnabled;
            LightsCheckBox.IsChecked = _currentSettings.DecorativeLightsEnabled;
            MusicCheckBox.IsChecked = _currentSettings.MusicEnabled && _musicAvailable;
            ActivityAwarenessCheckBox.IsChecked = _currentSettings.ActivityAwarenessEnabled;
            AutomaticIpLocationCheckBox.IsChecked = _currentSettings.AutomaticIpLocationEnabled;
            WeatherRemindersCheckBox.IsChecked = _currentSettings.WeatherRemindersEnabled;
            PreferredLocationTextBox.Text = _currentSettings.PreferredLocation ?? string.Empty;
        }
        finally
        {
            _updatingControls = false;
        }
    }

    private static PetSettings Copy(PetSettings source) => new()
    {
        WindowLeft = source.WindowLeft,
        WindowTop = source.WindowTop,
        ActiveAvatarId = source.ActiveAvatarId,
        AlwaysOnTop = source.AlwaysOnTop,
        SnowEnabled = source.SnowEnabled,
        DecorativeLightsEnabled = source.DecorativeLightsEnabled,
        MusicEnabled = source.MusicEnabled,
        ActivityAwarenessEnabled = source.ActivityAwarenessEnabled,
        AutomaticIpLocationEnabled = source.AutomaticIpLocationEnabled,
        WeatherRemindersEnabled = source.WeatherRemindersEnabled,
        PreferredLocation = source.PreferredLocation,
        CachedLocationName = source.CachedLocationName,
        CachedLocationCity = source.CachedLocationCity,
        CachedLocationCountry = source.CachedLocationCountry,
        CachedLocationLatitude = source.CachedLocationLatitude,
        CachedLocationLongitude = source.CachedLocationLongitude,
        CachedLocationTimeZoneId = source.CachedLocationTimeZoneId,
        CachedLocationSource = source.CachedLocationSource,
        CachedLocationQuery = source.CachedLocationQuery,
        CachedLocationUpdatedAtUtc = source.CachedLocationUpdatedAtUtc,
        LastColdWeatherReminderAtUtc = source.LastColdWeatherReminderAtUtc,
        LastRainReminderAtUtc = source.LastRainReminderAtUtc,
        LastHeatReminderAtUtc = source.LastHeatReminderAtUtc,
        WindowWidth = source.WindowWidth,
        WindowHeight = source.WindowHeight
    };
}
