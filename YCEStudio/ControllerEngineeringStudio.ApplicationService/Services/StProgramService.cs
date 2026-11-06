using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Models;
using Microsoft.Extensions.Logging;
using Yaskawa.ControllerEngineeringService.STProgramming.V1;

namespace ControllerEngineeringStudio.ApplicationService.Services
{
    /// <summary>
    /// STプログラムサービス
    /// </summary>
    internal class StProgramService : GrpcServiceBase<StProgramService>, IStProgramService
    {
        /// <summary>STプログラムサービス</summary>
        private readonly STProgrammingGrpcService.STProgrammingGrpcServiceClient _stProgrammingService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="context">EngineeringServer情報</param>
        /// <param name="stProgrammingService">STプログラムサービス</param>
        public StProgramService(
            ILogger<StProgramService> logger,
            EngineeringServerContext context,
            STProgrammingGrpcService.STProgrammingGrpcServiceClient stProgrammingService)
                : base(logger, context)
        {
            _stProgrammingService = stProgrammingService;
        }

        /// <inheritdoc/>
        public async Task<GrpcResult<string>> GetPropertyAsync()
        {
            var result = await ExecuteGrpcAsync(
                _stProgrammingService.LadderProgGetPropertyAsync(new LadderProgGetPropertyReq
                {
                    EngineeringHandle = _engineeringHandle
                }));
            if (!result.Success)
            {
                return new(result.ErrorCode, string.Empty);
            }
            return new(result.ErrorCode, result.Result?.ProgramComment);
        }

        /// <inheritdoc/>
        public async Task<bool> StartEngineeringAsync(string programName)
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
                _stProgrammingService.StartEngineeringAsync(new StartEngineeringReq
                {
                    SessionHandle = _context.SessionHandle,
                    IsOnline = _context.IsOnline,
                    MotionFactoryId = _context.MotionFactoryId,
                    PrgFile = programName
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
            _stProgrammingService.EndEngineeringAsync(new EndEngineeringReq
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
