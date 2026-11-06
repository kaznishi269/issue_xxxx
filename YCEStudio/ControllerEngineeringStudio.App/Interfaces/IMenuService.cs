using ControllerEngineeringStudio.App.ViewModels;
using System.Collections.ObjectModel;

namespace ControllerEngineeringStudio.App.Interfaces
{
    /// <summary>
    /// メニューサービスインターフェース
    /// </summary>
    public interface IMenuService
    {
        /// <summary>
        /// MenuItemViewModels
        /// </summary>
        ReadOnlyObservableCollection<MenuItemViewModel> Items { get; }
    }
}
