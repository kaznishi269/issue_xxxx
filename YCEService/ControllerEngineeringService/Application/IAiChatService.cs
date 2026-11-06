using Yaskawa.ControllerEngineeringService.AiChat.V1;

namespace Yaskawa.ControllerEngineeringService.Application
{
	public interface IAiChatService:IDisposable
	{
		//コンポーネントのオープン/クローズ
		Task<ComponentContextBase> OpenComponentAsync();
		Task<int> CloseComponentAsync(string componentHandle);

		// メッセージの送信
		Task<SendMessageRes> SendMessageAsync(string componentHandle, SendMessageReq req);

    }
}
