using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace BichonDesktopPet.App.Views;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly SnowAnimationService _snowAnimation;
    private readonly AudioService _audioService = new();
    private SettingsWindow? _settingsWindow;
    private PetSettings _settings = new();
    private bool _allowExit;
    private bool _dragCandidate;
    private bool _wasDragged;
    private WpfPoint _mouseDownPosition;

    public MainWindow()
    {
        InitializeComponent();
        _settingsService = new SettingsService(log: AppLogger.Log);
        _snowAnimation = new SnowAnimationService(SnowCanvas);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPetImage();

        _settings = _settingsService.Load();
        Topmost = _settings.AlwaysOnTop;

        var workArea = SystemParameters.WorkArea;
        Left = Clamp(_settings.WindowLeft ?? workArea.Right - Width - 24, workArea.Left, workArea.Right - Width);
        Top = Clamp(_settings.WindowTop ?? workArea.Bottom - Height - 24, workArea.Top, workArea.Bottom - Height);

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
    }

    private void LoadPetImage()
    {
        var imagePath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "characters",
            "bichon",
            "base",
            "bichon-idle.png");

        if (!File.Exists(imagePath))
        {
            FallbackPetCanvas.Visibility = Visibility.Visible;
            PetImage.Visibility = Visibility.Collapsed;
            StatusText.Text = "比熊图片素材未找到";
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            PetImage.Source = bitmap;
            PetImage.Visibility = Visibility.Visible;
            FallbackPetCanvas.Visibility = Visibility.Collapsed;
            StatusText.Text = "汪！你好呀";
        }
        catch (Exception exception)
        {
            AppLogger.Log("Unable to load the Bichon runtime image.", exception);
            PetImage.Source = null;
            PetImage.Visibility = Visibility.Collapsed;
            FallbackPetCanvas.Visibility = Visibility.Visible;
            StatusText.Text = "比熊素材加载失败";
        }
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

        StatusText.Text = "嘿嘿，摸摸我！";
        var animation = new DoubleAnimation
        {
            From = -3,
            To = 3,
            Duration = TimeSpan.FromMilliseconds(160),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2)
        };

        PetRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
    }

    private void GiftButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "汪！礼物打开啦 ✨";
        var animation = new DoubleAnimation
        {
            From = -5,
            To = 5,
            Duration = TimeSpan.FromMilliseconds(120),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(3)
        };

        PetRotate.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
    }

    public void ShowFromTray()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
    }

    public void HideToTray()
    {
        _settingsWindow?.Close();
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

    private PetSettings ApplySettings(PetSettings requested)
    {
        _settings.AlwaysOnTop = requested.AlwaysOnTop;
        _settings.SnowEnabled = requested.SnowEnabled;
        _settings.DecorativeLightsEnabled = requested.DecorativeLightsEnabled;
        _settings.MusicEnabled = requested.MusicEnabled;

        Topmost = _settings.AlwaysOnTop;
        _snowAnimation.SetEnabled(_settings.SnowEnabled);
        SetLightsEnabled(_settings.DecorativeLightsEnabled);

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

        SaveSettings();
        return CopySettings(_settings);
    }

    private void ResetWindowPosition()
    {
        var workArea = SystemParameters.WorkArea;
        Left = Clamp(workArea.Right - Width - 24, workArea.Left, workArea.Right - Width);
        Top = Clamp(workArea.Bottom - Height - 24, workArea.Top, workArea.Bottom - Height);
        SaveSettings();
    }

    private static PetSettings CopySettings(PetSettings source) => new()
    {
        WindowLeft = source.WindowLeft,
        WindowTop = source.WindowTop,
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
        var applied = ApplySettings(proposed);

        StatusText.Text = applied.MusicEnabled
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
            Hide();
        }
    }

    public void ExitApplication()
    {
        _allowExit = true;
        _settingsWindow?.Close();
        SaveSettings();
        _snowAnimation.Dispose();
        _audioService.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void SaveSettings()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.AlwaysOnTop = Topmost;
        _settingsService.Save(_settings);
    }
}
