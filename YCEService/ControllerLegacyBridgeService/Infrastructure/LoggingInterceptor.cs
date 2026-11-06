using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Yaskawa.ControllerLegacyBridgeService.Infrastructure
{
	/// <summary>
	/// ロギングインターセプター
	/// </summary>
	/// <remarks>
	/// サービスメソッドコール時のメソッド名をロギングする
	/// </remarks>
	public class LoggingInterceptor : Interceptor
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<LoggingInterceptor> _logger;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// サービスメソッド名のロギング
		/// </summary>
		/// <remarks>
		/// サービスメソッドコールをインターセプトして
		/// サービスメソッド名をロギングする
		/// </remarks>
		/// <typeparam name="TRequest"></typeparam>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="request">クライアントからのリクエストメッセージ</param>
		/// <param name="context">サーバー側呼び出し情報</param>
		/// <param name="continuation">実際のサービス処理実行のデリゲート</param>
		/// <returns>RPC サービスメソッドが返却するレスポンスメッセージ</returns>
		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
			TRequest request,
			ServerCallContext context,
			UnaryServerMethod<TRequest, TResponse> continuation)
		{
			//サービスメソッド名をロギングする
			_logger.LogInformation("Method={Method}", context.Method);

			//実際のRPCメソッドを呼び出すデリゲート
			return await continuation(request, context);
		}
	}
}