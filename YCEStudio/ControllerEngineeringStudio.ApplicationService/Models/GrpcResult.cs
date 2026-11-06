using ControllerEngineeringStudio.ApplicationService.Constants;

namespace ControllerEngineeringStudio.ApplicationService.Models
{
    /// <summary>
    /// gRPC結果
    /// </summary>
    /// <typeparam name="T">結果の型</typeparam>
    /// <param name="ErrorCode">エラーコード</param>
    /// <param name="Result">結果</param>
    public record class GrpcResult<T>(
        int ErrorCode,
        T? Result)
    {
        /// <summary>
        /// 成功したか
        /// </summary>
        public bool Success => ErrorCode == GrpcErrorCode.NoError;
    }
}
