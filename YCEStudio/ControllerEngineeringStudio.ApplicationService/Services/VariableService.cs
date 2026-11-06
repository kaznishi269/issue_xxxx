using ControllerEngineeringStudio.ApplicationService.Interfaces;
using ControllerEngineeringStudio.ApplicationService.Models;
using Microsoft.Extensions.Logging;
using Yaskawa.ControllerEngineeringService.Variable.V1;

namespace ControllerEngineeringStudio.ApplicationService.Services
{
    /// <summary>
    /// 変数サービス
    /// </summary>
    internal class VariableService : GrpcServiceBase<VariableService>, IVariableService
    {
        /// <summary>変数サービス</summary>
        private readonly VariableGrpcService.VariableGrpcServiceClient _variableService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="context">EngineeringServer情報</param>
        /// <param name="variableService">変数サービス</param>
        public VariableService(
            ILogger<VariableService> logger,
            EngineeringServerContext context,
            VariableGrpcService.VariableGrpcServiceClient variableService)
                : base(logger, context)
        {
            _variableService = variableService;
        }

        /// <inheritdoc/>
        public async Task<GrpcResult<string>> FindSymbolAsync(string symbolName, string programName)
        {
            var result = await ExecuteGrpcAsync(
              _variableService.FindSymbolRegisterAsync(new FindSymbolRegisterReq
              {
                  EngineeringHandle = _engineeringHandle,
                  IsSymbol = true,
                  IsGlobal = !string.IsNullOrEmpty(programName),
                  FindText = symbolName,
                  ProgName = programName,
              }));
            if (!result.Success)
            {
                return new(result.ErrorCode, string.Empty);
            }
            return new(result.ErrorCode, result.Result?.FindResult);
        }

        /// <inheritdoc/>
        public async Task<GrpcResult<string>> FindRegisterAsync(string registerName, string programName)
        {
            var result = await ExecuteGrpcAsync(
              _variableService.FindSymbolRegisterAsync(new FindSymbolRegisterReq
              {
                  EngineeringHandle = _engineeringHandle,
                  IsSymbol = false,
                  IsGlobal = !string.IsNullOrEmpty(programName),
                  FindText = registerName,
                  ProgName = programName,
              }));
            if (!result.Success)
            {
                return new(result.ErrorCode, string.Empty);
            }
            return new(result.ErrorCode, result.Result?.FindResult);

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
                _variableService.StartEngineeringAsync(new StartEngineeringReq
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
            _variableService.EndEngineeringAsync(new EndEngineeringReq
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
