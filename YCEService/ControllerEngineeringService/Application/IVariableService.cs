using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.Application
{
	public interface IVariableService:IDisposable
	{
		//コンポーネントのオープン/クローズ
		Task<ComponentContextBase> OpenComponentAsync();
		Task<int> CloseComponentAsync(string componentHandle);

		//シンボル/レジスタの検索
		Task<string> FindSymbolRegisterAsync(string componentHandle, bool isSymbol,bool isGlobal, string findText,string progName);
	}
}
