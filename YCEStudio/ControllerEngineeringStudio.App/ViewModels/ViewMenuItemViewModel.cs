using CommunityToolkit.Mvvm.Messaging;
using ControllerEngineeringStudio.UI.Docking;

namespace ControllerEngineeringStudio.App.ViewModels
{
    /// <summary>
    /// ViewメニューViewModel
    /// </summary>
    public partial class ViewMenuItemViewModel : MenuItemViewModel, IRecipient<DockingStateChangedMessage>
    {
        #region Properties
        /// <inheritdoc/>
        public override bool IsCheckable => true;

        /// <summary>
        /// ドッキングOpen用パラメータ
        /// </summary>
        public required DockingParameter Parameter { get; init; }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="messenger">メッセンジャー</param>
        public ViewMenuItemViewModel(IMessenger messenger)
            : base(messenger)
        {
            IsActive = true;
        }

        /// <inheritdoc/>
        public void Receive(DockingStateChangedMessage message)
        {
            // ドッキング画面の表示/非表示状態をメニューのチェック状態に反映する
            if (Parameter.DockingId == message.DockingId
                && Parameter.Key == message.Key)
            {
                IsChecked = !message.IsHidden;
            }
        }
    }
}
