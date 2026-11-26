using System.Drawing;
using Forms = System.Windows.Forms;
using BichonDesktopPet.App.Views;

namespace BichonDesktopPet.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconService(MainWindow window)
    {
        var menu = new Forms.ContextMenuStrip();

        var showItem = new Forms.ToolStripMenuItem("显示比熊");
        showItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray));

        var snowItem = new Forms.ToolStripMenuItem("切换雪花");
        snowItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.ToggleSnow));

        var lightsItem = new Forms.ToolStripMenuItem("切换装饰灯");
        lightsItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.ToggleLights));

        var musicItem = new Forms.ToolStripMenuItem("背景音乐");
        musicItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.ToggleMusic));

        var exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.ExitApplication));

        menu.Items.AddRange([
            showItem,
            new Forms.ToolStripSeparator(),
            snowItem,
            lightsItem,
            musicItem,
            new Forms.ToolStripSeparator(),
            exitItem
        ]);

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "比熊桌宠",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) =>
            window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray));
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
