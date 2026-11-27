using System;
using System.Threading.Tasks;
using System.Windows;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.App.Views;
using BichonDesktopPet.Core.AI;
using WpfApplication = System.Windows.Application;

namespace BichonDesktopPet.App;

public partial class App : WpfApplication
{
    private TrayIconService? _trayIconService;
    private HttpClient? _aiHttpClient;

    public IAiChatProvider? AiChatProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            AppLogger.Log("An unhandled UI dispatcher exception occurred.", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            AppLogger.Log(
                $"An unhandled application-domain exception occurred. Process terminating: {args.IsTerminating}.",
                args.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLogger.Log("An unobserved task exception occurred.", args.Exception);
            args.SetObserved();
        };

        try
        {
            var userDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BichonDesktopPet");
            var qwenOptions = new QwenOptions();

            try
            {
                qwenOptions = QwenOptionsLoader.Load(userDataDirectory);
            }
            catch (Exception exception)
            {
                AppLogger.Log("Qwen local configuration could not be read. AI remains disabled.", exception);
            }

            _aiHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(45)
            };
            AiChatProvider = new QwenChatCompletionProvider(_aiHttpClient, qwenOptions);
            AppLogger.Log(qwenOptions.IsConfigured
                ? $"Qwen provider configured with model '{qwenOptions.Model}'."
                : "Qwen API key is not configured. AI features remain disabled; local pet features are unaffected.");

            var petWindow = new MainWindow();
            MainWindow = petWindow;
            _trayIconService = new TrayIconService(petWindow);
            petWindow.Show();
        }
        catch (Exception exception)
        {
            AppLogger.Log("Application startup failed.", exception);
            System.Windows.MessageBox.Show(
                "比熊桌宠启动失败。诊断信息已写入本地日志目录。",
                "比熊桌宠",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _aiHttpClient?.Dispose();
        base.OnExit(e);
    }
}
