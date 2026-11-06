using ControllerEngineeringStudio.ApplicationService.Models;

namespace ControllerEngineeringStudio.ApplicationService.Interfaces
{
    /// <summary>
    /// 変数サービス
    /// </summary>
    public interface IVariableService
    {
        /// <summary>
        /// エンジニアリング開始
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> StartEngineeringAsync();

        /// <summary>
        /// 変数検索
        /// </summary>
        /// <param name="symbolName">変数名</param>
        /// <param name="programName">プログラム名(ローカル時のみ使用)</param>
        /// <returns>レジスタ</returns>
        Task<GrpcResult<string>> FindSymbolAsync(string symbolName, string programName);

        /// <summary>
        /// レジスタ検索
        /// </summary>
        /// <param name="registerName">レジスタ名</param>
        /// <param name="programName">プログラム名(ローカル時のみ使用)</param>
        /// <returns>変数名</returns>
        Task<GrpcResult<string>> FindRegisterAsync(string registerName, string programName);

        /// <summary>
        /// エンジニアリング終了
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> EndEngineeringAsync();
    }
}
