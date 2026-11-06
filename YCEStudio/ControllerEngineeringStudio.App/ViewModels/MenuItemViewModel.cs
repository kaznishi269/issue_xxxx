using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ControllerEngineeringStudio.App.ViewModels
{
    /// <summary>
    /// メニューアイテムViewModel
    /// </summary>
    public partial class MenuItemViewModel : ObservableRecipient
    {
        #region Properties
        /// <summary>チェックON/OFF</summary>
        [ObservableProperty]
        private bool _isChecked = false;

        /// <summary>
        /// ヘッダー
        /// </summary>
        public string Header { get; init; } = string.Empty;

        /// <summary>
        /// メニュー説明
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// チェック可否
        /// </summary>
        public virtual bool IsCheckable => false;

        /// <summary>
        /// セパレーターか？
        /// </summary>
        public bool IsSeparator { get; init; } = false;

        /// <summary>
        /// 子のViewメニューアイテム
        /// </summary>
        public ObservableCollection<MenuItemViewModel> SubItems { get; } = new();
        #endregion

        /// <summary>
        /// メニュー実行コマンド
        /// </summary>
        public ICommand? ExecuteCommand { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="messenger">メッセンジャー</param>
        public MenuItemViewModel(IMessenger messenger)
            : base(messenger)
        {
        }
    }
}
