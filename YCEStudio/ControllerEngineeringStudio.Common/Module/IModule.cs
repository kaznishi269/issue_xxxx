using Microsoft.Extensions.DependencyInjection;

namespace ControllerEngineeringStudio.Common.Module
{
    /// <summary>
    /// Moduleインターフェース
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// DI登録
        /// </summary>
        /// <param name="services">ServiceCollection</param>
        public abstract static void RegisterServices(IServiceCollection services);
    }
}
