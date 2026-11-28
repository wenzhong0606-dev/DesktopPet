using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace BichonDesktopPet.App.Views;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly AvatarCatalogService _avatarCatalogService;
    private readonly CompanionIdentityStore _companionIdentityStore;
    private readonly ChatHistoryStore _chatHistoryStore;
    private PersonalityGrowthStore? _growthStore;
    private readonly SnowAnimationService _snowAnimation;
    private readonly AudioService _audioService = new();
    private readonly PetBehaviorEngine _behaviorEngine = new();
    private readonly ActivityRhythmEngine _activityRhythmEngine = new();
    private readonly WindowsActivityMonitor _activityMonitor = new();
    private DateTimeOffset _lastActivitySampleAt = DateTimeOffset.MinValue;
    private readonly HttpClient _careHttpClient;
    private readonly IpLocationService _ipLocationService;
    private readonly OpenMeteoWeatherService _weatherService;
    private readonly NetworkPrivacyInspector _networkPrivacyInspector = new();
    private readonly WeatherReminderPolicy _weatherReminderPolicy = new();
    private DateTimeOffset _lastWeatherRefreshAt = DateTimeOffset.MinValue;
    private bool _weatherRefreshInProgress;
    private bool _weatherRefreshPending;
    private CancellationTokenSource? _weatherRefreshCancellation;
    private readonly PetBehaviorPresentationResolver _behaviorPresentationResolver = new();
    private readonly PetBehaviorAnimationService _behaviorAnimation;
    private readonly DispatcherTimer _behaviorTimer;
    private readonly PetClickGestureRecognizer _clickGestureRecognizer;
    private readonly DispatcherTimer _clickGestureTimer;
    private SettingsWindow? _settingsWindow;
    private AvatarManagerWindow? _avatarManagerWindow;
    private ChatWindow? _chatWindow;
    private PetSettings _settings = new();
    private CompanionIdentity? _activeCompanion;
    private bool _chatHistoryReady;
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
        var companionDatabasePath = Path.Combine(userDataDirectory, "companions.db");
        _companionIdentityStore = new CompanionIdentityStore(companionDatabasePath);
        _chatHistoryStore = new ChatHistoryStore(companionDatabasePath);
        _careHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(35) };
        _ipLocationService = new IpLocationService(_careHttpClient);
        _weatherService = new OpenMeteoWeatherService(_careHttpClient);
        _snowAnimation = new SnowAnimationService(SnowCanvas);
        _behaviorAnimation = new PetBehaviorAnimationService(PetRotate, PetScale, PetTranslate);
        _behaviorTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _behaviorTimer.Tick += BehaviorTimer_Tick;

        var doubleClickInterval = TimeSpan.FromMilliseconds(
            Math.Max(1, System.Windows.Forms.SystemInformation.DoubleClickTime));
        _clickGestureRecognizer = new PetClickGestureRecognizer(
            doubleClickInterval,
            Math.Max(1, System.Windows.Forms.SystemInformation.DoubleClickSize.Width),
            Math.Max(1, System.Windows.Forms.SystemInformation.DoubleClickSize.Height));
        _clickGestureTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = doubleClickInterval
        };
        _clickGestureTimer.Tick += ClickGestureTimer_Tick;
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

        InitializeCompanionIdentities();
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
        if (_settings.WeatherRemindersEnabled)
        {
            _ = RefreshWeatherAsync(forceLocationRefresh: false);
        }
    }

    private void InitializeCompanionIdentities()
    {
        try
        {
            _companionIdentityStore.Initialize();
            foreach (var avatar in _avatarCatalogService.GetAvatars())
            {
                _companionIdentityStore.EnsureCompanion(avatar);
            }

            _activeCompanion = _companionIdentityStore.GetByAvatarId(_settings.ActiveAvatarId);
        }
        catch (Exception exception)
        {
            _activeCompanion = null;
            AppLogger.Log("Unable to initialize the local companion identity store. The desktop pet will continue without its interaction journal.", exception);
            return;
        }

        try
        {
            _chatHistoryStore.Initialize();
            _chatHistoryReady = true;
            try
            {
                var growth = new PersonalityGrowthStore(_chatHistoryStore.DatabasePath);
                growth.Initialize();
                _growthStore = growth;
            }
            catch (Exception) { /* Optional growth must not affect core behavior. */ }
        }
        catch (Exception exception)
        {
            _chatHistoryReady = false;
            AppLogger.Log("Unable to initialize local chat history. Chat history features will remain unavailable.", exception);
        }
    }

    private void SetActiveCompanion(AvatarProfile avatar)
    {
        try
        {
            _activeCompanion = _companionIdentityStore.EnsureCompanion(avatar);
        }
        catch (Exception exception)
        {
            _activeCompanion = null;
            AppLogger.Log("Unable to resolve the active companion identity.", exception);
        }
    }

    private void RecordInteraction(string eventType, string payloadJson = "{}")
    {
        if (_activeCompanion is null)
        {
            return;
        }

        try
        {
            _companionIdentityStore.RecordInteraction(
                _activeCompanion.Id,
                eventType,
                payloadJson);
        }
        catch (Exception exception)
        {
            AppLogger.Log($"Unable to record companion interaction '{eventType}'.", exception);
        }
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
            var petNickname = ChatWelcomeMessageBuilder.GetPrimaryNickname(avatar);
            StatusText.Text = string.IsNullOrWhiteSpace(petNickname)
                ? "你好呀"
                : $"{petNickname}，你好呀";
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

    private void ClickGestureTimer_Tick(object? sender, EventArgs e)
    {
        if (_clickGestureRecognizer.TryCompleteSingleClick(DateTimeOffset.UtcNow))
        {
            _clickGestureTimer.Stop();
            HandleSinglePetClick();
        }
    }

    private void HandleSinglePetClick()
    {
        ApplyBehavior(_behaviorEngine.OnPetClicked(DateTimeOffset.UtcNow));
        RecordInteraction("pet_clicked", "{\"clickCount\":1}");
    }

    private void FlushPendingPetClick()
    {
        _clickGestureTimer.Stop();
        if (_clickGestureRecognizer.FlushPendingSingleClick())
        {
            HandleSinglePetClick();
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

        if (!_settings.ActivityAwarenessEnabled)
        {
            _activityRhythmEngine.Reset();
        }
        else
        {
            var localNow = DateTimeOffset.Now;
            if (localNow - _lastActivitySampleAt >= TimeSpan.FromSeconds(30))
            {
                _lastActivitySampleAt = localNow;
                var idleDuration = _activityMonitor.GetIdleDuration();
                if (idleDuration is { } idle)
                {
                    var reminder = _activityRhythmEngine.Observe(localNow, idle);
                    if (reminder?.Type == CareReminderType.TakeBreak)
                    {
                        var avatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId);
                        ShowCareReminder(CareReminderMessageBuilder.BuildBreakReminder(avatar, ReadActiveGrowth()), "桌面伙伴 · 休息提醒");
                    }
                }
            }
        }

        var utcNow = DateTimeOffset.UtcNow;
        if (_settings.WeatherRemindersEnabled &&
            !_weatherRefreshInProgress &&
            utcNow - _lastWeatherRefreshAt >= TimeSpan.FromHours(1))
        {
            _ = RefreshWeatherAsync(forceLocationRefresh: false);
        }
    }

    private PersonalityGrowthProfile? ReadActiveGrowth()
    {
        try { return _activeCompanion is { } companion ? _growthStore?.Read(companion.Id) : null; }
        catch (Exception) { return null; }
    }

    private void ShowCareReminder(string message, string title = "桌面伙伴 · 关怀提醒")
    {
        StatusText.Text = message;
        if (!IsActive)
        {
            (System.Windows.Application.Current as App)?.ShowTrayNotification(title, message);
        }
    }

    private async Task RefreshWeatherAsync(bool forceLocationRefresh)
    {
        if (!_settings.WeatherRemindersEnabled || _allowExit)
        {
            return;
        }

        if (_weatherRefreshInProgress)
        {
            return;
        }

        _weatherRefreshInProgress = true;
        var refreshCancellation = new CancellationTokenSource();
        _weatherRefreshCancellation = refreshCancellation;
        var cancellationToken = refreshCancellation.Token;

        try
        {
            var location = await ResolveCareLocationAsync(forceLocationRefresh, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!_settings.WeatherRemindersEnabled || location is null)
            {
                if (_settings.WeatherRemindersEnabled &&
                    string.IsNullOrWhiteSpace(_settings.PreferredLocation) &&
                    !_settings.AutomaticIpLocationEnabled)
                {
                    StatusText.Text = "天气提醒已开启；请设置关注城市或允许 IP 自动定位。";
                }

                _lastWeatherRefreshAt = DateTimeOffset.UtcNow;
                return;
            }

            var snapshot = await _weatherService.GetForecastAsync(location, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!_settings.WeatherRemindersEnabled)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            _lastWeatherRefreshAt = now;
            var alert = _weatherReminderPolicy.Evaluate(snapshot, now);
            if (alert is null || !TryMarkWeatherReminder(alert.Type, now))
            {
                return;
            }

            var avatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId);
            ShowCareReminder(
                CareReminderMessageBuilder.BuildWeatherReminder(avatar, location, alert, ReadActiveGrowth()),
                "桌面伙伴 · 天气提醒");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Disabling weather care or changing its location cancels the in-flight request.
        }
        catch (OperationCanceledException exception)
        {
            _lastWeatherRefreshAt = DateTimeOffset.UtcNow;
            AppLogger.Log("Weather and location refresh timed out.", exception);
        }
        catch (Exception exception)
        {
            _lastWeatherRefreshAt = DateTimeOffset.UtcNow;
            AppLogger.Log("Weather and location refresh failed; no weather reminder was issued.", exception);
        }
        finally
        {
            if (ReferenceEquals(_weatherRefreshCancellation, refreshCancellation))
            {
                _weatherRefreshCancellation = null;
            }

            refreshCancellation.Dispose();
            _weatherRefreshInProgress = false;

            if (_weatherRefreshPending && _settings.WeatherRemindersEnabled && !_allowExit)
            {
                _weatherRefreshPending = false;
                _ = RefreshWeatherAsync(forceLocationRefresh: true);
            }
            else
            {
                _weatherRefreshPending = false;
            }
        }
    }

    private bool TryMarkWeatherReminder(CareReminderType type, DateTimeOffset now)
    {
        var lastReminderAt = type switch
        {
            CareReminderType.ColdWeather => _settings.LastColdWeatherReminderAtUtc,
            CareReminderType.RainExpected => _settings.LastRainReminderAtUtc,
            CareReminderType.HeatWarning => _settings.LastHeatReminderAtUtc,
            _ => null
        };

        if (lastReminderAt is not null &&
            now >= lastReminderAt.Value &&
            now - lastReminderAt.Value < TimeSpan.FromHours(6))
        {
            return false;
        }

        switch (type)
        {
            case CareReminderType.ColdWeather:
                _settings.LastColdWeatherReminderAtUtc = now;
                break;
            case CareReminderType.RainExpected:
                _settings.LastRainReminderAtUtc = now;
                break;
            case CareReminderType.HeatWarning:
                _settings.LastHeatReminderAtUtc = now;
                break;
            default:
                return false;
        }

        SaveSettings();
        return true;
    }

    private async Task<CareLocation?> ResolveCareLocationAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var manualQuery = _settings.PreferredLocation?.Trim() ?? string.Empty;
        if (manualQuery.Length > 0)
        {
            var cachedManual = GetCachedLocation("manual");
            if (!forceRefresh &&
                cachedManual is not null &&
                string.Equals(_settings.CachedLocationQuery, manualQuery, StringComparison.OrdinalIgnoreCase) &&
                _settings.CachedLocationUpdatedAtUtc is { } manualUpdatedAt &&
                now >= manualUpdatedAt &&
                now - manualUpdatedAt < TimeSpan.FromDays(30))
            {
                return cachedManual;
            }

            var resolvedManual = await _weatherService.ResolveCityAsync(manualQuery, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (resolvedManual is null)
            {
                StatusText.Text = "没有找到这个城市；请补充省份或国家/地区名称。";
                return null;
            }

            SaveCachedLocation(resolvedManual, manualQuery);
            return resolvedManual;
        }

        if (!_settings.AutomaticIpLocationEnabled)
        {
            return null;
        }

        var cachedIp = GetCachedLocation("ip");
        var recentCachedIp = GetRecentCachedIpLocation(now);
        if (!forceRefresh &&
            cachedIp is not null &&
            _settings.CachedLocationUpdatedAtUtc is { } ipUpdatedAt &&
            now >= ipUpdatedAt &&
            now - ipUpdatedAt < TimeSpan.FromHours(12))
        {
            return cachedIp;
        }

        var localRouteWarning = _networkPrivacyInspector.GetPotentialAnonymizationReason();
        if (localRouteWarning is not null)
        {
            StatusText.Text = $"{localRouteWarning}；为避免定位到代理出口，暂不更新自动位置。";
            return recentCachedIp;
        }

        var ipResult = await _ipLocationService.ResolveCurrentAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ipResult.IsTrusted || string.IsNullOrWhiteSpace(ipResult.City) ||
            string.IsNullOrWhiteSpace(ipResult.Country))
        {
            StatusText.Text = $"{ipResult.RejectionReason ?? "无法确认网络位置"}；未采用出口城市，可在设置中手动指定关注城市。";
            return recentCachedIp;
        }

        // IPLogs provides a coarse city/country. Resolve city coordinates separately;
        // never infer coordinates from a VPN exit or persist the raw public IP.
        var geocodedIpCity = await _weatherService.ResolveCityAsync($"{ipResult.City}, {ipResult.Country}", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (geocodedIpCity is null)
        {
            StatusText.Text = "已确认网络出口可信，但无法将其城市解析为天气坐标；请手动指定关注城市。";
            return recentCachedIp;
        }

        if (!IsIpLocationTimeZoneConsistent(geocodedIpCity))
        {
            StatusText.Text = "自动 IP 城市的时区与 Windows 本机时区差异较大，可能存在 VPN/代理；未采用出口城市。";
            return recentCachedIp;
        }

        var trustedLocation = geocodedIpCity with { Source = "ip" };
        SaveCachedLocation(trustedLocation, query: null);
        return trustedLocation;
    }

    private static bool IsIpLocationTimeZoneConsistent(CareLocation location)
    {
        if (string.IsNullOrWhiteSpace(location.TimeZoneId) ||
            string.Equals(location.TimeZoneId, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var cityTimeZone = TimeZoneInfo.FindSystemTimeZoneById(location.TimeZoneId);
            var now = DateTimeOffset.UtcNow;
            var systemOffset = TimeZoneInfo.Local.GetUtcOffset(now);
            var cityOffset = cityTimeZone.GetUtcOffset(now);
            return LocationTrustPolicy.IsTimeZoneConsistent(systemOffset, cityOffset);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private CareLocation? GetRecentCachedIpLocation(DateTimeOffset now)
    {
        if (_settings.CachedLocationUpdatedAtUtc is not { } updatedAt ||
            now < updatedAt ||
            now - updatedAt > TimeSpan.FromDays(7))
        {
            return null;
        }

        return GetCachedLocation("ip");
    }

    private CareLocation? GetCachedLocation(string? requiredSource = null)
    {
        if (_settings.CachedLocationLatitude is not { } latitude ||
            _settings.CachedLocationLongitude is not { } longitude ||
            string.IsNullOrWhiteSpace(_settings.CachedLocationCity) ||
            string.IsNullOrWhiteSpace(_settings.CachedLocationCountry))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(requiredSource) &&
            !string.Equals(_settings.CachedLocationSource, requiredSource, StringComparison.Ordinal))
        {
            return null;
        }

        return new CareLocation(
            _settings.CachedLocationName ?? $"{_settings.CachedLocationCity}, {_settings.CachedLocationCountry}",
            _settings.CachedLocationCity,
            _settings.CachedLocationCountry,
            latitude,
            longitude,
            _settings.CachedLocationTimeZoneId ?? "auto",
            _settings.CachedLocationSource ?? "cached",
            _settings.CachedLocationUpdatedAtUtc ?? DateTimeOffset.MinValue);
    }

    private void SaveCachedLocation(CareLocation location, string? query)
    {
        _settings.CachedLocationName = location.DisplayName;
        _settings.CachedLocationCity = location.City;
        _settings.CachedLocationCountry = location.Country;
        _settings.CachedLocationLatitude = location.Latitude;
        _settings.CachedLocationLongitude = location.Longitude;
        _settings.CachedLocationTimeZoneId = location.TimeZoneId;
        _settings.CachedLocationSource = location.Source;
        _settings.CachedLocationQuery = query;
        _settings.CachedLocationUpdatedAtUtc = DateTimeOffset.UtcNow;
        SaveSettings();
    }

    private void ApplyBehavior(PetBehaviorState state)
    {
        var avatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId);
        var presentation = _behaviorPresentationResolver.Resolve(state, avatar);

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

        var position = e.GetPosition(PetCanvas);
        var result = _clickGestureRecognizer.RegisterClick(
            DateTimeOffset.UtcNow,
            position.X,
            position.Y);

        if (result.CompletedSingleClicks > 0)
        {
            HandleSinglePetClick();
        }

        if (result.IsDoubleClick)
        {
            _clickGestureTimer.Stop();
            RecordInteraction("pet_double_clicked", "{\"action\":\"open_gift\"}");
            OpenGift("pet_double_click");
            return;
        }

        if (result.HasPendingSingleClick)
        {
            _clickGestureTimer.Stop();
            _clickGestureTimer.Start();
        }
    }

    private void GiftButton_Click(object sender, RoutedEventArgs e) =>
        OpenGift("context_menu");

    private void OpenGift(string source)
    {
        ApplyBehavior(_behaviorEngine.OnGiftOpened(DateTimeOffset.UtcNow));
        RecordInteraction("gift_opened", $"{{\"source\":\"{source}\"}}");
    }

    public void ShowFromTray()
    {
        var wasHidden = !IsVisible;
        if (wasHidden)
        {
            Show();
            ApplyBehavior(_behaviorEngine.OnWake(DateTimeOffset.UtcNow));
            RecordInteraction("woke_from_tray");
        }

        WindowState = WindowState.Normal;
        Activate();
    }

    public void HideToTray()
    {
        if (!IsVisible)
        {
            return;
        }

        FlushPendingPetClick();
        _settingsWindow?.Close();
        _avatarManagerWindow?.Close();
        _chatWindow?.Close();
        SaveSettings();
        RecordInteraction("hidden_to_tray", "{\"source\":\"user_action\"}");
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

    public void OpenChatWindow()
    {
        if (_chatWindow is { IsVisible: true })
        {
            _chatWindow.Activate();
            return;
        }

        if (!IsVisible)
        {
            ShowFromTray();
        }

        var avatar = _avatarCatalogService.GetAvatar(_settings.ActiveAvatarId);
        if (avatar is null)
        {
            System.Windows.MessageBox.Show("当前角色不可用，暂时无法打开聊天窗口。", "桌面伙伴", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var app = System.Windows.Application.Current as App;
        var provider = app?.AiChatProvider;
        if (provider is null)
        {
            System.Windows.MessageBox.Show("AI 服务初始化失败。桌宠仍可离线使用，请检查本地日志。", "桌面伙伴", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _activeCompanion ??= _companionIdentityStore.EnsureCompanion(avatar);
        }
        catch (Exception exception)
        {
            AppLogger.Log("Unable to load the companion identity for chat.", exception);
            System.Windows.MessageBox.Show("伙伴身份数据暂时不可用，请检查本地日志。", "桌面伙伴", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_chatHistoryReady)
        {
            System.Windows.MessageBox.Show("本地聊天记录数据库初始化失败，暂时无法打开聊天窗口。桌宠其他功能不受影响。", "桌面伙伴", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var companion = _activeCompanion;
        if (companion is null)
        {
            System.Windows.MessageBox.Show("伙伴身份数据暂时不可用，请稍后重试。", "桌面伙伴", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ChatWindow chat;
        try
        {
            chat = new ChatWindow(provider, companion, avatar, () => _behaviorEngine.CurrentState, _chatHistoryStore)
            {
                Owner = this
            };
        }
        catch (Exception exception)
        {
            AppLogger.Log("Unable to open the chat window or load local chat history.", exception);
            System.Windows.MessageBox.Show("聊天窗口打开失败，请检查本地聊天数据库和应用日志。", "桌面伙伴", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _chatWindow = chat;
        chat.Closed += (_, _) =>
        {
            if (ReferenceEquals(_chatWindow, chat))
            {
                _chatWindow = null;
            }
        };
        chat.Show();
        chat.Activate();
    }

    private (bool Success, bool Saved) SetActiveAvatar(string avatarId)
    {
        var avatar = _avatarCatalogService.GetAvatar(avatarId);
        if (avatar is null ||
            (!avatar.IsBuiltIn && _avatarCatalogService.ResolveImagePath(avatarId) is null))
        {
            return (false, false);
        }

        FlushPendingPetClick();
        _chatWindow?.Close();
        _settings.ActiveAvatarId = avatar.Id;
        SetActiveCompanion(avatar);
        LoadPetImage();
        ApplyBehavior(_behaviorEngine.CurrentState);
        var saved = SaveSettings();
        return (true, saved);
    }

    private (bool Success, bool Saved) DeleteAvatar(string avatarId)
    {
        FlushPendingPetClick();
        var wasActive = string.Equals(
            _settings.ActiveAvatarId,
            avatarId,
            StringComparison.OrdinalIgnoreCase);

        if (wasActive)
        {
            _chatWindow?.Close();
        }

        if (!_avatarCatalogService.Delete(avatarId))
        {
            return (false, false);
        }

        try
        {
            _companionIdentityStore.ArchiveByAvatarId(avatarId);
        }
        catch (Exception exception)
        {
            AppLogger.Log("Unable to archive the removed companion identity; its local record was retained.", exception);
        }

        if (!wasActive)
        {
            return (true, true);
        }

        _settings.ActiveAvatarId = AvatarCatalogService.DefaultAvatarId;
        var defaultAvatar = _avatarCatalogService.GetAvatar(AvatarCatalogService.DefaultAvatarId);
        if (defaultAvatar is not null)
        {
            SetActiveCompanion(defaultAvatar);
        }

        LoadPetImage();
        ApplyBehavior(_behaviorEngine.CurrentState);
        return (true, SaveSettings());
    }

    private (PetSettings Settings, bool Saved) ApplySettings(PetSettings requested)
    {
        var musicChanged = requested.MusicEnabled != _settings.MusicEnabled;
        var weatherSettingsChanged =
            requested.AutomaticIpLocationEnabled != _settings.AutomaticIpLocationEnabled ||
            requested.WeatherRemindersEnabled != _settings.WeatherRemindersEnabled ||
            !string.Equals(requested.PreferredLocation, _settings.PreferredLocation, StringComparison.Ordinal);

        _settings.AlwaysOnTop = requested.AlwaysOnTop;
        _settings.SnowEnabled = requested.SnowEnabled;
        _settings.DecorativeLightsEnabled = requested.DecorativeLightsEnabled;
        _settings.MusicEnabled = requested.MusicEnabled;
        _settings.ActivityAwarenessEnabled = requested.ActivityAwarenessEnabled;
        _settings.AutomaticIpLocationEnabled = requested.AutomaticIpLocationEnabled;
        _settings.WeatherRemindersEnabled = requested.WeatherRemindersEnabled;
        _settings.PreferredLocation = requested.PreferredLocation?.Trim() ?? string.Empty;

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
        if (weatherSettingsChanged)
        {
            _lastWeatherRefreshAt = DateTimeOffset.MinValue;
            _weatherRefreshCancellation?.Cancel();

            if (_settings.WeatherRemindersEnabled)
            {
                if (_weatherRefreshInProgress)
                {
                    _weatherRefreshPending = true;
                }
                else
                {
                    _ = RefreshWeatherAsync(forceLocationRefresh: true);
                }
            }
            else
            {
                _weatherRefreshPending = false;
            }
        }

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
        WindowHeight = source.WindowHeight,
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
        LastHeatReminderAtUtc = source.LastHeatReminderAtUtc
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
    private void Chat_Click(object sender, RoutedEventArgs e) => OpenChatWindow();
    private void Avatars_Click(object sender, RoutedEventArgs e) => OpenAvatarManagerWindow();
    private void ToggleSnow_Click(object sender, RoutedEventArgs e) => ToggleSnow();
    private void ToggleLights_Click(object sender, RoutedEventArgs e) => ToggleLights();
    private void ToggleMusic_Click(object sender, RoutedEventArgs e) => ToggleMusic();
    private void Hide_Click(object sender, RoutedEventArgs e) => HideToTray();
    private void Exit_Click(object sender, RoutedEventArgs e) => ExitApplication();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowExit)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        SaveSettings();
    }

    public void ExitApplication()
    {
        if (_allowExit)
        {
            return;
        }

        FlushPendingPetClick();
        _allowExit = true;
        RecordInteraction("application_exited", "{\"source\":\"explicit_exit\"}");
        _settingsWindow?.Close();
        _avatarManagerWindow?.Close();
        _chatWindow?.Close();
        SaveSettings();
        _behaviorTimer.Stop();
        _behaviorTimer.Tick -= BehaviorTimer_Tick;
        _clickGestureTimer.Stop();
        _clickGestureTimer.Tick -= ClickGestureTimer_Tick;
        _activityRhythmEngine.Reset();
        _weatherRefreshPending = false;
        _weatherRefreshCancellation?.Cancel();
        _behaviorAnimation.Dispose();
        _snowAnimation.Dispose();
        _audioService.Dispose();
        _careHttpClient.Dispose();
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
