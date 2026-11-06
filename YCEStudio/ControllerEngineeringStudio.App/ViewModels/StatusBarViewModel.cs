using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ControllerEngineeringStudio.Common.Resources;
using ControllerEngineeringStudio.UI.Messages;

namespace ControllerEngineeringStudio.App.ViewModels
{
    /// <summary>
    /// ステータスバーViewModel
    /// </summary>
    public partial class StatusBarViewModel : ObservableRecipient, IRecipient<StatusBarMessage>
    {
        /// <summary>ステータス</summary>
        [ObservableProperty]
        private string _statusText = CommonStrings.Ready;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="messenger">メッセンジャー</param>
        public StatusBarViewModel(IMessenger messenger)
            : base(messenger)
        {
            IsActive = true;
        }

        /// <inheritdoc/>
        public void Receive(StatusBarMessage message)
        {
            if (string.IsNullOrEmpty(message.StatusText))
            {
                StatusText = CommonStrings.Ready;
            }
            else
            {
                StatusText = message.StatusText;
            }
        }
    }
}
