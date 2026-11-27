using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.Win32;

namespace BichonDesktopPet.App.Views;

public partial class AvatarManagerWindow : Window
{
    private readonly AvatarCatalogService _catalog;
    private readonly Func<string, (bool Success, bool Saved)> _setActiveAvatar;
    private readonly Func<string, (bool Success, bool Saved)> _deleteAvatar;
    private readonly Func<string> _getActiveAvatarId;

    public AvatarManagerWindow(
        AvatarCatalogService catalog,
        Func<string, (bool Success, bool Saved)> setActiveAvatar,
        Func<string, (bool Success, bool Saved)> deleteAvatar,
        Func<string> getActiveAvatarId)
    {
        InitializeComponent();

        _catalog = catalog;
        _setActiveAvatar = setActiveAvatar;
        _deleteAvatar = deleteAvatar;
        _getActiveAvatarId = getActiveAvatarId;

        RefreshAvatars();
    }

    private void RefreshAvatars(string? preferredId = null)
    {
        var avatars = _catalog.GetAvatars();
        AvatarListBox.ItemsSource = avatars;

        var activeId = _getActiveAvatarId();
        AvatarListBox.SelectedItem =
            avatars.FirstOrDefault(avatar => avatar.Id == preferredId) ??
            avatars.FirstOrDefault(avatar => avatar.Id == activeId) ??
            avatars.FirstOrDefault();

        var activeAvatar = avatars.FirstOrDefault(avatar => avatar.Id == activeId);
        CurrentAvatarText.Text = activeAvatar is null
            ? "当前角色：未选择"
            : $"当前角色：{activeAvatar.Name}";

        UpdateButtons();
    }

    private void AvatarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var selected = AvatarListBox.SelectedItem as AvatarProfile;
        SetActiveButton.IsEnabled = selected is not null;
        DeleteButton.IsEnabled = selected is { IsBuiltIn: false };
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要导入的角色图片",
            Filter = "支持的图片 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var name = AvatarNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }

        var species = (SpeciesComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "custom";

        try
        {
            var imported = _catalog.ImportImage(dialog.FileName, name, species);
            AvatarNameTextBox.Text = imported.Name;
            RefreshAvatars(imported.Id);
            StatusText.Text = "图片已复制到本地角色库。选择「设为当前角色」即可切换。";
        }
        catch (Exception exception)
        {
            AppLogger.Log("Unable to import a local avatar image.", exception);
            StatusText.Text = exception.Message;
        }
    }

    private void SetActive_Click(object sender, RoutedEventArgs e)
    {
        if (AvatarListBox.SelectedItem is not AvatarProfile selected)
        {
            return;
        }

        ApplyAvatar(selected.Id);
    }

    private void RestoreDefault_Click(object sender, RoutedEventArgs e)
    {
        ApplyAvatar(AvatarCatalogService.DefaultAvatarId);
    }

    private void ApplyAvatar(string avatarId)
    {
        var result = _setActiveAvatar(avatarId);
        if (!result.Success)
        {
            StatusText.Text = "角色图片不可用，无法切换。";
            return;
        }

        StatusText.Text = result.Saved
            ? "角色已切换，选择会在下次启动时保留。"
            : "角色已切换，但保存失败；请检查本地日志。";

        RefreshAvatars(avatarId);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (AvatarListBox.SelectedItem is not AvatarProfile selected || selected.IsBuiltIn)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"确定删除本地角色「{selected.Name}」吗？此操作会删除该角色复制到本机的图片文件。",
            "删除角色",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        var result = _deleteAvatar(selected.Id);
        if (!result.Success)
        {
            StatusText.Text = "删除失败，角色清单未发生变化。";
            return;
        }

        StatusText.Text = result.Saved
            ? "角色已删除。"
            : "角色已删除，但当前角色设置保存失败；下次启动将自动恢复比熊。";

        RefreshAvatars();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
