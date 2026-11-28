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
    private bool _refreshing = true;

    public MemoryWindow(CompanionMemoryStore store, string companionId)
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
            var memories = _store.GetAll(_companionId);
            MemoryList.ItemsSource = memories;
            RetrievalCheckBox.IsChecked = _store.IsRetrievalEnabled(_companionId);
            TopicInput.Clear();
            ContentInput.Clear();
            StatusText.Text = $"已保存 {memories.Count} / {CompanionMemoryStore.MaximumMemories} 条记忆。";
        }
        finally { _refreshing = false; }
    }

    private void RetrievalChanged(object sender, RoutedEventArgs e)
    {
        if (_refreshing) return;
        try
        {
            _store.SetRetrievalEnabled(_companionId, RetrievalCheckBox.IsChecked == true);
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
            Refresh();
            StatusText.Text = "记忆已保存在本机。";
        }
        catch (ArgumentException exception) { StatusText.Text = exception.Message; }
        catch (Exception) { StatusText.Text = "保存失败，请检查记忆数量上限或数据库状态。"; }
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
        if (System.Windows.MessageBox.Show(this, "清空当前伙伴的全部长期记忆？聊天历史不受影响。",
            "清空记忆", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _store.Clear(_companionId); Refresh(); }
        catch (Exception) { StatusText.Text = "清空失败，请重试。"; }
    }
}
