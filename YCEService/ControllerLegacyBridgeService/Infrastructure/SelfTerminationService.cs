using Yaskawa.ControllerLegacyBridgeService.CimScopeWrapper.V1.GrpcService;

namespace Yaskawa.ControllerLegacyBridgeService.Infrastructure
{
	/// <summary>
	/// 自己プロセス終了監視サービス
	/// </summary>
	public class SelfTerminationService : BackgroundService
	{
		/// <summary>
		/// ライフタイムインスタンス
		/// </summary>
		private IHostApplicationLifetime _lifetime;
		/// <summary>
		/// 最終アクセス時間インスタンス
		/// </summary>
		private DateTime _lastAccessTime = DateTime.UtcNow;
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<CimScopeWrapperGrpcServiceImpl> _logger;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		/// <param name="lifetime">ライフタイムインスタンス</param>
		public SelfTerminationService(ILogger<CimScopeWrapperGrpcServiceImpl> logger,IHostApplicationLifetime lifetime)
		{
			_logger = logger;
			_lifetime = lifetime;
		}

		//最終アクセス時間更新
		/// <summary>
		/// 最終アクセス時間更新
		/// </summary>
		public void Touch()
		{
			//最終アクセス時間を更新する
			_lastAccessTime = DateTime.UtcNow;
		}

		/// <summary>
		/// 自己プロセス終了監視定周期処理
		/// </summary>
		/// <param name="stoppingToken">キャンセル要求監視トークン</param>
		/// <returns>なし</returns>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			int counter = 0;
			while (!stoppingToken.IsCancellationRequested)
			{
				//30秒周期で自プロセスを終了するか判定する
				await Task.Delay(TimeSpan.FromSeconds(30),stoppingToken);

				//コントローラエンジニアリングサービルからの最終アクセス時間をチェックし、
				//一定時間アクセスが無い場合は、自身のプロセスを終了する。
				TimeSpan spn = DateTime.UtcNow - _lastAccessTime;
				//_logger.LogInformation("TimeSpn={TimeSpn}", spn.TotalSeconds.ToString());
				if (DateTime.UtcNow - _lastAccessTime > TimeSpan.FromSeconds(30))
				{
					//カウンターを加算して比較する
					if (++counter >= 2)
					{
						//自プロセスを終了する
						_logger.LogInformation("Terminate the current process");
						_lifetime.StopApplication();
						break;
					}
				}
				else
				{
					//カウンターをリセットしておく
					counter = 0;
				}
			}
		}
	}
}
