using ControllerEngineeringStudio.UI.Docking;
using ControllerEngineeringStudio.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ControllerEngineeringStudio.UI.Extensions
{
    /// <summary>
    /// ServiceCollection拡張
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// ViewMappingの追加
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel</typeparam>
        /// <typeparam name="TView">View</typeparam>
        /// <param name="services">ServiceCollection</param>
        /// <param name="id">DockingId</param>
        /// <returns>ServiceCollection</returns>
        public static IServiceCollection AddDocking<TViewModel, TView>(this IServiceCollection services, DockingId id)
            where TView : FrameworkElement
            where TViewModel : class
        {
            if (services
                .Select(x => x.ImplementationInstance)
                .OfType<IViewMapping>()
                .Any(x => x.ViewModelType == typeof(TViewModel) || x.ViewType == typeof(TView)))
            {
                throw new InvalidOperationException("ViewMappingが登録済み");
            }

            services.AddViewAndViewModel<TViewModel, TView>();

            var dockingMapping = new DockingMapping<TViewModel, TView>(id);
            services.AddSingleton<IDockingMapping>(dockingMapping);
            // ViewFactoryでIViewMappingを解決するために登録する
            services.AddSingleton<IViewMapping>(dockingMapping);

            return services;
        }

        /// <summary>
        /// ViewMappingの追加
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel</typeparam>
        /// <typeparam name="TView">View</typeparam>
        /// <param name="services">ServiceCollection</param>
        /// <param name="id">DockingId</param>
        /// <returns>ServiceCollection</returns>
        public static IServiceCollection AddDialog<TViewModel, TView>(this IServiceCollection services)
            where TView : FrameworkElement
            where TViewModel : class
        {
            if (services
                .Select(x => x.ImplementationInstance)
                .OfType<IViewMapping>()
                .Any(x => x.ViewModelType == typeof(TViewModel) || x.ViewType == typeof(TView)))
            {
                throw new InvalidOperationException("ViewMappingが登録済み");
            }

            services.AddViewAndViewModel<TViewModel, TView>();

            var viewMapping = new ViewMapping<TViewModel, TView>();
            services.AddSingleton<IViewMapping>(viewMapping);

            return services;
        }

        /// <summary>
        /// ViewModelとViewをDI登録
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel</typeparam>
        /// <typeparam name="TView">View</typeparam>
        /// <param name="services">ServiceCollection</param>
        /// <returns>ServiceCollection</returns>
        /// <exception cref="InvalidOperationException">重複登録例外</exception>
        private static IServiceCollection AddViewAndViewModel<TViewModel, TView>(this IServiceCollection services)
            where TView : FrameworkElement
            where TViewModel : class
        {
            if (services.Any(x => x.ServiceType == typeof(TViewModel) || x.ServiceType == typeof(TView)))
            {
                throw new InvalidOperationException("ViewModelまたはViewが重複");
            }

            services.AddTransient<TView>();
            services.AddTransient<TViewModel>();

            return services;
        }
    }
}