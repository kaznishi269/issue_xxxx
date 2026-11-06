using ControllerEngineeringStudio.ApplicationService.Models;

namespace ControllerEngineeringStudio.ApplicationService.Interfaces
{
    /// <summary>
    /// STプログラムサービス
    /// </summary>
    public interface IStProgramService
    {
        /// <summary>
        /// エンジニアリング開始
        /// </summary>
        /// <param name="programName">プログラム名</param>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> StartEngineeringAsync(string programName);

        /// <summary>
        /// プロパティ取得
        /// </summary>
        /// <returns>プログラムコメント</returns>
        Task<GrpcResult<string>> GetPropertyAsync();

        /// <summary>
        /// エンジニアリング終了
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> EndEngineeringAsync();
    }
}
