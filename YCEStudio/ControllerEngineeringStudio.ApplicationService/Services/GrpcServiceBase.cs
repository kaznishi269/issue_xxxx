using ControllerEngineeringStudio.ApplicationService.Constants;
using ControllerEngineeringStudio.ApplicationService.Models;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace ControllerEngineeringStudio.ApplicationService.Services
{
    /// <summary>
    /// gRPCService基底クラス
    /// </summary>
    /// <typeparam name="T">継承先クラス</typeparam>
    internal abstract class GrpcServiceBase<TClass>
    {
        #region Fields
        /// <summary>ロガー</summary>
        protected readonly ILogger<TClass> _logger;

        /// <summary>EngineeringServer情報</summary>
        protected readonly EngineeringServerContext _context;

        /// <summary>Engineeringハンドル</summary>
        protected string _engineeringHandle = string.Empty;
        #endregion

        /// <summary>
        /// エンジニアリング中か？
        /// </summary>
        protected bool IsEngineering => !string.IsNullOrEmpty(_engineeringHandle);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="context">EngineeringServer情報</param>
        protected GrpcServiceBase(ILogger<TClass> logger, EngineeringServerContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// gRPCサービスの実行用関数
        /// </summary>
        /// <param name="call">実行するgRPCサービス</param>
        /// <returns>GrpcResult</returns>
        protected async Task<GrpcResult<TResult>> ExecuteGrpcAsync<TResult>(AsyncUnaryCall<TResult> call)
        {
            try
            {
                var res = await call.ResponseAsync;
                return new GrpcResult<TResult>(GrpcErrorCode.NoError, res);
            }
            catch (RpcException e)
            {
                if (e.StatusCode == StatusCode.Cancelled)
                {
                    _logger.LogDebug(e, "キャンセル");
                    return new GrpcResult<TResult>(GrpcErrorCode.Cancel, default);
                }

                var codestring = e.Trailers.FirstOrDefault(t => t.Key == "error-code")?.Value;
                if (string.IsNullOrEmpty(codestring))
                {
                    _logger.LogError(e, "想定外のエラー");
                    return new GrpcResult<TResult>(GrpcErrorCode.UnknownError, default);
                }
                if (int.TryParse(codestring, out int errorCode))
                {
                    _logger.LogWarning(e, "想定内のエラー:{ErrorCode}", errorCode);
                    return new GrpcResult<TResult>(errorCode, default);
                }
                else
                {
                    _logger.LogError(e, "errorCode変換失敗");
                    return new GrpcResult<TResult>(GrpcErrorCode.UnknownError, default);
                }
            }
        }
    }
}
