using ControllerEngineeringStudio.ApplicationService.Constants;
using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Models;
using Microsoft.Extensions.Logging;
using Yaskawa.ControllerEngineeringService.NativeEnginnering.V1;

namespace ControllerEngineeringStudio.ApplicationService.Services
{
    internal class RegisterAccessService : GrpcServiceBase<RegisterAccessService>, IRegisterAccessService
    {
        /// <summary>NativeEngineeringサービス</summary>
        private readonly NativeEngineeringGrpcService.NativeEngineeringGrpcServiceClient _nativeEngineeringService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="context">EngineeringServer情報</param>
        /// <param name="nativeEngineeringService">NativeEngineeringサービス</param>
        public RegisterAccessService(
            ILogger<RegisterAccessService> logger,
            EngineeringServerContext context,
            NativeEngineeringGrpcService.NativeEngineeringGrpcServiceClient nativeEngineeringService)
            : base(logger, context)
        {
            _nativeEngineeringService = nativeEngineeringService;
        }

        /// <inheritdoc/>
        public async Task<GrpcResult<string>> ReadRegisterAsync(string registerName, CancellationToken cancellationToken)
        {
            var result = await ExecuteGrpcAsync(
                _nativeEngineeringService.RegReadAsync(new RegReadReq
                {
                    EngineeringHandle = _engineeringHandle,
                    Registar = registerName,

                },
                cancellationToken: cancellationToken));
            if (!result.Success)
            {
                return new(result.ErrorCode, string.Empty);
            }
            return new(result.Result?.Result ?? GrpcErrorCode.UnknownError, result.Result?.Value);
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
                _nativeEngineeringService.StartEngineeringAsync(new StartEngineeringReq
                {
                    SessionHandle = _context.SessionHandle,
                    IsOnline = _context.IsOnline,
                    PortNo = _context.PortNo,
                    IpAddress = _context.IPAddress,
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
            _nativeEngineeringService.EndEngineeringAsync(new EndEngineeringReq
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
