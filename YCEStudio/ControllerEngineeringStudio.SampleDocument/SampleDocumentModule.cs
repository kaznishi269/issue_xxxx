using ControllerEngineeringStudio.Common.Module;
using ControllerEngineeringStudio.SampleDocument.ViewModels;
using ControllerEngineeringStudio.SampleDocument.Views;
using ControllerEngineeringStudio.UI.Docking;
using ControllerEngineeringStudio.UI.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerEngineeringStudio.SampleDocument
{
    public class SampleDocumentModule : IModule
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddDocking<SampleDocumentViewModel, SampleDocumentView>(DockingId.SampleDocument);
            services.AddDialog<NewSampleDocumentDialogViewModel, NewSampleDocumentDialog>();
        }
    }

}
