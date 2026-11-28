using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace BichonDesktopPet.App.Views;

public partial class ChatWindow : Window
{
    private const int ContextHistoryLimit = 12;

    private readonly IAiChatProvider _provider;
    private readonly CompanionIdentity _companion;
    private readonly AvatarProfile _avatar;
    private readonly Func<PetBehaviorState> _getCurrentState;
    private readonly ChatHistoryStore _historyStore;
    private CompanionMemoryStore? _memoryStore;
    private PersonalityGrowthStore? _growthStore;
    private readonly ObservableCollection<ChatHistoryMessage> _messages = [];
    private readonly List<AiChatMessage> _conversation = [];
    private CancellationTokenSource? _requestCancellation;
    private string? _activeSessionId;
    private long? _olderMessageCursor;
    private bool _hasOlderMessages;
    private bool _isRefreshingSessions;
    private bool _isSending;

    public ChatWindow(
        IAiChatProvider provider,
        CompanionIdentity companion,
        AvatarProfile avatar,
        Func<PetBehaviorState> getCurrentState,
        ChatHistoryStore historyStore)
    {
        InitializeComponent();
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _companion = companion ?? throw new ArgumentNullException(nameof(companion));
        _avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));
        _getCurrentState = getCurrentState ?? throw new ArgumentNullException(nameof(getCurrentState));
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));

        try
        {
            var memoryStore = new CompanionMemoryStore(_historyStore.DatabasePath);
            memoryStore.Initialize();
            _memoryStore = memoryStore;
        }
        catch (Exception)
        {
            MemoryButton.IsEnabled = false;
            MemoryButton.ToolTip = "本机记忆数据库暂不可用";
        }

        try
        {
            var growthStore = new PersonalityGrowthStore(_historyStore.DatabasePath);
            growthStore.Initialize();
            _growthStore = growthStore;
        }
        catch (Exception) { /* Optional growth does not block chat. */ }

        MessageList.ItemsSource = _messages;
        var petNickname = ChatWelcomeMessageBuilder.GetPrimaryNickname(avatar);
        CompanionNameText.Text = string.IsNullOrWhiteSpace(petNickname) ? "伙伴对话" : petNickname;
        Title = string.IsNullOrWhiteSpace(petNickname) ? "和伙伴聊天" : $"与{petNickname}聊天";

        ConnectionStatusText.Text = provider.IsConfigured
            ? "聊天记录保存在本机；发送消息时，消息、角色人格、近期上下文、已授权的相关记忆及成长表达倾向会发送至 Qwen 云端处理。请勿输入敏感信息。"
            : "当前未配置 Qwen API Key。可离线查询本机时间、管理聊天记录和记忆；其他对话需配置密钥。";

        MessageInput.IsEnabled = true;
        SendButton.IsEnabled = true;

        var sessions = RefreshSessionSelector(null, selectLatestWhenMissing: true);
        if (sessions is not null)
        {
            LoadSession(sessions.Id);
        }
        else
        {
            StartNewConversation();
        }
    }

    private void MemoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSending || _memoryStore is null) return;
        try { new MemoryWindow(_memoryStore, _companion.Id, _growthStore) { Owner = this }.ShowDialog(); }
        catch (Exception) { ConnectionStatusText.Text = "本机记忆暂不可用，请检查数据库后重试。"; }
    }

    private async Task<IReadOnlyList<CompanionMemory>> RetrieveMemoriesAsync(string query, bool extraUsed, CancellationToken token)
    {
        if(_memoryStore is null) return [];
        try
        {
            var previous=_conversation.Where(message=>message.Role=="user").SkipLast(1).LastOrDefault()?.Content;
            var local=_memoryStore.Retrieve(_companion.Id,query,previous);
            // Only try expansion when local search misses; share the one-extra-request budget.
            if(local.Count>0 || extraUsed || !SemanticMemoryQuery.CanRequest(query) ||
                !_memoryStore.IsSemanticEnabled(_companion.Id) ||
                !_memoryStore.TryReserveExtraRequest(_companion.Id,semantic:true)) return local;
            var expansion=await SemanticMemoryQuery.ExpandAsync(_provider,query,token);
            token.ThrowIfCancellationRequested();
            return string.IsNullOrEmpty(expansion)?local:_memoryStore.Retrieve(_companion.Id,query,previous,expansion);
        }
        catch(Exception) when(!token.IsCancellationRequested) { return []; }
    }

    private async Task<bool> ObserveMemoryAsync(ChatHistoryMessage userMessage, CancellationToken token)
    {
        if (_memoryStore is null) return false;
        var extraUsed=false;
        try
        {
            string? evidenceJson = null;
            if (_memoryStore.IsCloudEnhancedEnabled(_companion.Id) &&
                LocalMemoryExtractor.Extract(userMessage.Content).Count == 0 &&
                ExperienceMemoryExtractor.ExtractUserStatement(userMessage.Content, userMessage.CreatedAtUtc).Count == 0 &&
                (!_memoryStore.IsOpenPreferenceEnabled(_companion.Id) || OpenPreferenceExtractor.Extract(userMessage.Content).Count==0) &&
                CloudMemoryEvidence.CanRequest(userMessage.Content) &&
                _memoryStore.TryReserveExtraRequest(_companion.Id,semantic:false))
            {
                extraUsed=true;
                var evidence = await CloudMemoryEvidence.SelectAsync(_provider, userMessage.Content, token);
                evidenceJson = System.Text.Json.JsonSerializer.Serialize(new { evidence });
            }
            token.ThrowIfCancellationRequested();
            _memoryStore.ObserveUserMessage(_companion.Id, userMessage.Id, evidenceJson);
        }
        catch (Exception) when (!token.IsCancellationRequested) { /* Optional memory must never block chat. */ }
        return extraUsed;
    }

    private PersonalityGrowthProfile? ReadGrowth()
    {
        try { return _growthStore?.Read(_companion.Id); }
        catch (Exception) { return null; }
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

    private void SessionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshingSessions || _isSending || SessionSelector.SelectedItem is not ChatSession session)
        {
            return;
        }

        if (!string.Equals(session.Id, _activeSessionId, StringComparison.Ordinal))
        {
            LoadSession(session.Id);
        }
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isSending)
        {
            StartNewConversation();
        }
    }

    private void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSending || string.IsNullOrWhiteSpace(_activeSessionId))
        {
            return;
        }

        var confirmation = System.Windows.MessageBox.Show(
            this,
            "确定删除当前会话及其中的全部消息吗？独立保存的长期记忆不受影响。此操作不可撤销。",
            "删除聊天会话",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _historyStore.DeleteSession(_companion.Id, _activeSessionId);
            var next = RefreshSessionSelector(null, selectLatestWhenMissing: true);
            if (next is null)
            {
                StartNewConversation();
            }
            else
            {
                LoadSession(next.Id);
            }
        }
        catch (Exception)
        {
            ConnectionStatusText.Text = "删除会话失败，请检查本地数据库状态后重试。";
        }
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSending)
        {
            return;
        }

        var sessions = _historyStore.GetSessions(_companion.Id);
        if (sessions.Count == 0)
        {
            return;
        }

        var confirmation = System.Windows.MessageBox.Show(
            this,
            "确定清空当前角色的全部聊天会话和消息吗？长期记忆需在记忆管理中单独删除，其他角色不受影响。此操作不可撤销。",
            "清空聊天记录",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _historyStore.DeleteAllSessions(_companion.Id);
            RefreshSessionSelector(null, selectLatestWhenMissing: false);
            StartNewConversation();
        }
        catch (Exception)
        {
            ConnectionStatusText.Text = "清空聊天记录失败，请检查本地数据库状态后重试。";
        }
    }

    private void LoadOlderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSending || !_hasOlderMessages || _olderMessageCursor is null || _activeSessionId is null)
        {
            return;
        }

        try
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(MessageList);
            var previousOffset = scrollViewer?.VerticalOffset ?? 0;
            var previousExtent = scrollViewer?.ExtentHeight ?? 0;
            var page = _historyStore.GetMessagePage(
                _companion.Id,
                _activeSessionId,
                ChatHistoryStore.DefaultPageSize,
                _olderMessageCursor);

            for (var index = 0; index < page.Messages.Count; index++)
            {
                _messages.Insert(index, page.Messages[index]);
            }

            _olderMessageCursor = page.NextBeforeMessageId;
            _hasOlderMessages = page.HasMore;
            UpdateHistoryButtons();

            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    MessageList.UpdateLayout();
                    if (scrollViewer is not null)
                    {
                        var addedHeight = Math.Max(0, scrollViewer.ExtentHeight - previousExtent);
                        scrollViewer.ScrollToVerticalOffset(previousOffset + addedHeight);
                    }
                }));
        }
        catch (Exception)
        {
            ConnectionStatusText.Text = "加载更早消息失败，请稍后重试。";
        }
    }

    private async Task SendCurrentMessageAsync()
    {
        if (_isSending)
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

        var isLocalTimeQuery = LocalTimeQueryHandler.TryHandle(content, out var localTimeAnswer);
        if (!_provider.IsConfigured && !isLocalTimeQuery)
        {
            ConnectionStatusText.Text = "尚未配置 Qwen API Key；本机时间问题可离线回答，其他 AI 对话需要先配置密钥。";
            return;
        }

        var createdSession = false;
        string sessionId;
        ChatHistoryMessage userMessage;
        try
        {
            if (_activeSessionId is null)
            {
                var session = _historyStore.CreateSession(_companion.Id);
                _activeSessionId = session.Id;
                createdSession = true;
            }

            sessionId = _activeSessionId;
            userMessage = _historyStore.SaveMessage(_companion.Id, sessionId, "user", content);
        }
        catch (Exception)
        {
            if (createdSession && _activeSessionId is not null)
            {
                try
                {
                    _historyStore.DeleteSession(_companion.Id, _activeSessionId);
                }
                catch (Exception)
                {
                    // Preserve the original save failure for the user-facing message.
                }

                _activeSessionId = null;
            }

            ConnectionStatusText.Text = "本地聊天记录保存失败，消息尚未发送。请检查磁盘空间或数据库权限。";
            return;
        }

        if (_messages.Count == 1 && _messages[0].Id == 0)
        {
            _messages.Clear();
        }

        MessageInput.Clear();
        _messages.Add(userMessage);
        AppendContextMessage(new AiChatMessage("user", content));
        // Only newly persisted user messages are eligible; no history backfill.
        if (!isLocalTimeQuery)
        {
            try { _growthStore?.Observe(_companion.Id, userMessage.Id); }
            catch (Exception) { /* Optional growth must never block chat. */ }
        }
        RefreshSessionSelector(_activeSessionId, selectLatestWhenMissing: false);
        ScrollToLatest();
        SetSending(true);

        if (isLocalTimeQuery)
        {
            try
            {
                var assistantMessage = _historyStore.SaveMessage(
                    _companion.Id,
                    sessionId,
                    "assistant",
                    localTimeAnswer);
                _messages.Add(assistantMessage);
                AppendContextMessage(new AiChatMessage("assistant", localTimeAnswer));
                ConnectionStatusText.Text = "本机系统时间 · 本地回答；本次消息未发送到云端。";
            }
            catch (Exception)
            {
                _messages.Add(new ChatHistoryMessage
                {
                    Id = -1,
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = localTimeAnswer,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                AppendContextMessage(new AiChatMessage("assistant", localTimeAnswer));
                ConnectionStatusText.Text = "已读取本机时间，但回复写入聊天记录失败。";
            }

            RefreshSessionSelector(sessionId, selectLatestWhenMissing: false);
            ScrollToLatest();
            SetSending(false);
            return;
        }

        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        _requestCancellation = cancellation;
        try
        {
            var extraUsed = await ObserveMemoryAsync(userMessage, cancellation.Token);
            var memories = await RetrieveMemoriesAsync(content, extraUsed, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            var messages = ChatContextBuilder.BuildMessages(
                _companion,
                _avatar,
                _getCurrentState(),
                _conversation, relevantMemories: memories, growth: ReadGrowth());

            var result = await _provider.CompleteAsync(
                new AiChatRequest
                {
                    Messages = messages,
                    MaxTokens = 512,
                    Temperature = 0.4
                },
                cancellation.Token);

            var assistantContent = ChatReplySanitizer.Sanitize(result.Content);
            var hasValidReply = !string.IsNullOrWhiteSpace(assistantContent);
            if (!hasValidReply)
            {
                assistantContent = "我刚才没有给出有效的文字回答，可以换个方式问我吗？";
            }

            try
            {
                var assistantMessage = _historyStore.SaveMessage(
                    _companion.Id,
                    sessionId,
                    "assistant",
                    assistantContent);
                _messages.Add(assistantMessage);
                AppendContextMessage(new AiChatMessage("assistant", assistantContent));
                // Only successfully persisted, non-fallback replies establish an exchange.
                if (hasValidReply)
                {
                    try { _memoryStore?.ObserveCompletedExchange(_companion.Id, userMessage.Id, assistantMessage.Id); }
                    catch (Exception) { /* Optional memory must never block chat. */ }
                }
                RefreshSessionSelector(sessionId, selectLatestWhenMissing: false);
                ConnectionStatusText.Text = $"{_provider.ProviderName} · {result.Model} · 回复已保存到本机；请求内容由 Qwen 云端处理。";
            }
            catch (Exception)
            {
                _messages.Add(new ChatHistoryMessage
                {
                    Id = -1,
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = assistantContent,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                AppendContextMessage(new AiChatMessage("assistant", assistantContent));
                ConnectionStatusText.Text = "已收到 AI 回复，但保存到本机失败；请检查磁盘空间或数据库权限。";
            }

            ScrollToLatest();
        }
        catch (OperationCanceledException)
        {
            ConnectionStatusText.Text = "请求已取消或超时；已发送的消息仍保存在本机。";
        }
        catch (AiProviderNotConfiguredException)
        {
            ConnectionStatusText.Text = "尚未配置 Qwen API Key；已发送的消息仍保存在本机。";
        }
        catch (AiProviderException exception)
        {
            ConnectionStatusText.Text = $"AI 服务暂时不可用：{exception.Message}；已发送的消息仍保存在本机。";
        }
        catch (HttpRequestException)
        {
            ConnectionStatusText.Text = "网络连接失败；已发送的消息仍保存在本机，请检查网络后重试。";
        }
        catch (Exception)
        {
            ConnectionStatusText.Text = "对话遇到意外错误；已发送的消息仍保存在本机。";
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

    private ChatSession? RefreshSessionSelector(
        string? preferredSessionId,
        bool selectLatestWhenMissing = false)
    {
        var sessions = _historyStore.GetSessions(_companion.Id);
        var selected = sessions.FirstOrDefault(session =>
            string.Equals(session.Id, preferredSessionId, StringComparison.Ordinal));

        if (selected is null && selectLatestWhenMissing)
        {
            selected = sessions.FirstOrDefault();
        }

        _isRefreshingSessions = true;
        SessionSelector.ItemsSource = sessions;
        SessionSelector.SelectedItem = selected;
        _isRefreshingSessions = false;
        UpdateHistoryButtons();
        return selected;
    }

    private void LoadSession(string sessionId)
    {
        var page = _historyStore.GetMessagePage(
            _companion.Id,
            sessionId,
            ChatHistoryStore.DefaultPageSize);

        _activeSessionId = sessionId;
        _messages.Clear();
        _conversation.Clear();

        foreach (var message in page.Messages)
        {
            _messages.Add(message);
        }

        foreach (var message in page.Messages.TakeLast(ContextHistoryLimit))
        {
            _conversation.Add(new AiChatMessage(message.Role, message.Content));
        }

        _olderMessageCursor = page.NextBeforeMessageId;
        _hasOlderMessages = page.HasMore;

        if (_messages.Count == 0)
        {
            ShowWelcomeMessage();
        }
        else
        {
            ScrollToLatest();
        }

        UpdateHistoryButtons();
    }

    private void StartNewConversation()
    {
        _activeSessionId = null;
        _olderMessageCursor = null;
        _hasOlderMessages = false;
        _conversation.Clear();
        _messages.Clear();

        _isRefreshingSessions = true;
        SessionSelector.SelectedItem = null;
        _isRefreshingSessions = false;

        ShowWelcomeMessage();
        UpdateHistoryButtons();
        MessageInput.Focus();
    }

    private void ShowWelcomeMessage()
    {
        _messages.Add(new ChatHistoryMessage
        {
            Id = 0,
            Role = "assistant",
            Content = ChatWelcomeMessageBuilder.Build(_companion, _avatar),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private void AppendContextMessage(AiChatMessage message)
    {
        _conversation.Add(message);
        if (_conversation.Count > ContextHistoryLimit)
        {
            _conversation.RemoveRange(0, _conversation.Count - ContextHistoryLimit);
        }
    }

    private void UpdateHistoryButtons()
    {
        NewChatButton.IsEnabled = !_isSending;
        SessionSelector.IsEnabled = !_isSending && SessionSelector.Items.Count > 0;
        DeleteSessionButton.IsEnabled = !_isSending && _activeSessionId is not null;
        ClearHistoryButton.IsEnabled = !_isSending && SessionSelector.Items.Count > 0;
        LoadOlderButton.IsEnabled = !_isSending && _hasOlderMessages;
        LoadOlderButton.Visibility = _hasOlderMessages ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetSending(bool sending)
    {
        _isSending = sending;
        MemoryButton.IsEnabled = !sending && _memoryStore is not null;
        SendButton.IsEnabled = !sending;
        MessageInput.IsEnabled = !sending;
        SendButton.Content = sending ? "发送中…" : "发送";
        UpdateHistoryButtons();

        if (sending)
        {
            ConnectionStatusText.Text = "正在联系 Qwen；消息已先保存到本机。";
        }
        else
        {
            MessageInput.Focus();
        }
    }

    private void ScrollToLatest()
    {
        if (_messages.Count > 0)
        {
            MessageList.ScrollIntoView(_messages[_messages.Count - 1]);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _requestCancellation?.Cancel();
        base.OnClosed(e);
    }
}
