namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// プログラムサービスインターフェースクラス
	/// </summary>
	public interface IProgramService : IDisposable
	{
		//コンポーネントのオープン
		Task<ProgramComponentContext> OpenComponentAsync(bool isOnline, int motionFactoryId);
		//コンポーネントのクローズ
		Task<int> CloseComponentAsync(string componentHandle);

		//プログラムのオープン
		Task<int> OpenAsync(string componentHandle, string progName);
		//プログラムのクローズ
		Task<int> CloseAsync(string componentHandle);

		//プログラムの読み込み
		Task<string> ReadAsync(string componentHandle);

		//プログラムのコンパイル（STプログラムのみ）
		Task<int> CompileAsync(string componentHandle,string programText);

		//以降、試験実装
		//プロパティ取得（コメントのみ）（試験実装）
		Task<string> GetPropertyAsync(string componentHandle);
	}
}
