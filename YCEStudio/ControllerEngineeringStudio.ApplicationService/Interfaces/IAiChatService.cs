using ControllerEngineeringStudio.ApplicationService.Models;

namespace ControllerEngineeringStudio.ApplicationService.Interfaces
{
    /// <summary>
    /// AIチャットサービス
    /// </summary>
    public interface IAiChatService
    {
        /// <summary>
        /// エンジニアリング開始
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> StartEngineeringAsync();

        /// <summary>
        /// メッセージ送信
        /// </summary>
        /// <returns>メッセージ応答</returns>
        Task<GrpcResult<string>> SendMessageAsync(string message);

        /// <summary>
        /// エンジニアリング終了
        /// </summary>
        /// <returns>true:成功、false:失敗</returns>
        Task<bool> EndEngineeringAsync();
    }
}
