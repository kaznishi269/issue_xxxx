using Grpc.Core;
using Yaskawa.ControllerLegacyBridgeService.Infrastructure;

namespace Yaskawa.ControllerLegacyBridgeService.Server.V1.GrpcService
{
	/// <summary>
	/// サーバーサービス
	/// </summary>
	public class LBServerGrpcServiceImpl : LBServerGrpcService.LBServerGrpcServiceBase
	{
		//ロガーインスタンス
		private readonly ILogger<LBServerGrpcServiceImpl> _logger;
		//コンポーネントマネージャ
		private readonly ComponentManager _componentManager;
		//自己終了監視
		private readonly SelfTerminationService _selfTerminationService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーインスタンス</param>
		/// <param name="componentManager">コンポーネントマネージャインスタンス</param>
		/// <param name="selfTerminationService">自己プロセス終了監視インスタンス</param>
		public LBServerGrpcServiceImpl(ILogger<LBServerGrpcServiceImpl> logger, ComponentManager componentManager, SelfTerminationService selfTerminationService)
		{
			_logger = logger;
			_componentManager = componentManager;
			_selfTerminationService = selfTerminationService;
		}

		/// <summary>
		/// バージョン情報取得
		/// </summary>
		/// <remarks>
		/// このgRPCサーバーのバージョン情報を返却する
		/// </remarks>
		/// <param name="req">
		/// リクエストメッセージ
		///		void
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		string version:バージョン情報
		/// </returns>
		public override Task<LBGetVersionRes> GetVersion(LBGetVersonReq req, ServerCallContext context)
		{
			return Task.FromResult(
				new LBGetVersionRes
				{
					Version = "1.0.0"
				});
		}

		/// <summary>
		/// クライアント生存確認
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		void
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		void
		/// </returns>
		public override Task<HeartbeatRes> Heartbeat(HeartbeatReq req, ServerCallContext context)
		{
			//最終アクセス時間を更新する
			_selfTerminationService.Touch();
			return Task.FromResult(new HeartbeatRes
			{
			});
		}
	}
}
