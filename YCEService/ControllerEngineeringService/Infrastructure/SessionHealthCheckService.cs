namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// セッションヘルスチェックバックグラウンドサービス
	/// </summary>
	public sealed class SessionHealthCheckService : BackgroundService
	{
		/// <summary>
		/// セッションハンドルマネージャインスタンス
		/// </summary>
		private readonly SessionHandleManager _sessionHandlemanager;
		/// <summary>
		/// 初期化終了フラグ
		/// </summary>
		private volatile bool _started = false;
		/// <summary>
		/// 起動オプションインスタンス
		/// </summary>
		private volatile ServerOpsions _options;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="manager">セッションハンドルマネージャインスタンス</param>
		/// <param name="lifetime">ライフタイムインスタンス</param>
		/// <param name="options">起動オプションインスタンス</param>
		public SessionHealthCheckService(SessionHandleManager manager,IHostApplicationLifetime lifetime, ServerOpsions options)
		{
			_sessionHandlemanager = manager;
			_options = options;

			//サーバーが利用可能になれば_startedがtrueとなる。
			lifetime.ApplicationStarted.Register(() =>
			{
				_started = true;
			});
		}

		/// <summary>
		/// セッションヘルスチェック定周期処理
		/// </summary>
		/// <param name="stoppingToken">キャンセル要求監視トークン</param>
		/// <returns>なし</returns>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				//ヘルスチェックインターバル値を取得
				var helthCheckInterval = _options.HealthCheckInterval;
				if (helthCheckInterval == 0)
				{
					//ヘルスチェックインターバル時間が０なので、セッションヘルスチェックは行わない
					return;
				}

				while (!stoppingToken.IsCancellationRequested)
				{
					//サーバーが利用可能であればば、セッションハンドルのチェックを行う
					if (_started)
					{
							_sessionHandlemanager.Cleanup();
					}

					//指定された秒の周期でハンドルをチェックする
					await Task.Delay(
						TimeSpan.FromSeconds(helthCheckInterval),
						stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}
	}
}
