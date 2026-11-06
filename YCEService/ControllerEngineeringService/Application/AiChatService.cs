using Yaskawa.ControllerEngineeringService.AiChat.V1;
using Yaskawa.ControllerEngineeringService.Application;
using Yaskawa.ControllerEngineeringService.Client.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Common;

namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// AIチャットサービスのコンポーネントコンテキスト
	/// </summary>
	public class AiComponentContext: ComponentContextBase
	{
		//このサービス特有の情報を保持する場合は、ここに追加する。
		public string sss { get; set; } = "";
	}

	/// <summary>
	/// AIチャットサービスクラス
	/// </summary>
	public class AiChatService : IAiChatService
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<AiChatService> _logger;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービス</param>
		public AiChatService(
			ILogger<AiChatService> logger
			)
		{
			_logger = logger;
		}

		/// <summary>
		/// ディスポーズ
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// AIチャットサービスのオープン
		/// </summary>
		/// <returns>AIサービスコンテキスト</returns>
		public async Task<ComponentContextBase> OpenComponentAsync()
		{
			string componentHandle = $"C-{Guid.NewGuid().ToString("N")}";

			return await Task.FromResult(new AiComponentContext()
			{
				Type = ComponentType.TypeAi,
				ComponentHandle = componentHandle
			});
		}

		/// <summary>
		/// AIチャットサービスのクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>リターンステータス</returns>
		public async Task<int> CloseComponentAsync(string componentHandle)
		{
			return await Task.FromResult((int)ErrorCode.Normal);
		}

		/// <summary>
		/// AIチャットのメッセージ送信
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <param name="req">送信メッセージ</param>
		/// <returns>応答メッセージ</returns>
        public async Task<SendMessageRes> SendMessageAsync(string componentHandle, SendMessageReq req)
        {
            // TODO：2026/11/04 (YE)Yuta Hatanaka AIサービスへメッセージ要求/応答
            return new SendMessageRes
            {
				Message = $"{req.Message}の応答",
            };
        }
    }
}
