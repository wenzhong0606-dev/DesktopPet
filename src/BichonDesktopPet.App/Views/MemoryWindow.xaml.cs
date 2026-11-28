using System;
using System.Windows;
using System.Windows.Controls;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.App.Views;

public partial class MemoryWindow : Window
{
    private readonly CompanionMemoryStore _store;
    private readonly string _companionId;
    private readonly PersonalityGrowthStore? _growthStore;
    private bool _refreshing = true;
    private int _offset;

    public MemoryWindow(CompanionMemoryStore store, string companionId, PersonalityGrowthStore? growthStore = null)
    {
        _store = store;
        _companionId = companionId;
        _growthStore = growthStore;
        InitializeComponent();
        GrowthButton.IsEnabled = _growthStore is not null;
        Refresh();
    }

    private void Growth_Click(object sender, RoutedEventArgs e)
    {
        if (_growthStore is null) return;
        try { new PersonalityGrowthWindow(_growthStore, _companionId) { Owner = this }.ShowDialog(); }
        catch (Exception) { StatusText.Text = "成长人格暂不可用，请检查本机数据库。"; }
    }

    private void Refresh()
    {
        _refreshing = true;
        try
        {
            var count = _store.Count(_companionId);
            if (_offset >= count)
                _offset = count == 0 ? 0 : (int)((count - 1) / CompanionMemoryStore.DefaultPageSize * CompanionMemoryStore.DefaultPageSize);
            var memories = _store.GetPage(_companionId, _offset);
            PreviousPageButton.IsEnabled = _offset > 0;
            NextPageButton.IsEnabled = (long)_offset + memories.Count < count;
            PageText.Text = count == 0 ? "暂无记忆" : $"{_offset + 1}–{_offset + memories.Count} / {count}";
            MemoryList.ItemsSource = memories;
            RetrievalCheckBox.IsChecked = _store.IsRetrievalEnabled(_companionId);
            AutomaticCheckBox.IsChecked = _store.IsAutomaticEnabled(_companionId);
            RefreshAdvanced();
            TopicInput.Clear();
            ContentInput.Clear();
            StatusText.Text = $"已保存 {count} 条记忆。";
        }
        finally { _refreshing = false; }
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        _offset = Math.Max(0, _offset - CompanionMemoryStore.DefaultPageSize);
        TryRefreshPage();
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        _offset += CompanionMemoryStore.DefaultPageSize;
        TryRefreshPage();
    }

    private void TryRefreshPage()
    {
        try { Refresh(); }
        catch (Exception) { StatusText.Text = "加载记忆失败，请重试。"; }
    }

    private void AutomaticChanged(object sender, RoutedEventArgs e)
    {
        if (_refreshing) return;
        try
        {
            _store.SetAutomaticEnabled(_companionId, AutomaticCheckBox.IsChecked == true);
            _refreshing = true;
            RefreshAdvanced();
            _refreshing = false;
            StatusText.Text = AutomaticCheckBox.IsChecked == true ? "已开启本地自动记忆。" : "已停止自动形成新记忆。";
        }
        catch (Exception)
        {
            _refreshing = true;
            AutomaticCheckBox.IsChecked = AutomaticCheckBox.IsChecked != true;
            _refreshing = false;
            StatusText.Text = "保存自动记忆设置失败。";
        }
    }

    private void RefreshAdvanced()
    {
        CloudCheckBox.IsEnabled=OpenCheckBox.IsEnabled=AutomaticCheckBox.IsChecked==true;
        CloudCheckBox.IsChecked=_store.IsCloudEnhancedEnabled(_companionId);
        OpenCheckBox.IsChecked=_store.IsOpenPreferenceEnabled(_companionId);
        SemanticCheckBox.IsEnabled=RetrievalCheckBox.IsChecked==true;
        SemanticCheckBox.IsChecked=_store.IsSemanticEnabled(_companionId);
    }
    private void OpenChanged(object sender,RoutedEventArgs e)=>ChangeAdvanced(true);
    private void SemanticChanged(object sender,RoutedEventArgs e)=>ChangeAdvanced(false);
    private void ChangeAdvanced(bool open)
    {
        if(_refreshing)return;
        try
        {
            if(open)_store.SetOpenPreferenceEnabled(_companionId,OpenCheckBox.IsChecked==true);
            else _store.SetSemanticEnabled(_companionId,SemanticCheckBox.IsChecked==true);
            StatusText.Text="已更新可选记忆增强设置。";
        }
        catch(Exception) { StatusText.Text="保存增强设置失败。"; }
        finally
        {
            _refreshing=true;
            try { RefreshAdvanced(); } catch(Exception) { }
            _refreshing=false;
        }
    }

