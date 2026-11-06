using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.App.ViewModels;
using ControllerEngineeringStudio.UI.Docking;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;

namespace ControllerEngineeringStudio.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        /// <summary>ロガー</summary>
        private readonly ILogger<MainWindow> _logger;

        /// <summary>プロセス間通信サービス</summary>
        private readonly IIpcService _ipcService;

        /// <summary>レイアウトサービス</summary>
        private readonly ILayoutService _layoutService;

        /// <summary>WindowをのCloseしてもよいか？</summary>
        private bool _canClose = false;

        /// <summary>WindowのClosing中か</summary>
        private bool _isClosing = false;

        /// <summary>RadDockingのPreviewClose中か</summary>
        private bool _isPreviewClosing = false;
        #endregion

        /// <summary>
        /// MainWindowViewModel
        /// </summary>
        private MainWindowViewModel ViewModel
            => DataContext as MainWindowViewModel ?? throw new InvalidOperationException("DataContext不正");

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="customDockingPanesFactory">DockingPanesFactory</param>
        /// <param name="logger">ロガー</param>
        /// <param name="ipcService">プロセス間通信サービス</param>
        /// <param name="layoutService">レイアウトサービス</param>
        public MainWindow(CustomDockingPanesFactory customDockingPanesFactory, ILogger<MainWindow> logger,
            IDocumentCloseService documentCloseService, IIpcService ipcService,
            ILayoutService layoutService)
        {
            _logger = logger;
            _ipcService = ipcService;
            _layoutService = layoutService;

            InitializeComponent();

            _docking.DockingPanesFactory = customDockingPanesFactory;
        }

        #region RadDockingEvents
        /// <summary>
        /// RadDockingコンパス表示前イベント
        /// </summary>
        /// <param name="sender">RadDocking</param>
        /// <param name="e">コンパス表示前イベントパラメータ</param>
        private void OnRadDockingPreviewShowCompass(object sender, PreviewShowCompassEventArgs e)
        {
            // DocumnetをTool側にドッキングできないようにする
            var isRootCompass = e.Compass is RootCompass;
            var splitContainer = e.DraggedElement as RadSplitContainer;
            if (splitContainer != null)
            {
                var isTargetDocument = e.TargetGroup == null ? true : e.TargetGroup.EnumeratePanes().Any(p => p is RadDocumentPane);
                if (splitContainer.EnumeratePanes().Any(p => p is RadDocumentPane))
                {
                    e.Canceled = isRootCompass || !isTargetDocument;
                }
                else
                {
                    e.Canceled = !isRootCompass && isTargetDocument;
                }
            }
        }

        /// <summary>
        /// ドッキング画面クローズ前イベント
        /// </summary>
        /// <param name="sender">RadDocking</param>
        /// <param name="e">状態変更イベントパラメータ</param>
        private void OnRadDockingPreviewClose(object sender, StateChangeEventArgs e)
        {
            var documents = e.Panes.Select(x => x.DataContext).OfType<DocumentViewModelBase>().ToList();
            if (!documents.Any())
            {
                // ツール画面の場合、RadDockingに任せる
                return;
            }

            // ドキュメント画面の場合、保存処理や閉じる処理が必要になるため、ここではキャンセルして閉じる処理を行う
            e.Handled = true;
            if (!_isPreviewClosing)
            {
                _isPreviewClosing = true;
                _ = PreviewCloseAsync(documents);
            }
        }

        /// <summary>
        /// ドッキング画面クローズ前イベント
        /// </summary>
        /// <param name="documents">対象のドキュメント画面のViewModel</param>
        /// <returns>Task</returns>
        private async Task PreviewCloseAsync(IReadOnlyList<DocumentViewModelBase> documents)
        {
            try
            {
                await ViewModel.SaveAndCloseDocumentsAsync(documents);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "想定外のエラー");
            }
            _isPreviewClosing = false;
        }

        /// <summary>
        /// ドッキング画面のレイアウト保存
        /// </summary>
        /// <param name="sender">RadDocking</param>
        /// <param name="e">イベント引数</param>
        private void OnRadDockingElementLayoutSaving(object sender, LayoutSerializationSavingEventArgs e)
        {
            _layoutService.OnRadDockingElementLayoutSaving(sender, e);
        }

        /// <summary>
        /// ドッキング画面のレイアウト読込
        /// </summary>
        /// <param name="sender">RadDocking</param>
        /// <param name="e">イベント引数</param>
        private void OnRadDockingElementLoading(object sender, LayoutSerializationLoadingEventArgs e)
        {
            _layoutService.OnRadDockingElementLoading(sender, e);
        }
        #endregion

        #region WindowEvents
        /// <summary>
        /// Windowロード後イベント
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">イベント引数</param>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 2重実行防止
            Loaded -= OnWindowLoaded;

            await ViewModel.LoadAsync();

            try
            {
                await _layoutService.LoadLayoutAsync(_docking);
                _ipcService.Attach(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "想定外のエラー");
            }

            ViewModel.IsBusy = false;
        }

        /// <summary>
        /// Windowクローズ中イベント
        /// </summary>
        /// <param name="e">キャンセルイベント引数</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            // ドキュメントを閉じる場合、保存する/しないを確認する
            // 確認が終わるまでは常にキャンセルし、閉じないようにする
            if (_canClose)
            {
                // 閉じてもOK
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            if (!_isClosing)
            {
                _isClosing = true;
                _ = CloseWindowAsync();
            }
        }

        /// <summary>
        /// Windowクローズ
        /// </summary>
        /// <returns>Task</returns>
        private async Task CloseWindowAsync()
        {
            try
            {
                // レイアウト保存
                // CloseAsyncでDockingの解放が行われるため、先に保存する
                _layoutService.SaveLayout(_docking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "想定外のエラー");
            }

            if (!await ViewModel.CloseAsync())
            {
                _isClosing = false;
                return;
            }

            try
            {
                _ipcService.Detach();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "想定外のエラー");
            }

            // 確認が終わったので、フラグを立てて閉じる
            _canClose = true;
            _ = Dispatcher.BeginInvoke(Close);
        }
        #endregion
    }
}