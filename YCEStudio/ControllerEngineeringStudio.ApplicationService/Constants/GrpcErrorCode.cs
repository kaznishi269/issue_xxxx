namespace ControllerEngineeringStudio.ApplicationService.Constants
{
    /// <summary>
    /// gRPCエラーコード
    /// </summary>
    public static class GrpcErrorCode
    {
        /// <summary>
        /// キャンセル
        /// </summary>
        public const int Cancel = -2;

        /// <summary>
        /// 不明なエラー
        /// </summary>
        public const int UnknownError = -1;

        /// <summary>
        /// エラーなし
        /// </summary>
        public const int NoError = 0;
    }
}
