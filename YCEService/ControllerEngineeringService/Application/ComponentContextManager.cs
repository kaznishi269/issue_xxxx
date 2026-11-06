using System.Collections.Concurrent;

namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// コンポーネントタイプ
	/// </summary>
	public enum ComponentType
	{
		None = 0,
		TypeProgram,	//プログラムコンテキスト
		TypeCmif,		//Cmifコンテキスト
		TypeAi,			//AIコンテキスト
		TypeVariable,	//Variableコンテキスト
	}

	/// <summary>
	/// コンポーネントコンテキストベース
	/// 各アプリケーションレイヤサービスは、このクラスを基底クラスとした
	/// コンテキストクラスを用意すること
	/// </summary>
	public class ComponentContextBase
	{
		/// コンポーネントタイプ
		/// </summary>
		public ComponentType Type { get; set; } = ComponentType.None;

		/// <summary>
		/// コンポーネントハンドル
		/// </summary>
		public string ComponentHandle { get; set; } = "";

		/// <summary>
		/// 検索キー
		/// </summary>
		public string Key { get; set; } = "";

		/// <summary>
		/// 従来プログラマのプロセスID
		/// </summary>
		public int LegacyProcessId { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ComponentContextBase()
		{
		}
	}

	/// <summary>
	/// コンポーネントコンテキストマネージャ
	/// </summary>
	public class ComponentContextManager
	{
		/// <summary>
		/// コンポーネントハンドルコレクション(Handle,Context)
		/// </summary>
		private readonly ConcurrentDictionary<string, ComponentContextBase> _dicComponent = new ConcurrentDictionary<string, ComponentContextBase>();

		/// <summary>
		/// コンポーネントハンドルコレクション(Key,Context)
		/// </summary>
		private readonly ConcurrentDictionary<string, ComponentContextBase> _dicKey= new ConcurrentDictionary<string, ComponentContextBase>();

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ComponentContextManager()
		{
		}

		/// <summary>
		/// コンポーネントハンドルの登録
		/// </summary>
		/// <param name="componentContext"></param>
		public void AddHandle(ComponentContextBase componentContext)
		{
			//入力パラメータをチェックする
			if(string.IsNullOrEmpty(componentContext.ComponentHandle) ||  
				string.IsNullOrEmpty(componentContext.Key) )
			{
				//コンポーネントハンドルまたは、コンテキストが設定されていない
				return;
			}

			//コンポーネントハンドルが既に登録されているかチェックする
			if(_dicComponent.TryGetValue(componentContext.ComponentHandle, out ComponentContextBase? componentContextBase) != false)
			{
				//既に登録されているので、削除してから登録する。
				string key = componentContext.Key;
				_dicComponent.TryRemove(componentContext.ComponentHandle, out _);
				_dicKey.TryRemove(key, out _);
			}

			//コンポーネントハンドルをキーとするディクショナリに登録
			_dicComponent.TryAdd(componentContext.ComponentHandle, componentContext);
	
			//キーをキーとするディクショナリに登録
			_dicKey.TryAdd(componentContext.Key, componentContext);
		}

		/// <summary>
		/// コンポーネントハンドルの削除
		/// </summary>
		/// <param name="handle">コンポーネントハンドル</param>
		public void RemoveHandle(string handle)
		{
			//コンポーネントハンドルは登録されているか？
			if (_dicComponent.TryGetValue(handle, out var context) == false)
			{
				//登録されていない
				return;
			}

			//キーを保持
			string key = context.Key;

			//ディクショナリから削除する
			_dicComponent.TryRemove(handle, out _);
			_dicKey.TryRemove(key,out _);
		}

		/// <summary>
		/// コンポーネントコンテキストの取得（コンポーネントハンドルから取得）
		/// </summary>
		/// <param name="handle">コンポーネントハンドル</param>
		/// <returns>コンポーネントコンテキスト</returns>
		public ComponentContextBase? GetComponentContextFromHandle(string handle)
		{
			if (_dicComponent.TryGetValue(handle, out var context) == false)
			{
				//登録されていない
				return null;
			}
			return context;
		}

		/// <summary>
		/// コンポーネントコンテキストの取得（検索キーから取得）
		/// </summary>
		/// <param name="key">検索キー</param>
		/// <returns>コンポーネントコンテキスト</returns>
		public ComponentContextBase? GetComponentContextFromKey(string key)
		{
			//検索キーは登録されているか？
			if (_dicKey.TryGetValue(key, out var context) == false)
			{
				//登録されていない
				return null;
			}

			return context;
		}
	}
}
