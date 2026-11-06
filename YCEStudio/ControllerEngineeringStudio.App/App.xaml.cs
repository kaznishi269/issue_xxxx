using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ControllerEngineeringStudio.AiChatTool;
using ControllerEngineeringStudio.App.Commands;
using ControllerEngineeringStudio.App.Interfaces;
using ControllerEngineeringStudio.App.Services;
using ControllerEngineeringStudio.App.ViewModels;
using ControllerEngineeringStudio.App.Views;
using ControllerEngineeringStudio.ApplicationService;
using ControllerEngineeringStudio.Common;
using ControllerEngineeringStudio.Common.Startup;
using ControllerEngineeringStudio.SampleDocument;
using ControllerEngineeringStudio.SampleTool;
using ControllerEngineeringStudio.UI;
using ControllerEngineeringStudio.UI.Dialog;
using ControllerEngineeringStudio.UI.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Globalization;
using System.Windows;
using Telerik.Windows.Controls.Animation;

namespace ControllerEngineeringStudio.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>ロガー</summary>
        private ILogger<App> _logger = null!;

        /// <summary>
        /// サービスプロバイダー
        /// </summary>
        public IServiceProvider Services { get; private set; } = null!;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public App()
        {
            //Office2019Palette.LoadPreset(Office2019Palette.ColorVariation.Dark);

            // telerikコントロールのアニメーションを無効化
            AnimationManager.IsGlobalAnimationEnabled = false;
        }

        /// <summary>
        /// サービスプロバイダーの生成、DI登録
        /// </summary>
        /// <param name="startupArgs">起動引数</param>
        /// <returns>サービスプロバイダー</returns>
        private static IServiceProvider ConfigureServices(StartupArgs startupArgs)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMessenger, WeakReferenceMessenger>();
            services.AddSingleton(startupArgs);
            services.AddSingleton<IMenuService, MenuService>();
            services.AddSingleton<IDocumentCloseService, DocumentCloseService>();
            services.AddSingleton<IIpcService, IpcService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddDialog<ConfirmSaveWindowViewModel, ConfirmSaveWindow>();
            services.AddSingleton<StatusBarViewModel>();
            services.AddSingleton<RadDockingCommandsEx>();
            services.AddLogging(x =>
            {
                x.ClearProviders();
                x.AddNLog("NLog\\NLog.config");
            });

            CommonModule.RegisterServices(services);
            ApplicationServiceModule.RegisterServices(services);
            UIModule.RegisterServices(services);
            SampleToolModule.RegisterServices(services);
            SampleDocumentModule.RegisterServices(services);
            AiChatToolModule.RegisterServices(services);

            return services.BuildServiceProvider();
        }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
//#if DEBUG
//            if (!System.Diagnostics.Debugger.IsAttached)
//            {
//                // デバッグ時、アタッチ用にメッセージ表示
//                MessageBox.Show($"ProcessID={Environment.ProcessId}", Common.Resources.CommonStrings.AppName);
//            }
//#endif

            var startupArgs = new StartupArgs(e.Args);
            Services = ConfigureServices(startupArgs);
            Ioc.Default.ConfigureServices(Services);
            _logger = Services.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("StartupEventArgs:{StartupEventArgs}, StartupArgs:{StartupArgs}",
                string.Join(" ", e.Args),
                startupArgs);

            // 例外キャッチが漏れた例外を捕捉する
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            SetCulture(startupArgs);

            base.OnStartup(e);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();
            if (startupArgs.IsMinimized)
            {
                // 最小化起動
                mainWindow.WindowState = WindowState.Minimized;
            }
            mainWindow.Show();
        }

        /// <summary>
        /// UIスレッドで発生した未処理の例外を処理
        /// </summary>
        /// <param name="sender">発生元</param>
        /// <param name="e">例外情報</param>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // ログ出力
            _logger.LogError(e.Exception, "UIスレッドで発生した未処理の例外");

            // エラー情報を表示して、アプリは継続する
            IDialogService dialogService = Services.GetRequiredService<IDialogService>();
            dialogService.ShowMessageBox(e.Exception.ToString(), Common.Resources.CommonStrings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        }

        /// <summary>
        /// Taskで発生した未処理の例外を処理
        /// </summary>
        /// <param name="sender">発生元</param>
        /// <param name="e">例外情報</param>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // アプリケーションの異常終了を防止
            _logger.LogError(e.Exception, "Taskで発生した未処理の例外");
            e.SetObserved();
        }

        /// <summary>
        /// アプリケーションドメインで発生した未処理の例外を処理
        /// </summary>
        /// <param name="sender">発生元</param>
        /// <param name="e">例外情報</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // ここで補足される例外は致命的である可能性が高いため、アプリケーションは終了する場合がある
            var ex = e.ExceptionObject as Exception;
            _logger.LogCritical(ex, "アプリケーションドメインで発生した未処理の例外");
        }

        /// <summary>
        /// カルチャ設定
        /// </summary>
        /// <param name="startupArgs">起動引数</param>
        private void SetCulture(StartupArgs startupArgs)
        {
            if (!StartupArgsKey.LangDic.TryGetValue(startupArgs.Lang, out var culture))
            {
                // 対応していない言語のため、デフォルトの言語を使用する
                return;
            }

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
