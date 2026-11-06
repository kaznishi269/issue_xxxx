using Yaskawa.ControllerEngineeringService.Infrastructure;
using Yaskawa.ControllerEngineeringService.Common;
using Grpc.Core;

namespace Yaskawa.ControllerEngineeringService.Client.V1.GrpcService
{
	/// <summary>
	/// クライアントサービス
	/// </summary>
	public class ClientGrpcServiceImpl : ClientGrpcService.ClientGrpcServiceBase
	{
		//
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<ClientGrpcServiceImpl> _logger;
		/// <summary>
		/// セッションハンドルマネージャインスタンス
		/// </summary>
		private readonly SessionHandleManager _sessionHandleManager;
		/// <summary>
		/// レガシーブリッジマネージャインスタンス
		/// </summary>
		private readonly ILegacyBridgeServiceManager _legacyBridgeServiceManager;
		/// <summary>
		/// 起動オプションインスタンス
		/// </summary>
		private volatile ServerOpsions _options;
		/// <summary>
		/// レガシーブリッジハートビートサービスインスタンス
		/// </summary>
		private readonly LegacyBridgeServiceHeartBeatService _legacyBridgeServiceHeartBeatService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		/// <param name="sessionHandleManager">セッションハンドルマネージャインスタンス</param>
		/// <param name="legacyBridgeServiceManager">レガシーブリッジサービスマネージャインスタンス</param>
		/// <param name="options">オプションズインスタンス</param>
		/// <param name="legacyBridgeServiceHeartBeatService">レガシーブリッジハートビードサービスインスタンス</param>
		public ClientGrpcServiceImpl(
			ILogger<ClientGrpcServiceImpl> logger,
			SessionHandleManager sessionHandleManager,
			ILegacyBridgeServiceManager legacyBridgeServiceManager,
			ServerOpsions options,
			LegacyBridgeServiceHeartBeatService  legacyBridgeServiceHeartBeatService
			)
		{
			_logger = logger;
			_sessionHandleManager = sessionHandleManager;
			_legacyBridgeServiceManager = legacyBridgeServiceManager;
			_options = options;
			_legacyBridgeServiceHeartBeatService = legacyBridgeServiceHeartBeatService;
		}

		/// <summary>
		/// クライアント追加
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		void
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		string sessionHandle:セッションハンドル
		///		int32 healthCheckInterval :ヘルスチェック周期(s)
		/// </returns>
		public override async Task<AddClientRes> AddClient(AddClientReq req, ServerCallContext context)
		{
			//クライアントを登録する
			string resultSessionHandle = await _sessionHandleManager.RegistClientAsync(req.LegacyProgrammerProcessId);

			//登録情報を返却する
			return await Task.FromResult(new AddClientRes
			{
				//セッションハンドル
				SessionHandle = resultSessionHandle,
				//ヘルスチェック周期(s)
				HealthCheckInterval = _options.HealthCheckInterval
			});
		}


		/// <summary>
		/// クライアント削除
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		string sessionHandle:セッションハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		void
		/// </returns>
		/// <exception cref="RpcException">
		///		SessionHandleNotFound:セッションハンドルが存在しない
		/// </exception>
		public override async Task<RemoveClientRes> RemoveClient(RemoveClientReq req, ServerCallContext context)
		{
			//セッションハンドルをチェックする
			CheckSessionHandle(req.SessionHandle);

			//セッションハンドルを解放する
			//セッションハンドルは、事前にチェックしているので、メソッドの返却値は見ない
			await _sessionHandleManager.ReleaseClientAsync(req.SessionHandle);

			return await Task.FromResult(new RemoveClientRes
			{
			});
		}

		/// <summary>
		/// クライアント生存確認
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		string sessionHandle:セッションハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		void
		/// </returns>
		public override Task<HeartbeatRes> Heartbeat(HeartbeatReq req, ServerCallContext context)
		{
			//セッションハンドルをチェックする
			CheckSessionHandle(req.SessionHandle);

			//指定されたセッションハンドルの最終アクセス時間を更新する。
			//セッションハンドルは、事前にチェックしているので、メソッドの返却値は見ない
			_sessionHandleManager.UpdateLastAccessTime(req.SessionHandle);

			return Task.FromResult(new HeartbeatRes
			{
				//サーバーステータスを返却する
				ServerStatus = _legacyBridgeServiceHeartBeatService.GetServerStatus()
			});
		}

		/// <summary>
		/// セッションハンドルのチェック
		/// </summary>
		/// <param name="sessionHandle">セッションハンドル</param>
		/// <returns>
		///		true:セッションは正常
		///		false:セッションは異常(例外をスローするので、実際には返却されない)
		/// </returns>
		/// <exception cref="RpcException">
		///		SessionHandleEmpty:セッションハンドルが指定されていない
		///		SessionHandleNotFound:セッションハンドルが存在しない
		/// </exception>
		private bool CheckSessionHandle(string sessionHandle)
		{
			//セッションハンドルが指定されているか？
			if (string.IsNullOrEmpty(sessionHandle))
			{
				//セッションハンドルが指定されていない
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.SessionHandleEmpty)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.InvalidArgument,
						""),
					trailers);
			}
			if (_sessionHandleManager.IsSessionHandleExist(sessionHandle) == false)
			{
				//該当するセッションハンドルが存在しないの例外をスローする
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.SessionHandleNotFound)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.NotFound,
						""),
					trailers);
			}

			return true;
		}
	}
}
