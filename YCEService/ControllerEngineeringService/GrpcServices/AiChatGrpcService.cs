using Google.Protobuf;
using Grpc.Core;
using Yaskawa.ControllerEngineeringService.Application;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.AiChat.V1.GrpcService
{
	/// <summary>
	/// AIチャットサービス
	/// </summary>
	public class AiChatGrpcServiceImpl : AiChatGrpcService.AiChatGrpcServiceBase
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<AiChatGrpcServiceImpl> _logger;

		/// <summary>
		/// セッションハンドルマネージャインスタンス
		/// </summary>
		private readonly SessionHandleManager _sessionHandleManager;

		/// <summary>
		/// AIチャットサービス
		/// </summary>
		private readonly IAiChatService _aiChatService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		/// <param name="sessionHandleManager">セッションハンドルマネージャインスタンス</param>
		/// <param name="aiChatService">AIチャットサービスインスタンス</param>
		public AiChatGrpcServiceImpl(
			ILogger<AiChatGrpcServiceImpl> logger, 
			SessionHandleManager sessionHandleManager, 
			IAiChatService aiChatService
			)
		{
			_logger = logger;
			_sessionHandleManager = sessionHandleManager;
			_aiChatService = aiChatService;
		}

		/// <summary>
		/// エンジニアリング開始
		/// </summary>
		/// <param name="req">リクエストメッセージ</param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override async Task<StartEngineeringRes> StartEngineering(StartEngineeringReq req, ServerCallContext context)
		{
			// セッションハンドルをチェックする
			CheckSessionHandle(req.SessionHandle);

			// セッション情報に登録する
			var engineeringHandle = _sessionHandleManager.RegistEngineering(req.SessionHandle, SessionHandleManager.EngineeringType.TypeAiChat);

			// AIチャットサービスをオープンする
			var aiChatContext = await _aiChatService.OpenComponentAsync();

            // コンテキストのキーを設定
            aiChatContext.Key = MakeComponentSerchKey(engineeringHandle);

			// セッション情報にAIチャットのコンポーネントハンドルを登録する
			_sessionHandleManager.RegistComponent(engineeringHandle, aiChatContext);

			// エンジニアリングハンドルを返却する
			return await Task.FromResult(new StartEngineeringRes
			{
				EngineeringHandle = engineeringHandle,
			});
		}

		/// <summary>
		/// エンジニアリング終了
		/// </summary>
		/// <param name="req">リクエストメッセージ</param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override async Task<EndEngineeringRes> EndEngineering(EndEngineeringReq req, ServerCallContext context)
		{
			// エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			// AIチャットのコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var aiChatComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

            // AIチャットサービスをクローズする
            await _aiChatService.CloseComponentAsync(aiChatComponentHandle);

			// コンポーネントハンドルを解放する
			_sessionHandleManager.UnregistComponent(req.EngineeringHandle, aiChatComponentHandle);

			// エンジニアリングハンドルを解放する
			_sessionHandleManager.UnregistEngineering(req.EngineeringHandle);

			return await Task.FromResult(new EndEngineeringRes
			{
			});
		}

        /// <summary>
        /// コンポーネント検索キー作成
        /// </summary>
        /// <param name="engineeringHandle">エンジニアリングハンドル</param>
        /// <returns>コンポーネント検索キー</returns>
        private string MakeComponentSerchKey(string engineeringHandle)
		{
			//ユニークキーにするため、キー文字列にエンジニアリングハンドルを付加して生成
			const string aiChatComponentKey = "AiChatComponent";
			return aiChatComponentKey + "_" + engineeringHandle;
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

		/// <summary>
		/// エンジニアリングハンドルのチェック
		/// </summary>
		/// <param name="engineeringHandle">エンジニアリングハンドル</param>
		/// <returns>
		///		true=ハンドルが登録されている
		///		false=ハンドルが登録されていない(例外をスローするのでこの返却値になることはない)
		/// </returns>
		/// <exception cref="RpcException"></exception>
		private bool CheckEngineeringHandle(string engineeringHandle)
		{
			//エンジニアリングハンドルが指定されているか？
			if (string.IsNullOrEmpty(engineeringHandle))
			{
				//エンジニアリングハンドルが指定されていない
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.EngineeringHandleEmpty)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.InvalidArgument,
						""),
					trailers);
			}

			//エンジニアリングハンドルが登録されているか?
			if (_sessionHandleManager.IsEngineeringHandleExist(engineeringHandle) == false)
			{
				//該当するエンジニアリングハンドルが存在しない
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.EngineeringNotRegistered)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.NotFound,
						""),
					trailers);
			}

			return true;
		}

		/// <summary>
		/// メッセージ送信
		/// </summary>
		/// <param name="req"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task<SendMessageRes> SendMessage(SendMessageReq req, ServerCallContext context)
		{
			//エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//AIチャットのコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var aiChatComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//AIチャットへメッセージ送信
			return await _aiChatService.SendMessageAsync(aiChatComponentHandle, req);
		}
	}
}