    private void CloudChanged(object sender, RoutedEventArgs e)
    {
        if (_refreshing) return;
        try
        {
            _store.SetCloudEnhancedEnabled(_companionId, CloudCheckBox.IsChecked == true && AutomaticCheckBox.IsChecked == true);
            StatusText.Text = CloudCheckBox.IsChecked == true ? "已开启可选 Qwen 辅助提取，可能产生额外费用。" : "已关闭 Qwen 辅助提取。";
        }
        catch (Exception)
        {
            _refreshing = true;
            CloudCheckBox.IsChecked = CloudCheckBox.IsChecked != true;
            _refreshing = false;
            StatusText.Text = "保存辅助提取设置失败。";
        }
    }

    private void RetrievalChanged(object sender, RoutedEventArgs e)
    {
        if (_refreshing) return;
        try
        {
            _store.SetRetrievalEnabled(_companionId, RetrievalCheckBox.IsChecked == true);
            _refreshing = true;
            RefreshAdvanced();
            _refreshing = false;
            StatusText.Text = RetrievalCheckBox.IsChecked == true ? "已启用相关记忆检索。" : "已暂停相关记忆检索。";
        }
        catch (Exception)
        {
            _refreshing = true;
            RetrievalCheckBox.IsChecked = RetrievalCheckBox.IsChecked != true;
            _refreshing = false;
            StatusText.Text = "保存记忆设置失败，请检查本机数据库。";
        }
    }

    private void MemoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || MemoryList.SelectedItem is not CompanionMemory memory) return;
        TopicInput.Text = memory.Topic;
        ContentInput.Text = memory.Content;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        MemoryList.SelectedItem = null;
        TopicInput.Clear();
        ContentInput.Clear();
        TopicInput.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _store.Save(_companionId, TopicInput.Text, ContentInput.Text);
            _offset = 0;
            Refresh();
            StatusText.Text = "记忆已保存在本机。";
        }
        catch (ArgumentException exception) { StatusText.Text = exception.Message; }
        catch (Exception) { StatusText.Text = "保存失败，请检查本地数据库状态。"; }
    }

    private void Important_Click(object sender, RoutedEventArgs e) =>
        ChangeMetadata(memory => _store.SetImportant(_companionId, memory.Topic, !memory.Important));

    private void Archive_Click(object sender, RoutedEventArgs e) =>
        ChangeMetadata(memory => _store.SetArchived(_companionId, memory.Topic,
            memory.IsAvailableAt(DateTimeOffset.UtcNow)));

    private void Expiry_Click(object sender, RoutedEventArgs e) =>
        ChangeMetadata(memory => _store.SetExpiry(_companionId, memory.Topic, DateTimeOffset.UtcNow.AddDays(30)));

    private void Permanent_Click(object sender, RoutedEventArgs e) =>
        ChangeMetadata(memory => _store.SetExpiry(_companionId, memory.Topic, null));

    private void ChangeMetadata(Func<CompanionMemory, bool> update)
    {
        if (MemoryList.SelectedItem is not CompanionMemory memory)
        {
            StatusText.Text = "请先选择一条记忆。";
            return;
        }
        try
        {
            var changed = update(memory);
            Refresh();
            StatusText.Text = changed ? "记忆设置已更新。" : "未找到该记忆。";
        }
        catch (Exception) { StatusText.Text = "更新记忆设置失败。"; }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (MemoryList.SelectedItem is not CompanionMemory memory) return;
        if (System.Windows.MessageBox.Show(this, "删除所选记忆？", "删除记忆",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _store.Delete(_companionId, memory.Topic); Refresh(); }
        catch (Exception) { StatusText.Text = "删除失败，请重试。"; }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this, "清空当前伙伴的全部长期记忆？聊天历史不受影响；已发送到 Qwen 的内容无法撤回。",
            "清空记忆", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _store.Clear(_companionId); Refresh(); }
        catch (Exception) { StatusText.Text = "清空失败，请重试。"; }
    }
}
