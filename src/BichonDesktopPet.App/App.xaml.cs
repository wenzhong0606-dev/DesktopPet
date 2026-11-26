using System.Windows;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.App.Views;
using WpfApplication = System.Windows.Application;

namespace BichonDesktopPet.App;

public partial class App : WpfApplication
{
    private TrayIconService? _trayIconService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var petWindow = new MainWindow();
        MainWindow = petWindow;
        _trayIconService = new TrayIconService(petWindow);
        petWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        base.OnExit(e);
    }
}
