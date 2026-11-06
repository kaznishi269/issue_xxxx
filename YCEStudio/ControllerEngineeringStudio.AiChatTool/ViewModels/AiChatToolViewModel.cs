using CommunityToolkit.Mvvm.ComponentModel;
using ControllerEngineeringStudio.AiChatTool.Models;
using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.UI.Docking;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Telerik.Windows.Controls.ConversationalUI;
using Telerik.Windows.Controls.Docking;

namespace ControllerEngineeringStudio.AiChatTool.ViewModels
{
    /// <summary>
    /// AIチャットViewModel
    /// </summary>
    public partial class AiChatToolViewModel : ToolViewModelBase
    {
        /// <summary>ロガー</summary>
        private readonly ILogger<AiChatToolViewModel> _logger;

        /// <summary>AIチャットサービス</summary>
        private readonly IAiChatService _aiChatService;

        /// <inheritdoc/>
        public override DockingId DockingId => DockingId.AiChatTool;

        /// <inheritdoc/>
        public override DockState InitialPosition => DockState.DockedRight;

        /// <summary>応答側のAuthor</summary>
        private Author _aiAuthor = new Author("Yaskawa Copilot");

        /// <summary>
        /// 要求側のAuthor
        /// </summary>
        public Author CurrentAuthor { get; } = new Author("You");

        /// <summary>
        /// チャットメッセージ
        /// </summary>
        public ObservableCollection<ChatMessageModel> Messages { get; } = new();

        /// <summary>AI処理中テキスト</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTypingIndicator))]
        private string _typingIndicatorText = string.Empty;

        /// <summary>
        /// AIが処理中か？
        /// </summary>
        public bool IsTypingIndicator => !string.IsNullOrEmpty(TypingIndicatorText);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="aiChatService">AIチャットサービス</param>
        public AiChatToolViewModel(ILogger<AiChatToolViewModel> logger, IAiChatService aiChatService)
        {
            _logger = logger;
            _aiChatService = aiChatService;
            Messages.CollectionChanged += OnMessagesChanged;
        }

        /// <summary>
        /// チャットメッセージコレクション変更
        /// </summary>
        /// <param name="sender">未使用</param>
        /// <param name="e">コレクション変更イベント引数</param>
        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add ||
                e.NewItems is null)
            {
                return;
            }

            // 追加された要求側のメッセージをAIサービスへ
            foreach (object? item in e.NewItems!)
            {
                if (item is not ChatMessageModel message)
                {
                    continue;
                }

                if (message.Author.Name != CurrentAuthor.Name)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(message.Text))
                {
                    Messages.Remove(message);
                    continue;
                }

                _ = SendMessageAsync(message);
            }
        }

        /// <summary>
        /// 応答メッセージの追加
        /// </summary>
        /// <param name="text">メッセージ</param>
        private void AddResponseMessage(string text)
        {
            Messages.Add(new ChatMessageModel
            {
                Author = _aiAuthor,
                Text = text,
                CreationDate = DateTime.Now,
            });
        }

        /// <summary>
        /// AIへメッセージ送信
        /// </summary>
        /// <param name="chatMessage">要求メッセージ</param>
        /// <returns>Task</returns>
        private async Task SendMessageAsync(ChatMessageModel chatMessage)
        {
            TypingIndicatorText = "処理中...";

            // AIサービスを利用して要求/応答
            try
            {
                var res = await _aiChatService.SendMessageAsync(chatMessage.Text);
                AddResponseMessage(res.Result!);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "想定外のエラー");
            }
            finally
            {
                TypingIndicatorText = string.Empty;
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> InitializeAsync(DockingParameter parameter)
        {
            await base.InitializeAsync(parameter);
            _defaultHeader = Key;
            Header = _defaultHeader;

            try
            {
                await _aiChatService.StartEngineeringAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "想定外のエラー");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            try
            {
                await _aiChatService.EndEngineeringAsync();
            }
            catch (Exception)
            {
                // 終了処理なので、例外無視
            }
        }
    }
}
