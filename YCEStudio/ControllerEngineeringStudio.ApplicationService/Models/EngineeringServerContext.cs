namespace ControllerEngineeringStudio.ApplicationService.Models
{
    /// <summary>
    /// EngineeringServer情報
    /// </summary>
    internal record class EngineeringServerContext
    {
        /// <summary>
        /// セッションハンドル
        /// </summary>
        public string SessionHandle { get; set; } = string.Empty;

        /// <summary>
        /// 接続済みか？
        /// </summary>
        public bool IsConnected => !string.IsNullOrEmpty(SessionHandle);

        /// <summary>
        /// オンラインか？
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// ポートNo
        /// </summary>
        public int PortNo { get; set; }

        /// <summary>
        /// IPアドレス
        /// </summary>
        public string IPAddress { get; set; } = string.Empty;

        /// <summary>
        /// モーションファクトリID
        /// </summary>
        public int MotionFactoryId { get; set; }

        /// <summary>
        /// FWXIDEプロセスID
        /// </summary>
        public int LegacyProgrammerProcessId { get; set; }
    }
}
