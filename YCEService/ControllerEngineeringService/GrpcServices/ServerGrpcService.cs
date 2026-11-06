using Grpc.Core;

namespace Yaskawa.ControllerEngineeringService.Server.V1.GrpcService
{
	/// <summary>
	/// サーバーサービス
	/// </summary>
	public class ServerGrpcServiceImpl : ServerGrpcService.ServerGrpcServiceBase
	{
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ServerGrpcServiceImpl()
		{
		}

		/// <summary>
		/// バージョン情報取得
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		string version:バージョン情報
		/// </returns>
		public override Task<GetVersionRes> GetVersion(GetVersonReq req, ServerCallContext context)
		{
			//バージョン情報を返却する
			return Task.FromResult(
				new GetVersionRes
				{
					Version = "1.0.0"
				});
		}

		/// <summary>
		/// インフォメーションの取得
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		string information:インフォメーション
		/// </returns>
		public override Task<GetInformationRes> GetInformation(GetInformationReq req, ServerCallContext context)
		{
			//インフォメーションを返却する
			return Task.FromResult(
				new GetInformationRes
				{
					Information = ""
				});
		}
	}
}
