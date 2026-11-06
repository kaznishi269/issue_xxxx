using Grpc.Core;
using Yaskawa.ControllerEngineeringService.Application;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.NativeEnginnering.V1.GrpcService
{
	/// <summary>
	/// ネイティブエンジニアリングサービス
	/// </summary>
	public class NativeEngineeringGrpcServiceImpl : NativeEngineeringGrpcService.NativeEngineeringGrpcServiceBase
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<NativeEngineeringGrpcServiceImpl> _logger;

		/// <summary>
		/// セッションハンドルマネージャインスタンス
		/// </summary>
		private readonly SessionHandleManager _sessionHandleManager;

		/// <summary>
		/// Cmifサービス
		/// </summary>
		private readonly ICmifService _cmifService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		/// <param name="sessionHandleManager">セッションハンドルマネージャインスタンス</param>
		/// <param name="cmifService">Cmifサービスインスタンス</param>
		public NativeEngineeringGrpcServiceImpl(ILogger<NativeEngineeringGrpcServiceImpl> logger, SessionHandleManager sessionHandleManager, ICmifService cmifService)
		{
			_logger = logger;
			_sessionHandleManager = sessionHandleManager;
			_cmifService = cmifService;
		}

		/// <summary>
		/// エンジニアリング開始
		/// </summary>
		/// <param name="req">リクエストメッセージ</param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		/// </returns>
		public override async Task<StartEngineeringRes> StartEngineering(StartEngineeringReq req, ServerCallContext context)
		{
			//セッションハンドルをチェックする
			CheckSessionHandle(req.SessionHandle);

			//セッション情報に登録する
			var engineeringHandle = _sessionHandleManager.RegistEngineering(req.SessionHandle, SessionHandleManager.EngineeringType.TypeNative);

			//Cmifサービスをオープンする
			var cmifContext = await _cmifService.OpenComponentAsync(req.IsOnline, req.PortNo, req.IpAddress);

			//コンテキストのキーを設定
			cmifContext.Key = MakeCmifComponentSerchKey(engineeringHandle);

			//セッション情報にCMIFのコンポーネントハンドルを登録する
			_sessionHandleManager.RegistComponent(engineeringHandle, cmifContext);

			//エンジニアリングハンドルを返却する
			return await Task.FromResult(new StartEngineeringRes
			{
				//コンポーネントハンドルをエンジニアリングハンドルとして返却する
				EngineeringHandle = engineeringHandle,
			});
		}

		/// <summary>
		/// エンジニアリング終了
		/// </summary>
		/// <param name="req">リクエストメッセージ</param>
		/// <param name="context"></param>
		/// <returns>レスポンスメッセージ</returns>
		public override async Task<EndEngineeringRes> EndEngineering(EndEngineeringReq req, ServerCallContext context)
		{
			//エンジニアリング/コンポーネントハンドルのチェック
			CheckEngineeringHandle(req.EngineeringHandle);

			//Cmifのコンポーネントハンドルを取得する
			var key = MakeCmifComponentSerchKey(req.EngineeringHandle);
			var cmifComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);

			//Cmifサービスをクローズする
			await _cmifService.CloseComponentAsync(cmifComponentHandle);

			//エンジニアリングハンドルを解放する
			_sessionHandleManager.UnregistEngineering(req.EngineeringHandle);

			return await Task.FromResult(new EndEngineeringRes
			{
			});

		}
		private string MakeCmifComponentSerchKey(string EngineeringHandle)
		{
			const string cmifComponentKey = "CmifComponent";

			return cmifComponentKey + "_" + EngineeringHandle;
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

		//レジスタリード（試験実装）
		public override async Task<RegReadRes> RegRead(RegReadReq req, ServerCallContext context)
		{
			//cmifコンポーネントハンドルを取得する
			var key = MakeCmifComponentSerchKey(req.EngineeringHandle);
			var cmifComponentHandle = _sessionHandleManager.GetComponentHandle(req.EngineeringHandle, key);
			//Cmifサービスへレジスタリードを要求する
			var res = await _cmifService.RegReadAsync(cmifComponentHandle, req.Registar);

			//返却する
			return await Task.FromResult(new RegReadRes
			{
				Result = res.Item1,
				Value = res.Item2
			});
		}
	}
}
