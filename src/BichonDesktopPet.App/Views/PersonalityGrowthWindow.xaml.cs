using System;
using System.Windows;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.App.Views;

public partial class PersonalityGrowthWindow : Window
{
    private readonly PersonalityGrowthStore _store;
    private readonly string _companionId;
    private bool _refreshing = true;

    public PersonalityGrowthWindow(PersonalityGrowthStore store, string companionId)
    {
        _store = store;
        _companionId = companionId;
        InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        _refreshing = true;
        try
        {
            var profile = _store.Read(_companionId);
            EnabledCheckBox.IsChecked = profile.Enabled;
            SummaryText.Text = $"{(profile.Enabled ? "成长已开启" : "成长已暂停或未开启")} · 回复详略 {profile.Length:+0;-0;0} · 直接表达 {profile.Directness} · 自然表达 {profile.Naturalness} · 表情克制 {profile.EmojiRestraint}";
            var changes = _store.RecentChanges(_companionId);
            ChangesList.ItemsSource = changes;
            RollbackButton.IsEnabled = changes.Count > 0;
        }
        finally { _refreshing = false; }
    }

    private void EnabledChanged(object sender, RoutedEventArgs e)
    {
        if (_refreshing) return;
        try
        {
            _store.SetEnabled(_companionId, EnabledCheckBox.IsChecked == true);
            Refresh();
            StatusText.Text = EnabledCheckBox.IsChecked == true ? "成长已开启。" : "成长已暂停；保留已有状态，不再应用。";
        }
        catch (Exception)
        {
            _refreshing = true;
            EnabledCheckBox.IsChecked = EnabledCheckBox.IsChecked != true;
            _refreshing = false;
            StatusText.Text = "保存成长设置失败。";
        }
    }

    private void Rollback_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this, "撤销当前伙伴最近一次成长调整？未完成的成长证据也会清除。", "撤销成长",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            var changed = _store.RollbackLast(_companionId);
            Refresh();
            StatusText.Text = changed ? "已撤销最近一次调整。" : "没有可撤销的调整。";
        }
        catch (Exception) { StatusText.Text = "撤销失败。"; }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this, "清除当前伙伴的所有成长倾向和成长记录？基础人格、长期记忆和聊天记录不受影响；已发送内容无法撤回。", "重置成长",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            _store.Reset(_companionId);
            Refresh();
            StatusText.Text = "成长状态及成长记录已重置。";
        }
        catch (Exception) { StatusText.Text = "重置失败。"; }
    }
}
