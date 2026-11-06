using ControllerEngineeringStudio.AiChatTool.ViewModels;
using ControllerEngineeringStudio.AiChatTool.Views;
using ControllerEngineeringStudio.Common.Module;
using ControllerEngineeringStudio.UI.Docking;
using ControllerEngineeringStudio.UI.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerEngineeringStudio.AiChatTool
{
    /// <summary>
    /// AIチャットモジュール
    /// </summary>
    public class AiChatToolModule : IModule
    {
        /// <inheritdoc/>
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddDocking<AiChatToolViewModel, AiChatToolView>(DockingId.AiChatTool);
        }
    }
}
