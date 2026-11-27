using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace BichonDesktopPet.App.Views;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly AvatarCatalogService _avatarCatalogService;
    private readonly SnowAnimationService _snowAnimation;
    private readonly AudioService _audioService = new();
    private readonly PetBehaviorEngine _behaviorEngine = new();
    private readonly PetBehaviorPresentationResolver _behaviorPresentationResolver = new();
    private readonly PetBehaviorAnimationService _behaviorAnimation;
    private readonly DispatcherTimer _behaviorTimer;
    private SettingsWindow? _settingsWindow;
    private AvatarManagerWindow? _avatarManagerWindow;
    private PetSettings _settings = new();
    private bool _allowExit;
    private bool _petImageLoaded;
    private bool _dragCandidate;
    private bool _wasDragged;
    private WpfPoint _mouseDownPosition;

    public MainWindow()
    {
        InitializeComponent();
        _settingsService = new SettingsService(log: AppLogger.Log);
        var userDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BichonDesktopPet");
        _avatarCatalogService = new AvatarCatalogService(
            Path.Combine(AppContext.BaseDirectory, "Assets"),
            userDataDirectory,
            AppLogger.Log);
        _snowAnimation = new SnowAnimationService(SnowCanvas);
        _behaviorAnimation = new PetBehaviorAnimationService(PetRotate, PetScale, PetTranslate);
        _behaviorTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _behaviorTimer.Tick += BehaviorTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsService.Load();
        _avatarCatalogService.Load();

        var activeAvatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId);
        var activeAvatarNeedsReset = activeAvatar is null ||
            (!activeAvatar.IsBuiltIn && _avatarCatalogService.ResolveImagePath(activeAvatar.Id) is null);

        if (activeAvatarNeedsReset)
        {
            _settings.ActiveAvatarId = AvatarCatalogService.DefaultAvatarId;
        }

        Topmost = _settings.AlwaysOnTop;

        var workArea = SystemParameters.WorkArea;
        Left = Clamp(_settings.WindowLeft ?? workArea.Right - Width - 24, workArea.Left, workArea.Right - Width);
        Top = Clamp(_settings.WindowTop ?? workArea.Bottom - Height - 24, workArea.Top, workArea.Bottom - Height);

        if (activeAvatarNeedsReset)
        {
            SaveSettings();
        }

        LoadPetImage();
        _snowAnimation.SetEnabled(_settings.SnowEnabled);
        SetLightsEnabled(_settings.DecorativeLightsEnabled);

        if (_settings.MusicEnabled)
        {
            try
            {
                if (!_audioService.SetEnabled(true))
                {
                    _settings.MusicEnabled = false;
                    AppLogger.Log("Saved music preference could not be restored because the audio asset is unavailable.");
                    SaveSettings();
                }
            }
            catch (Exception exception)
            {
                _settings.MusicEnabled = false;
                AppLogger.Log("Saved music preference could not be restored.", exception);
                SaveSettings();
            }
        }

        ApplyBehavior(_behaviorEngine.Start(DateTimeOffset.UtcNow));
        _behaviorTimer.Start();
    }

    private void LoadPetImage()
    {
        var avatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId)
            ?? _avatarCatalogService.GetAvatar(AvatarCatalogService.DefaultAvatarId);

        if (avatar is null)
        {
            _petImageLoaded = false;
            PetImage.Source = null;
            PetImage.Visibility = Visibility.Collapsed;
            FallbackPetCanvas.Visibility = Visibility.Visible;
            StatusText.Text = "默认角色不可用";
            return;
        }

        var imagePath = _avatarCatalogService.ResolveImagePath(avatar.Id);
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            _petImageLoaded = false;
            PetImage.Source = null;
            PetImage.Visibility = Visibility.Collapsed;
            FallbackPetCanvas.Visibility = Visibility.Visible;
            StatusText.Text = $"{avatar.Name}图片未找到";
            AppLogger.Log($"Avatar image file is missing for '{avatar.Id}'.");
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.DecodePixelWidth = 520;
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            PetImage.Source = bitmap;
            _petImageLoaded = true;
            PetImage.Visibility = Visibility.Visible;
            FallbackPetCanvas.Visibility = Visibility.Collapsed;
            StatusText.Text = $"{avatar.Name}，你好呀";
        }
        catch (Exception exception)
        {
            AppLogger.Log($"Unable to load avatar image '{avatar.Id}'.", exception);
            _petImageLoaded = false;
            PetImage.Source = null;
            PetImage.Visibility = Visibility.Collapsed;
            FallbackPetCanvas.Visibility = Visibility.Visible;
            StatusText.Text = $"{avatar.Name}素材加载失败";
        }
    }

    private void BehaviorTimer_Tick(object? sender, EventArgs e)
    {
        var previousState = _behaviorEngine.CurrentState;
        var currentState = _behaviorEngine.Advance(DateTimeOffset.UtcNow);

        if (currentState != previousState)
        {
            ApplyBehavior(currentState);
        }
    }

    private void ApplyBehavior(PetBehaviorState state)
    {
        var avatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId);
        var presentation = _behaviorPresentationResolver.Resolve(state, avatar?.Species);

        if (_petImageLoaded)
        {
            StatusText.Text = presentation.Message;
        }

        _behaviorAnimation.Apply(presentation.VisualEffect);
    }

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(value, max));

    private static void StartLightPulse(UIElement light, int delayMilliseconds)
    {
        var animation = new DoubleAnimation
        {
            From = 0.25,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.9),
            AutoReverse = true,
            BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
            RepeatBehavior = RepeatBehavior.Forever
        };

        light.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _wasDragged = false;
        _mouseDownPosition = e.GetPosition(this);
        _dragCandidate = true;
    }

    private void Window_PreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_dragCandidate || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        var deltaX = Math.Abs(current.X - _mouseDownPosition.X);
        var deltaY = Math.Abs(current.Y - _mouseDownPosition.Y);

        if (deltaX < SystemParameters.MinimumHorizontalDragDistance &&
            deltaY < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dragCandidate = false;
        _wasDragged = true;
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The mouse can be released before DragMove starts; this is harmless.
        }
    }

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragCandidate = false;
    }

    private void PetCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_wasDragged)
        {
            _wasDragged = false;
            return;
        }

        if (e.ClickCount >= 2)
        {
            GiftButton_Click(sender, e);
            return;
        }

        ApplyBehavior(_behaviorEngine.OnPetClicked(DateTimeOffset.UtcNow));
    }

    private void GiftButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyBehavior(_behaviorEngine.OnGiftOpened(DateTimeOffset.UtcNow));
    }

    public void ShowFromTray()
    {
        var wasHidden = !IsVisible;
        if (wasHidden)
        {
            Show();
            ApplyBehavior(_behaviorEngine.OnWake(DateTimeOffset.UtcNow));
        }

        WindowState = WindowState.Normal;
        Activate();
    }

    public void HideToTray()
    {
        _settingsWindow?.Close();
        _avatarManagerWindow?.Close();
        SaveSettings();
        Hide();
    }

    public void OpenSettingsWindow()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        if (!IsVisible)
        {
            ShowFromTray();
        }

        var settingsWindow = new SettingsWindow(
            CopySettings(_settings),
            _audioService.IsAvailable,
            ApplySettings,
            ResetWindowPosition)
        {
            Owner = this
        };

        _settingsWindow = settingsWindow;
        settingsWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, settingsWindow))
            {
                _settingsWindow = null;
            }
        };

        settingsWindow.Show();
        settingsWindow.Activate();
    }

    public void OpenAvatarManagerWindow()
    {
        if (_avatarManagerWindow is { IsVisible: true })
        {
            _avatarManagerWindow.Activate();
            return;
        }

        if (!IsVisible)
        {
            ShowFromTray();
        }

        var manager = new AvatarManagerWindow(
            _avatarCatalogService,
            SetActiveAvatar,
            DeleteAvatar,
            () => _settings.ActiveAvatarId)
        {
            Owner = this
        };

        _avatarManagerWindow = manager;
        manager.Closed += (_, _) =>
        {
            if (ReferenceEquals(_avatarManagerWindow, manager))
            {
                _avatarManagerWindow = null;
            }
        };

        manager.Show();
        manager.Activate();
    }

    private (bool Success, bool Saved) SetActiveAvatar(string avatarId)
    {
        var avatar = _avatarCatalogService.GetAvatar(avatarId);
        if (avatar is null ||
            (!avatar.IsBuiltIn && _avatarCatalogService.ResolveImagePath(avatarId) is null))
        {
            return (false, false);
        }

        _settings.ActiveAvatarId = avatar.Id;
        LoadPetImage();
        ApplyBehavior(_behaviorEngine.CurrentState);
        var saved = SaveSettings();
        return (true, saved);
    }

    private (bool Success, bool Saved) DeleteAvatar(string avatarId)
    {
        var wasActive = string.Equals(
            _settings.ActiveAvatarId,
            avatarId,
            StringComparison.OrdinalIgnoreCase);

        if (!_avatarCatalogService.Delete(avatarId))
        {
            return (false, false);
        }

        if (!wasActive)
        {
            return (true, true);
        }

        _settings.ActiveAvatarId = AvatarCatalogService.DefaultAvatarId;
        LoadPetImage();
        ApplyBehavior(_behaviorEngine.CurrentState);
        return (true, SaveSettings());
    }

    private (PetSettings Settings, bool Saved) ApplySettings(PetSettings requested)
    {
        var musicChanged = requested.MusicEnabled != _settings.MusicEnabled;

        _settings.AlwaysOnTop = requested.AlwaysOnTop;
        _settings.SnowEnabled = requested.SnowEnabled;
        _settings.DecorativeLightsEnabled = requested.DecorativeLightsEnabled;
        _settings.MusicEnabled = requested.MusicEnabled;

        Topmost = _settings.AlwaysOnTop;
        _snowAnimation.SetEnabled(_settings.SnowEnabled);
        SetLightsEnabled(_settings.DecorativeLightsEnabled);

        if (musicChanged)
        {
            try
            {
                if (_settings.MusicEnabled)
                {
                    if (!_audioService.SetEnabled(true))
                    {
                        _settings.MusicEnabled = false;
                        StatusText.Text = "音乐素材尚未加入";
                    }
                }
                else
                {
                    _audioService.SetEnabled(false);
                }
            }
            catch (Exception exception)
            {
                _settings.MusicEnabled = false;
                AppLogger.Log("Unable to apply the background music setting.", exception);
                StatusText.Text = "音乐启动失败，请检查本地日志";
            }
        }

        var saved = SaveSettings();
        return (CopySettings(_settings), saved);
    }

    private bool ResetWindowPosition()
    {
        var workArea = SystemParameters.WorkArea;
        Left = Clamp(workArea.Right - Width - 24, workArea.Left, workArea.Right - Width);
        Top = Clamp(workArea.Bottom - Height - 24, workArea.Top, workArea.Bottom - Height);
        return SaveSettings();
    }

    private static PetSettings CopySettings(PetSettings source) => new()
    {
        WindowLeft = source.WindowLeft,
        WindowTop = source.WindowTop,
        ActiveAvatarId = source.ActiveAvatarId,
        AlwaysOnTop = source.AlwaysOnTop,
        SnowEnabled = source.SnowEnabled,
        DecorativeLightsEnabled = source.DecorativeLightsEnabled,
        MusicEnabled = source.MusicEnabled,
        WindowWidth = source.WindowWidth,
        WindowHeight = source.WindowHeight
    };

    public void ToggleSnow()
    {
        _settings.SnowEnabled = !_settings.SnowEnabled;
        _snowAnimation.SetEnabled(_settings.SnowEnabled);
        StatusText.Text = _settings.SnowEnabled ? "雪花飘起来啦" : "雪花暂时休息";
        SaveSettings();
    }

    public void ToggleLights()
    {
        _settings.DecorativeLightsEnabled = !_settings.DecorativeLightsEnabled;
        SetLightsEnabled(_settings.DecorativeLightsEnabled);
        StatusText.Text = _settings.DecorativeLightsEnabled ? "小灯亮起来啦" : "装饰灯已关闭";
        SaveSettings();
    }

    public void ToggleMusic()
    {
        var proposed = CopySettings(_settings);
        proposed.MusicEnabled = !_settings.MusicEnabled;
        var result = ApplySettings(proposed);

        StatusText.Text = !result.Saved
            ? "设置已应用，但保存失败，请检查本地日志"
            : result.Settings.MusicEnabled
                ? "音乐播放中"
                : proposed.MusicEnabled
                    ? "音乐素材不可用或启动失败"
                    : "音乐已关闭";
    }

    private void SetLightsEnabled(bool enabled)
    {
        var lights = new[] { LightOne, LightTwo, LightThree, LightFour };
        for (var index = 0; index < lights.Length; index++)
        {
            if (enabled)
            {
                StartLightPulse(lights[index], index * 250);
            }
            else
            {
                lights[index].BeginAnimation(UIElement.OpacityProperty, null);
                lights[index].Opacity = 0.12;
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();
    private void Avatars_Click(object sender, RoutedEventArgs e) => OpenAvatarManagerWindow();
    private void ToggleSnow_Click(object sender, RoutedEventArgs e) => ToggleSnow();
    private void ToggleLights_Click(object sender, RoutedEventArgs e) => ToggleLights();
    private void ToggleMusic_Click(object sender, RoutedEventArgs e) => ToggleMusic();
    private void Hide_Click(object sender, RoutedEventArgs e) => HideToTray();
    private void Exit_Click(object sender, RoutedEventArgs e) => ExitApplication();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        SaveSettings();

        if (!_allowExit)
        {
            e.Cancel = true;
            _settingsWindow?.Close();
            _avatarManagerWindow?.Close();
            Hide();
        }
    }

    public void ExitApplication()
    {
        _allowExit = true;
        _settingsWindow?.Close();
        _avatarManagerWindow?.Close();
        SaveSettings();
        _behaviorTimer.Stop();
        _behaviorTimer.Tick -= BehaviorTimer_Tick;
        _behaviorAnimation.Dispose();
        _snowAnimation.Dispose();
        _audioService.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private bool SaveSettings()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.AlwaysOnTop = Topmost;
        return _settingsService.Save(_settings);
    }
}
