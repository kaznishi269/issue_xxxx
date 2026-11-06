using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace ControllerEngineeringStudio.UI.Navigation
{
    /// <summary>
    /// ViewFactory
    /// </summary>
    internal sealed class ViewFactory : IViewFactory
    {
        #region Fields
        /// <summary>サービスプロバイダー</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>ViewModelとViewのマッピング</summary>
        private readonly Dictionary<Type, Type> _mappings;
        /// <summary>ロガー</summary>
        private readonly ILogger<ViewFactory> _logger;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="serviceProvider">サービスプロバイダー</param>
        /// <param name="mappings">ViewModelとViewのマッピング</param>
        /// <param name="logger">ロガー</param>
        public ViewFactory(IServiceProvider serviceProvider, IEnumerable<IViewMapping> mappings, ILogger<ViewFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _mappings = mappings.ToDictionary(x => x.ViewModelType, x => x.ViewType);
            _logger = logger;
        }

        /// <inheritdoc/>
        public object CreateViewModel(Type type)
        {
            _logger.LogInformation("Type:{Type}", type.FullName);
            return _serviceProvider.GetRequiredService(type);
        }

        /// <inheritdoc/>
        public FrameworkElement CreateView(object viewModel)
        {
            _logger.LogInformation("ViewModel:{ViewModel}", viewModel.GetType().FullName);
            var vmType = viewModel.GetType();
            if (!_mappings.TryGetValue(vmType, out var viewType))
            {
                throw new InvalidOperationException($"Viewが登録されていない。ViewModel={vmType.FullName}");
            }

            // 指定のViewModelをViewのDataContextに設定する
            var view = (FrameworkElement)_serviceProvider.GetRequiredService(viewType);
            view.DataContext = viewModel;

            return view;
        }
    }
}
