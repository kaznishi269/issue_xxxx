using Google.Protobuf;
using Yaskawa.ControllerEngineeringService.Common;

namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// レガシーブリッジサービスのインターフェースクラス
	/// </summary>
	public interface ILegacyBridgeServiceManager : IDisposable
	{
		#region 共通部
		bool IsConnected { get; }
		Task StartAsync();
		Task StopAsync();
		#endregion

		#region サーバーサービス
		void ServerHeartBeat();
		#endregion

		#region CimScopeラッパーサービス
		/// <summary>
		/// CimScopeラッパー用リターンステータス
		/// </summary>
		public class CimScopeResult
		{
			public ErrorCode result;	//エラーコード
			public int cimScopeHResult; //CimScope例外コード(エラーコードがCimScopeExceptionの時に格納される)
		}
		Task<(CimScopeResult, string)> OpenComponentLadderComAsync(bool isOnline,int motionFactoryId);
		Task<CimScopeResult> CloseComponentLadderComAsync(string componentHandle);
		Task<CimScopeResult> LadderOpenAsync(string componentHandle, string progName);
		Task<CimScopeResult> LadderCloseAsync(string componentHandle);
		Task<(CimScopeResult,string)> LadderProgGetPropertyAsync(string componentHandle);
		Task<(CimScopeResult, ByteString)> LadderProgReadProgramDataPacketAsync(string componentHandle, short packetId, short packetNumber);
		Task<CimScopeResult> LadderProgWriteProgramDataPacketAsync(string componentHandle, ByteString data);
		Task<CimScopeResult> LadderProgSaveAsync(string componentHandle);
		#endregion

		#region Cmifラッパーサービス
		//CmifService
		Task<string> OpenComponentCmifAsync( bool isOnline, int portNo,string ipAddress);
		Task<int> CloseComponentCmifAsync(string componentHandle);
		Task<(int,string)> RegReadAsync(string componentHandle, string register);
		#endregion
	}
}
