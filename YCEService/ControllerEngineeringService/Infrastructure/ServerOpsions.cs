namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// サーバーオプション
	/// </summary>
	public class ServerOpsions
	{
		/// <summary>
		/// セッションヘルスチェック定周期時間(S)
		/// </summary>
		public int HealthCheckInterval { get; set; } = 60;
		/// <summary>
		/// プロセス自己終了機能の有効/無効
		/// </summary>
		public bool SelfShutdown { get; set; } = true;
		/// <summary>
		/// TCPポート番号(将来用)
		/// </summary>
		public int Port { get; set; } = 0;
		/// <summary>
		/// ログ出力レベル(将来用)
		/// </summary>
		public string LogLevel { get; set; } = "Info";
	}
}
