using ControllerEngineeringStudio.Common.Module;
using ControllerEngineeringStudio.SampleTool.ViewModels;
using ControllerEngineeringStudio.SampleTool.Views;
using ControllerEngineeringStudio.UI.Docking;
using ControllerEngineeringStudio.UI.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerEngineeringStudio.SampleTool
{
    public class SampleToolModule : IModule
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddDocking<SampleToolViewModel, SampleToolView>(DockingId.SampleTool);
        }
    }
}
