using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Markup;

namespace ControllerEngineeringStudio.UI.Extensions
{
    /// <summary>
    /// ServiceProviderからResolveする拡張クラス
    /// </summary>
    public class ResolveExtension : MarkupExtension
    {
        /// <summary>
        /// ServiceProviderからResolveする型
        /// </summary>
        public Type Type { get; set; } = null!;

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Ioc.Default.GetRequiredService(Type);
        }
    }
}
