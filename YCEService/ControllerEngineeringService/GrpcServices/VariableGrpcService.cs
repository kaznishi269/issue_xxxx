using Grpc.Core;
using Yaskawa.ControllerEngineeringService.Application;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.Variable.V1.GrpcService
{
	/// <summary>
	/// 変数サービス
	/// </summary>
	public class VariableGrpcServiceImpl : VariableGrpcService.VariableGrpcServiceBase
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<VariableGrpcServiceImpl> _logger;

		/// <summary>
		/// セッションハンドルマネージャインスタンス
		/// </summary>
		private readonly SessionHandleManager _sessionHandleManager;

		/// <summary>
		/// 変数サービス
		/// </summary>
		private readonly IVariableService _variableService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public VariableGrpcServiceImpl(
			ILogger<VariableGrpcServiceImpl> logger,
			SessionHandleManager sessionHandleManager, 
			IVariableService variableService
			)
		{
			_logger = logger;
			_sessionHandleManager = sessionHandleManager;
			_variableService = variableService;
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
			var engineeringHandle = _sessionHandleManager.RegistEngineering(req.SessionHandle, SessionHandleManager.EngineeringType.TypeVariable);

			//変数サービスをオープンする
			var variableContext = await _variableService.OpenComponentAsync();

			//コンテキストのキーを設定
			variableContext.Key = MakeComponentSerchKey(engineeringHandle);

			//セッション情報にプログラムのコンポーネントハンドルを登録する
			_sessionHandleManager.RegistComponent(engineeringHandle, variableContext);

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

			//変数のコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var variableComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//変数サービスをクローズする
			await _variableService.CloseComponentAsync(variableComponentHandle);

			//コンポーネントハンドルを解放する
			_sessionHandleManager.UnregistComponent(req.EngineeringHandle, variableComponentHandle);

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
			const string programComponentKey = "VariableComponent";
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
		/// 変数/レジスタ検索
		/// </summary>
		/// <param name="req">リクエストメッセージ</param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override async Task<FindSymbolRegisterRes> FindSymbolRegister(FindSymbolRegisterReq req, ServerCallContext context)
		{
			//エンジニアリングハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//プログラムのコンポーネントハンドルを取得する
			var key = MakeComponentSerchKey(req.EngineeringHandle);
			var programComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//変数サービスに変数/レジスタ検索を依頼する
			var result = await _variableService.FindSymbolRegisterAsync(programComponentHandle, req.IsSymbol,req.IsGlobal,req.FindText,req.ProgName);

			//検索結果を返却する
			return new FindSymbolRegisterRes
			{
				FindResult = result
			};
		}
	}
}
