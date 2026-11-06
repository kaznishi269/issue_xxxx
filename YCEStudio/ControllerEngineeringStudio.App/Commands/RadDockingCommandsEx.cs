using CommunityToolkit.Mvvm.Input;
using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.UI.Docking;
using System.Collections.Specialized;

namespace ControllerEngineeringStudio.App.Commands
{
    /// <summary>
    /// RadDockingのコマンド
    /// </summary>
    public sealed partial class RadDockingCommandsEx
    {
        #region Fields
        /// <summary>ドキュメントクローズサービス</summary>
        private readonly IDocumentCloseService _documentCloseService;

        /// <summary>ドッキングマネージャー</summary>
        private readonly IDockingManager _dockingManager;
        #endregion

        #region Commands
        /// <summary>
        /// 全て閉じる
        /// </summary>
        [RelayCommand]
        private async Task CloseAll()
        {
            await _documentCloseService.SaveAndCloseDocumentsAsync(_dockingManager.Items.OfType<DocumentViewModelBase>());
        }

        /// <summary>
        /// 他を全て閉じる
        /// </summary>
        /// <param name="vm">ViewModel</param>
        [RelayCommand(CanExecute = nameof(CanCloseOthers))]
        private async Task CloseOthers(DocumentViewModelBase vm)
        {
            // 自身以外のドキュメントを閉じる
            await _documentCloseService.SaveAndCloseDocumentsAsync(
                _dockingManager.Items.OfType<DocumentViewModelBase>().Where(x => x != vm));
        }

        /// <summary>
        /// 他を全て閉じるの実行可否
        /// </summary>
        /// <param name="vm">ViewModel</param>
        /// <returns>true:可能、false:不可</returns>
        private bool CanCloseOthers(DocumentViewModelBase vm)
        {
            // 自身以外のドキュメントがあれば実行可能
            return _dockingManager.Items.OfType<DocumentViewModelBase>().Any(x => x != vm);
        }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="documentCloseService">保存確認用サービス</param>
        /// <param name="dockingManager">ドッキングマネージャー</param>
        public RadDockingCommandsEx(IDocumentCloseService documentCloseService, IDockingManager dockingManager)
        {
            _documentCloseService = documentCloseService;
            _dockingManager = dockingManager;
            _dockingManager.ItemsChanged += OnItemChanged;
        }

        /// <summary>
        /// DockingItems変更イベント
        /// </summary>
        /// <param name="sender">未使用</param>
        /// <param name="e">未使用</param>
        private void OnItemChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // ドッキング画面の増減でコマンドの実行可否を更新
            CloseOthersCommand.NotifyCanExecuteChanged();
        }
    }
}
