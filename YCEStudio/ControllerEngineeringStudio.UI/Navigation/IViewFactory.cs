using System.Windows;

namespace ControllerEngineeringStudio.UI.Navigation
{
    /// <summary>
    /// ViewFactoryインターフェース
    /// </summary>
    public interface IViewFactory
    {
        /// <summary>
        /// ViewModelの生成
        /// </summary>
        /// <param name="type">ViewModelのタイプ</param>
        /// <returns>ViewModel</returns>
        public object CreateViewModel(Type type);

        /// <summary>
        /// ViewModelからViewの生成
        /// </summary>
        /// <param name="viewModel">ViewModel</param>
        /// <returns>View</returns>
        public FrameworkElement CreateView(object viewModel);
    }
}
