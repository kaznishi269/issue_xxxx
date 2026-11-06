using ControllerEngineeringStudio.ApplicationService.Models;

namespace ControllerEngineeringStudio.ApplicationService.Interfaces
{
    /// <summary>
    /// エンジニアリングサーバーサービス
    /// </summary>
    public interface IEngineeringServerService
    {
        /// <summary>
        /// EngineeringServerの開始
        /// </summary>
        /// <param name="parameter">パラメーター</param>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> StartEngineeringAsync(StartEngineeringParameter parameter);

        /// <summary>
        /// EngineeringServerの終了
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> EndEngineeringAsync();
    }
}
