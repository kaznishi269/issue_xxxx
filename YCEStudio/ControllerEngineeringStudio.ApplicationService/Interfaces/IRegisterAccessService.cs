using ControllerEngineeringStudio.ApplicationService.Models;
namespace ControllerEngineeringStudio.ApplicationService.Interfaces
{
    /// <summary>
    /// レジスタアクセスサービス
    /// </summary>
    public interface IRegisterAccessService
    {
        /// <summary>
        /// エンジニアリング開始
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> StartEngineeringAsync();

        /// <summary>
        /// エンジニアリング終了
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> EndEngineeringAsync();

        /// <summary>
        /// レジスタ読み込み
        /// </summary>
        /// <param name="registerName">レジスタ</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>レジスタ値</returns>
        Task<GrpcResult<string>> ReadRegisterAsync(string registerName, CancellationToken cancellationToken);
    }
}
