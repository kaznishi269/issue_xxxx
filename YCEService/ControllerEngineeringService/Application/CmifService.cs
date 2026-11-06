using Grpc.Core;
using Yaskawa.ControllerEngineeringService.Client.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// Cmifサービスコンテキスト
	/// </summary>
	public class CmifComponentContext : ComponentContextBase
	{
		//このサービス特有の情報を保持する場合は、ここに追加する。
		public string sss { get; set; } = "";
	}

	/// <summary>
	/// Cmifサービスクラス
	/// </summary>
	public class CmifService : ICmifService
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<ClientGrpcServiceImpl> _logger;

		/// <summary>
		/// レガシーブリッジサービスマネージャインスタンス
		/// </summary>
		private readonly ILegacyBridgeServiceManager _legacyBridgeService;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービス</param>
		/// <param name="legacyBridgeService">レガシーブリッジサービスインスタンス</param>
		public CmifService(
			ILogger<ClientGrpcServiceImpl> logger,
			ILegacyBridgeServiceManager legacyBridgeService
			)
		{
			_logger = logger;
			_legacyBridgeService = legacyBridgeService;
		}

		/// <summary>
		/// ディスポーズ
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// Cmifサービスのオープン
		/// </summary>
		/// <param name="isOnline">オンライン情報 true=オンライン false=オフライン</param>
		/// <param name="portNo">ポート番号</param>
		/// <param name="ipAddress">IPアドレス</param>
		/// <returns>コンポーネントコンテキスト</returns>
		/// <exception cref="RpcException"></exception>
		public async Task<ComponentContextBase> OpenComponentAsync(bool isOnline, int portNo, string ipAddress)
		{
			//レガシーブリッジサービスへCMIFの接続を依頼する
			var componentHandle = await _legacyBridgeService.OpenComponentCmifAsync(
				isOnline,
				portNo,
				ipAddress
				);

			if (componentHandle.Length == 0)
			{
				//ポートオープン失敗例外
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.PortOpenFailed)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.InvalidArgument,
						""),
					trailers);
			}

			return await Task.FromResult(new ComponentContextBase
			{
				Type = ComponentType.TypeCmif,
				ComponentHandle = componentHandle,
			});
		}

		/// <summary>
		/// Cmifサービスのクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>エラーコード</returns>
		/// <exception cref="RpcException">
		/// PortCloseFailed:ポートクローズ失敗
		/// </exception>
		public async Task<int> CloseComponentAsync(string componentHandle)
		{
			//レガシーブリッジサービスへCMIFのクローズを依頼する
			var resultClose = await _legacyBridgeService.CloseComponentCmifAsync(componentHandle);
			if (resultClose!= 0)
			{
				//ポートクローズ失敗例外
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.PortCloseFailed)).ToString()}
				};
				throw new RpcException(
					new Status(
						StatusCode.InvalidArgument,
						""),
					trailers);
			}

			return (int)ErrorCode.Normal;
		}

		//レジスタ読み込み（試験実装）
		public async Task<(int, string)> RegReadAsync(string componentHandle, string register)
		{
			//レガシーブリッジサービスマネージャへレジスタリードを要求する
			var res = await _legacyBridgeService.RegReadAsync(componentHandle, register);

			return res;
		}
	}
}
