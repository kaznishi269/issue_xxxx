using CommunityToolkit.Mvvm.ComponentModel;
using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.Common.Startup;
using ControllerEngineeringStudio.UI.Dialog;
using ControllerEngineeringStudio.UI.Docking;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace ControllerEngineeringStudio.App.ViewModels
{
    /// <summary>
    /// MainWindowViewModel
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        #region Fields
        /// <summary>ドッキング管理</summary>
        private readonly IDockingManager _dockingManager;

        /// <summary>ロガー</summary>
        private readonly ILogger<MainWindowViewModel> _logger;

        /// <summary>メニューサービス</summary>
        private readonly IMenuService _menuService;

        /// <summary>ダイアログサービス</summary>
        private readonly IDialogService _dialogService;

        /// <summary>EngineeringServerサービス</summary>
        private readonly IEngineeringServerService _engineeringServerService;

        /// <summary>ドキュメントクローズサービス</summary>
        private readonly IDocumentCloseService _documentCloseService;

        /// <summary>起動引数</summary>
        private readonly StartupArgs _startupArgs;
        #endregion

        #region Properties
        /// <summary>タイトル</summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>ビジー</summary>
        [ObservableProperty]
        private bool _isBusy = true;

        /// <summary>
        /// ドッキングViewModelCollection
        /// </summary>
        public ReadOnlyObservableCollection<DockingViewModelBase> DockingItems => _dockingManager.Items;

        /// <summary>
        /// ViewメニューアイテムCollection
        /// </summary>
        public ReadOnlyObservableCollection<MenuItemViewModel> MenuItemViewModels => _menuService.Items;

        /// <summary>
        /// ステータスバーViewModel
        /// </summary>
        public StatusBarViewModel StatusBarViewModel { get; }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dockingManager">ドッキング管理</param>
        /// <param name="startupArgs">起動引数</param>
        /// <param name="logger">ロガー</param>
        /// <param name="menuService">メニューサービス</param>
        /// <param name="statusBarViewModel">StatusBarViewModel</param>
        public MainWindowViewModel(IDockingManager dockingManager, StartupArgs startupArgs,
            ILogger<MainWindowViewModel> logger, IMenuService menuService, StatusBarViewModel statusBarViewModel,
            IDialogService dialogService, IEngineeringServerService engineeringServerService, IDocumentCloseService documentCloseService)
        {
            _dockingManager = dockingManager;
            _logger = logger;
            _menuService = menuService;
            _startupArgs = startupArgs;
            StatusBarViewModel = statusBarViewModel;
            _dialogService = dialogService;
            _engineeringServerService = engineeringServerService;
            _documentCloseService = documentCloseService;

            CreateTitle();
        }

        /// <summary>
        /// アプリケーションタイトル生成
        /// </summary>
        private void CreateTitle()
        {
            Title = Common.Resources.CommonStrings.AppName;
            if (!string.IsNullOrEmpty(_startupArgs.ProjectFilePath))
            {
                // プロジェクトファイルパスからプロジェクト名を取得し、タイトルに付与
                try
                {
                    Title += " " + System.IO.Path.GetFileNameWithoutExtension(_startupArgs.ProjectFilePath);
                }
                catch (Exception)
                {
                    // 無視
                }
            }
            if (!string.IsNullOrEmpty(_startupArgs.CPU))
            {
                // 機種名をタイトルに付与
                Title += $" [{_startupArgs.CPU}]";
            }
        }

        /// <summary>
        /// ドキュメントの保存/閉じる
        /// </summary>
        /// <param name="documents">対象のDocumentViewModelBase</param>
        /// <returns>true:成功、false:失敗、キャンセル</returns>
        public async Task<bool> SaveAndCloseDocumentsAsync(IReadOnlyList<DocumentViewModelBase> documents)
        {
            try
            {
                return await _documentCloseService.SaveAndCloseDocumentsAsync(documents);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "想定外のエラー");
                return false;
            }
        }

        /// <summary>
        /// 読み込み処理
        /// </summary>
        /// <returns>Task</returns>
        public async Task LoadAsync()
        {
            try
            {
                // EngineeringServerの開始
                // TODO：2026/08/26 (YE)Yuta Hatanaka PortNoとIPAddress
                if (!await _engineeringServerService.StartEngineeringAsync(new ApplicationService.Models.StartEngineeringParameter(
                    _startupArgs.IsOnline,
                    2,
                    "127.0.0.1",
                    _startupArgs.MotionFactoryId,
                    _startupArgs.ProcessId)))
                {
                    _dialogService.ShowMessageBox("Yaskawa.ControllerEngineeringServiceを開始できませんでした。", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "想定外のエラー");
            }
        }

        /// <summary>
        /// クローズ処理
        /// </summary>
        /// <returns>true:成功、false:失敗、キャンセル</returns>
        public async Task<bool> CloseAsync()
        {
            try
            {
                // ドキュメントの保存
                if (!await _documentCloseService.SaveDocumentsAsync(_dockingManager.Items.OfType<DocumentViewModelBase>()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // 例外発生時、画面は閉じない
                _logger.LogError(ex, "想定外のエラー");
                return false;
            }

            try
            {
                if (_dockingManager is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync();
                }

                // EngineeringServerの終了
                await _engineeringServerService.EndEngineeringAsync();
            }
            catch (Exception ex)
            {
                // 後処理で例外が発生しているのみなので、画面は閉じる
                _logger.LogError(ex, "想定外のエラー");
            }

            return true;
        }
    }
}
