using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.App.Views;

public partial class ChatWindow : Window
{
    private readonly IAiChatProvider _provider;
    private readonly CompanionIdentity _companion;
    private readonly AvatarProfile _avatar;
    private readonly Func<PetBehaviorState> _getCurrentState;
    private readonly List<AiChatMessage> _conversation = [];
    private CancellationTokenSource? _requestCancellation;
    private bool _isSending;

    public ChatWindow(
        IAiChatProvider provider,
        CompanionIdentity companion,
        AvatarProfile avatar,
        Func<PetBehaviorState> getCurrentState)
    {
        InitializeComponent();
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _companion = companion ?? throw new ArgumentNullException(nameof(companion));
        _avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));
        _getCurrentState = getCurrentState ?? throw new ArgumentNullException(nameof(getCurrentState));

        CompanionNameText.Text = string.IsNullOrWhiteSpace(avatar.Name)
            ? companion.DisplayName
            : avatar.Name;
        ConnectionStatusText.Text = provider.IsConfigured
            ? $"已连接 {provider.ProviderName} · {provider.Model}。本次对话仅保存在当前窗口内。"
            : "尚未配置 Qwen API Key。请先按 docs/QWEN_CONFIGURATION.md 配置；桌宠其他功能仍可离线使用。";
        MessageInput.IsEnabled = provider.IsConfigured;
        SendButton.IsEnabled = provider.IsConfigured;
        AddMessage("assistant", provider.IsConfigured
            ? $"你好，我是{CompanionNameText.Text}。我们可以开始聊天啦。"
            : "我现在还不能连接 AI 服务。配置本地 API Key 后，就可以开始聊天了。");
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e) =>
        await SendCurrentMessageAsync();

    private async void MessageInput_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            await SendCurrentMessageAsync();
        }
    }

    private async Task SendCurrentMessageAsync()
    {
        if (_isSending || !_provider.IsConfigured)
        {
            return;
        }

        var content = MessageInput.Text.Trim();
        if (content.Length == 0)
        {
            return;
        }

        if (content.Length > 2000)
        {
            ConnectionStatusText.Text = "单条消息最多输入 2000 个字符。";
            return;
        }

        MessageInput.Clear();
        _conversation.Add(new AiChatMessage("user", content));
        AddMessage("user", content);
        SetSending(true);

        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        _requestCancellation = cancellation;
        try
        {
            var messages = ChatContextBuilder.BuildMessages(
                _companion,
                _avatar,
                _getCurrentState(),
                _conversation);

            var result = await _provider.CompleteAsync(
                new AiChatRequest
                {
                    Messages = messages,
                    MaxTokens = 512,
                    Temperature = 0.7
                },
                cancellation.Token);

            _conversation.Add(new AiChatMessage("assistant", result.Content));
            AddMessage("assistant", result.Content);
            ConnectionStatusText.Text = $"{_provider.ProviderName} · {result.Model} · 本次对话仅保存在当前窗口内。";
        }
        catch (OperationCanceledException)
        {
            ConnectionStatusText.Text = "请求已取消或超时，请检查网络后重试。";
        }
        catch (AiProviderNotConfiguredException)
        {
            ConnectionStatusText.Text = "尚未配置 Qwen API Key。请检查本地配置文件。";
        }
        catch (AiProviderException exception)
        {
            ConnectionStatusText.Text = $"AI 服务暂时不可用：{exception.Message}";
        }
        catch (HttpRequestException)
        {
            ConnectionStatusText.Text = "网络连接失败，请检查网络后重试。";
        }
        catch (Exception)
        {
            ConnectionStatusText.Text = "对话遇到意外错误。详细信息未记录用户消息，请稍后重试。";
        }
        finally
        {
            cancellation.Dispose();
            if (ReferenceEquals(_requestCancellation, cancellation))
            {
                _requestCancellation = null;
            }
            if (IsLoaded)
            {
                SetSending(false);
            }
        }
    }

    private void SetSending(bool sending)
    {
        _isSending = sending;
        SendButton.IsEnabled = !sending && _provider.IsConfigured;
        MessageInput.IsEnabled = !sending && _provider.IsConfigured;
        SendButton.Content = sending ? "发送中…" : "发送";
        if (sending)
        {
            ConnectionStatusText.Text = "正在联系 AI，请稍候…";
        }
        if (!sending)
        {
            MessageInput.Focus();
        }
    }

    private void AddMessage(string role, string content)
    {
        var isUser = role == "user";
        var bubble = new Border
        {
            MaxWidth = 350,
            HorizontalAlignment = isUser ? WpfHorizontalAlignment.Right : WpfHorizontalAlignment.Left,
            Background = isUser ? new SolidColorBrush(WpfColor.FromRgb(226, 239, 255)) : new SolidColorBrush(Color.FromRgb(247, 242, 237)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 10)
        };
        bubble.Child = new TextBlock
        {
            Text = content,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(65, 54, 49)),
            FontSize = 14
        };
        MessagePanel.Children.Add(bubble);
        ConversationScroll.ScrollToEnd();
    }

    protected override void OnClosed(EventArgs e)
    {
        _requestCancellation?.Cancel();
        base.OnClosed(e);
    }
}
