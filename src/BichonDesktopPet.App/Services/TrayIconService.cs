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

        var showItem = new Forms.ToolStripMenuItem("显示桌宠");
        showItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray));

        var hideItem = new Forms.ToolStripMenuItem("隐藏桌宠");
        hideItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.HideToTray));

        var settingsItem = new Forms.ToolStripMenuItem("设置...");
        settingsItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.OpenSettingsWindow));

        var avatarsItem = new Forms.ToolStripMenuItem("角色管理...");
        avatarsItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.OpenAvatarManagerWindow));

        var chatItem = new Forms.ToolStripMenuItem("和伙伴聊天...");
        chatItem.Click += (_, _) => window.Dispatcher.BeginInvoke(new Action(window.OpenChatWindow));

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
            hideItem,
            new Forms.ToolStripSeparator(),
            settingsItem,
            avatarsItem,
            chatItem,
            new Forms.ToolStripSeparator(),
            snowItem,
            lightsItem,
            musicItem,
            new Forms.ToolStripSeparator(),
            exitItem
        ]);

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "桌面伙伴",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) =>
            window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray));
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message.Length > 240 ? message[..240] : message;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
