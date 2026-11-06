using Google.Protobuf;
using Grpc.Core;
using Yaskawa.ControllerEngineeringService.Application;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.STProgramming.V1.GrpcService
{
	/// <summary>
	/// STプログラミングサービス
	/// </summary>
	public class STProgrammingGrpcServiceImpl : STProgrammingGrpcService.STProgrammingGrpcServiceBase
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<STProgrammingGrpcServiceImpl> _logger;

		/// <summary>
		/// セッションハンドルマネージャインスタンス
		/// </summary>
		private readonly SessionHandleManager _sessionHandleManager;

		/// <summary>
		/// プログラムサービス
		/// </summary>
		private readonly IProgramService _programService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		/// <param name="sessionHandleManager">セッションハンドルマネージャインスタンス</param>
		/// <param name="programService">プログラムサービスインスタンス</param>
		public STProgrammingGrpcServiceImpl(
			ILogger<STProgrammingGrpcServiceImpl> logger, 
			SessionHandleManager sessionHandleManager, 
			IProgramService programService
			)
		{
			_logger = logger;
			_sessionHandleManager = sessionHandleManager;
			_programService = programService;
		}

		/// <summary>
		/// エンジニアリング開始
		/// </summary>
		/// <param name="req">リクエストメッセージ</param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override async Task<StartEngineeringRes> StartEngineering(StartEngineeringReq req, ServerCallContext context)
		{
			//セッションハンドルをチェックする
			CheckSessionHandle(req.SessionHandle);

			//セッション情報に登録する
			var engineeringHandle = _sessionHandleManager.RegistEngineering(req.SessionHandle, SessionHandleManager.EngineeringType.TypeSTPrograming);

			//プログラムサービスをオープンする
			var programContext = await _programService.OpenComponentAsync(req.IsOnline, req.MotionFactoryId);

			//コンテキストのキーを設定
			programContext.Key = MakeComponentSerchKey(engineeringHandle);

			//セッション情報にプログラムのコンポーネントハンドルを登録する
			_sessionHandleManager.RegistComponent(engineeringHandle, programContext);

			//プログラムをオープンする
			var resultOpen = await _programService.OpenAsync(programContext.ComponentHandle, req.PrgFile);

			//エンジニアリングハンドルを返却する
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
			//エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//プログラムのコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var programComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//プログラムサービスをクローズする
			await _programService.CloseComponentAsync(programComponentHandle);

			//コンポーネントハンドルを解放する
			_sessionHandleManager.UnregistComponent(req.EngineeringHandle, programComponentHandle);

			//エンジニアリングハンドルを解放する
			_sessionHandleManager.UnregistEngineering(req.EngineeringHandle);

			return await Task.FromResult(new EndEngineeringRes
			{
			});
		}

		/// <summary>
		/// コンポーネント検索キー作成
		/// </summary>
		/// <param name="EngineeringHandle">エンジニアリングハンドル</param>
		/// <returns>コンポーネント検索キー</returns>
		private string MakeComponentSerchKey(string EngineeringHandle)
		{
			//ユニークキーにするため、キー文字列にエンジニアリングハンドルを付加して生成
			const string programComponentKey = "ProgramComponent";
			return programComponentKey + "_" + EngineeringHandle;
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
		/// プログラムの読み込み
		/// </summary>
		/// <param name="req"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task<ReadRes> Read(ReadReq req, ServerCallContext context)
		{
			//エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//プログラムのコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var programComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//STプログラムの取得
			var progText = await _programService.ReadAsync(programComponentHandle);

			return new ReadRes
			{
				ProgramText = progText
			};
		}

		/// <summary>
		/// プログラムのコンパイル
		/// </summary>
		/// <param name="req"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async Task<CompileRes> Compile(CompileReq req, ServerCallContext context)
		{
			//エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//プログラムのコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var programComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//STプログラムのコンパイル
			var result = await _programService.CompileAsync(programComponentHandle,req.ProgramText);

			return new CompileRes
			{
			};
		}

		//プログラムプロパティ取得(試験実装)
		public override async Task<LadderProgGetPropertyRes> LadderProgGetProperty(LadderProgGetPropertyReq req, ServerCallContext context)
		{
			//エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//プログラムコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var programComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);
			//プログラムサービスよりプロパティを取得する
			var comment = await _programService.GetPropertyAsync(programComponentHandle);

			//コメントを返却する
			return await Task.FromResult(new LadderProgGetPropertyRes
			{
				ProgramComment = comment, 
			});
		}

		//試験実装
		public override async Task Chat(
			IAsyncStreamReader<ChatMessage> reqStream,
			IServerStreamWriter<ChatMessage> resStream,
			ServerCallContext context
			)
		{
			await foreach (var message in reqStream.ReadAllAsync())
			{
				await resStream.WriteAsync(
					new ChatMessage
					{
						User = "Server",
						Text = $"Echo:{message.Text}"
					});
			}
		}
	}
}
