using ControllerEngineeringStudio.UI.Navigation;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace ControllerEngineeringStudio.UI.Dialog
{
    /// <summary>
    /// ダイアログサービス
    /// </summary>
    internal class DialogService : IDialogService
    {
        #region Fields
        /// <summary>ViewFactory</summary>
        private readonly IViewFactory _viewFactory;

        /// <summary>ロガー</summary>
        private readonly ILogger<DialogService> _logger;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="viewFactory">ViewFactory</param>
        /// <param name="logger">Logger</param>
        public DialogService(IViewFactory viewFactory, ILogger<DialogService> logger)
        {
            _viewFactory = viewFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult = MessageBoxResult.None)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon, defaultResult);
        }

        /// <inheritdoc/>
        public TResult? ShowDialog<TViewModel, TResult>(object? parameter = null) where TViewModel : DialogViewModelBase<TResult>
        {
            _logger.LogInformation("TViewModel:{TViewModel}, TResult:{TResult}, Parameter:{Parameter}",
                typeof(TViewModel).FullName,
                typeof(TResult).FullName,
                parameter?.GetType().FullName);

            var vm = (TViewModel)_viewFactory.CreateViewModel(typeof(TViewModel));
            var view = _viewFactory.CreateView(vm);
            if (view is not Window window)
            {
                throw new InvalidOperationException("viewがWindowでない。");
            }

            if (!vm.Initialize(parameter))
            {
                return default;
            }

            // ViewModelからViewを閉じるための関数
            void OnRequestClose(bool isResult)
            {
                window.DialogResult = isResult;
            }
            vm.RequestClose += OnRequestClose;

            try
            {
                // アクティブなウィンドウを親とする
                var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                        ?? Application.Current.MainWindow;
                if (owner != null)
                {
                    window.Owner = owner;

                    // 親ウィンドウが表示されていない場合、表示する
                    if (owner.WindowState == WindowState.Minimized)
                    {
                        owner.WindowState = WindowState.Normal;
                    }
                    owner.Activate();
                }

                if (window.ShowDialog() == true)
                {
                    return vm.Result;
                }
                else
                {
                    return default;
                }
            }
            finally
            {
                vm.RequestClose -= OnRequestClose;
            }
        }
    }
}
