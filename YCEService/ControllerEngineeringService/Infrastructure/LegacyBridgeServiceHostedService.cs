using Yaskawa.ControllerEngineeringService.Client.V1;

namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// レガシーブリッジサービス管理クラス
	/// </summary>
	public class LegacyBridgeServiceHostedService :IHostedService
	{
		/// <summary>
		/// レガシーブリッジサービスマネージャ
		/// </summary>
		private readonly ILegacyBridgeServiceManager _legacyBridgeServiceManager;

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="legacyBridgeServiceManager">レガシーブリッジサービスマネージャインスタンス</param>
		public LegacyBridgeServiceHostedService(ILegacyBridgeServiceManager legacyBridgeServiceManager)
		{
			_legacyBridgeServiceManager = legacyBridgeServiceManager;
		}
		#endregion

		/// <summary>
		/// プロセス起動
		/// </summary>
		/// <param name="cancellationToken">キャンセル検出するためのトークン</param>
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await _legacyBridgeServiceManager.StartAsync();
		}

		/// <summary>
		/// プロセス終了
		/// </summary>
		/// <param name="cancellationToken">キャンセル検出するためのトークン</param>
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await _legacyBridgeServiceManager.StopAsync();
		}
	}

	/// <summary>
	/// レガシーブリッジサービスの定周期確認
	/// </summary>
	public class LegacyBridgeServiceHeartBeatService : BackgroundService
	{
		/// <summary>
		/// レガシーブリッジサービスマネージャ
		/// </summary>
		private readonly ILegacyBridgeServiceManager _legacyBridgeServiceManager;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="legacyBridgeServiceManager">レガシーブリッジサービスマネージャインスタンス</param>
		public LegacyBridgeServiceHeartBeatService(ILegacyBridgeServiceManager legacyBridgeServiceManager)
		{
			_legacyBridgeServiceManager = legacyBridgeServiceManager;
		}

		/// <summary>
		/// サーバーステータス
		/// </summary>
		private ServerStatus _serverStatus = ServerStatus.Unknown;

		/// <summary>
		/// 定周期処理
		/// </summary>
		/// <param name="stoppingToken">キャンセル検出するためのトークン</param>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					//レガシーブリッジサービスへハートビードを送信する
					_legacyBridgeServiceManager.ServerHeartBeat();
				}
				catch
				{
					//レガシーブリッジサービスから応答がない
					_serverStatus = ServerStatus.LegacyBridgeServiceDisconnected;
				}

				//レガシーブリッジサーバーが30秒周期の監視なので、少し早く回る（20秒）
				await Task.Delay(
					TimeSpan.FromSeconds(20),
					stoppingToken);
			}
		}

		/// <summary>
		/// サーバーステータスの取得
		/// </summary>
		/// <returns>サーバーステータス</returns>
		public ServerStatus GetServerStatus()
		{
			//サーバーステータスを返却する
			return _serverStatus;
		}
	}
}
