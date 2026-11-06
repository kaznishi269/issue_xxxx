namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// Cmifサービスインターフェースクラス
	/// </summary>
	public interface ICmifService : IDisposable
	{
		//コンポーネントのオープン/クローズ
		Task<ComponentContextBase> OpenComponentAsync(bool isOnline,int portNo,string ipAddress);
		Task<int> CloseComponentAsync(string componentHandle);

		//レジスタリード(試験実装)
		Task<(int, string)> RegReadAsync(string componentHandle, string register);
	}
}
