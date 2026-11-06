using ControllerEngineeringStudio.Common.Module;
using ControllerEngineeringStudio.UI.Dialog;
using ControllerEngineeringStudio.UI.Docking;
using ControllerEngineeringStudio.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerEngineeringStudio.UI
{
    /// <summary>
    /// UIモジュール
    /// </summary>
    public class UIModule : IModule
    {
        /// <inheritdoc/>
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IViewFactory, ViewFactory>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<CustomDockingPanesFactory>();
            services.AddSingleton<IDockingManager, DockingManager>();
            services.AddSingleton<ILayoutService, LayoutService>();
        }
    }
}
