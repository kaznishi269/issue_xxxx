using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Models;
using Microsoft.Extensions.Logging;
using Yaskawa.ControllerEngineeringService.AiChat.V1;

namespace ControllerEngineeringStudio.ApplicationService.Services
{
    /// <summary>
    /// AIチャットサービス
    /// </summary>
    internal class AiChatService : GrpcServiceBase<AiChatService>, IAiChatService
    {
        /// <summary>AIチャットサービス</summary>
        private readonly AiChatGrpcService.AiChatGrpcServiceClient _aiChatService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="context">EngineeringServer情報</param>
        /// <param name="aiChatService">AIチャットサービス</param>
        public AiChatService(
            ILogger<AiChatService> logger,
            EngineeringServerContext context,
            AiChatGrpcService.AiChatGrpcServiceClient aiChatService)
                : base(logger, context)
        {
            _aiChatService = aiChatService;
        }

        /// <inheritdoc/>
        public async Task<GrpcResult<string>> SendMessageAsync(string message)
        {
            var result = await ExecuteGrpcAsync(
                _aiChatService.SendMessageAsync(new SendMessageReq
                {
                    EngineeringHandle = _engineeringHandle,
                    Message = message
                }));
            if (!result.Success)
            {
                return new(result.ErrorCode, string.Empty);
            }
            return new(result.ErrorCode, result.Result?.Message);
        }

        /// <inheritdoc/>
        public async Task<bool> StartEngineeringAsync()
        {
            if (IsEngineering)
            {
                // 既に開始されている
                return true;
            }
            if (!_context.IsConnected)
            {
                // 接続されていない
                _logger.LogDebug("SessionHandle無し");
                return false;
            }

            var result = await ExecuteGrpcAsync(
                _aiChatService.StartEngineeringAsync(new StartEngineeringReq
                {
                    SessionHandle = _context.SessionHandle,
                }));
            if (result.Success
                && !string.IsNullOrEmpty(result.Result?.EngineeringHandle))
            {
                _engineeringHandle = result.Result.EngineeringHandle;
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> EndEngineeringAsync()
        {
            if (!IsEngineering)
            {
                // 開始されていないのでOK
                return true;
            }

            var result = await ExecuteGrpcAsync(
            _aiChatService.EndEngineeringAsync(new EndEngineeringReq
            {
                EngineeringHandle = _engineeringHandle
            }));
            if (result.Success)
            {
                _engineeringHandle = string.Empty;
                return true;
            }

            return false;
        }
    }
}
