using System.Collections.Concurrent;

namespace Yaskawa.ControllerLegacyBridgeService.Infrastructure
{
	/// <summary>
	/// コンポーネント情報のベースクラス
	/// </summary>
	public class ComponentContextBase
	{
		/// <summary>
		/// コンポーネントタイプ
		/// </summary>
		public enum ComponentType
		{
			None = 0,
			TypeCMIF,		//CMIF
			TypePlcCom,		//コントローラCOM
			TypeLadderCom,	//ラダープログラムCOM
		}
		ComponentType componentType { get; }
		/// <summary>
		/// コンポーネントハンドル
		/// </summary>
		private string _componentHandle;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="type">コンポーネントタイプ</param>
		public ComponentContextBase(ComponentType type)
		{
			//コンポーネントハンドル作成
			_componentHandle = "C-" + Guid.NewGuid().ToString("N");
			//コンポーネントタイプ
			componentType = type;
		}

		/// <summary>
		/// コンポーネントハンドルの取得
		/// </summary>
		/// <returns></returns>
		public string GetComponentHandle()
		{
			return _componentHandle;
		}
	}

	/// <summary>
	/// コンポーネント管理クラス
	/// </summary>
	public class ComponentManager
	{
		//コンポーネントハンドルコレクション
		private readonly ConcurrentDictionary<string, ComponentContextBase> _dicComponentHandle = new();

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ComponentManager()
		{
		}

		/// <summary>
		/// コンポーネント情報の登録
		/// </summary>
		/// <param name="component">コンポーネント情報</param>
		public void AddComponent(ComponentContextBase component)
		{
			//コンポーネントをディクショナリに登録する
			_dicComponentHandle.TryAdd(component.GetComponentHandle(), component);
		}

		/// <summary>
		/// コンポーネントの削除
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		public void RemoveComponent(string componentHandle)
		{
			//ディクショナリから指定コンポーネント情報を削除する
			_dicComponentHandle.TryRemove(componentHandle, out _);
		}

		/// <summary>
		/// コンポーネントハンドルの登録確認
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>
		/// true:登録されている
		/// false:登録されていない
		/// </returns>
		public bool IsComponentHandleExist(string componentHandle)
		{
			return _dicComponentHandle.TryGetValue(componentHandle, out _);
		}

		/// <summary>
		/// コンポーネント情報の返却
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>コンポーネント情報</returns>
		public ComponentContextBase? GetComponent(string componentHandle)
		{
			_dicComponentHandle.TryGetValue(componentHandle, out var component);
			return component;
		}
	}
}
