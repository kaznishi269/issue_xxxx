using System.Collections.Concurrent;
using Yaskawa.ControllerEngineeringService.Application;

namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// セッションハンドル管理クラス
	/// </summary>
	public class SessionHandleManager
	{
		#region セッション情報クラス
		/// <summary>
		/// エンジニアリングタイプ
		/// </summary>
		public enum EngineeringType
		{
			None = 0,
			TypeNative,         //ネイティブタイプ
			TypeSTPrograming,   //STプログラミングタイプ
			TypeVariable,		//変数タイプ
			TypeAiChat,				//AIチャットタイプ
		}

		/// <summary>
		/// セッション情報クラス
		/// </summary>
		private class SessionContext
		{
			#region エンジニアリング情報クラス
			/// <summary>
			/// エンジニアリング情報クラス
			/// </summary>
			private class EngineeringContext
			{
				/// <summary>
				/// コンストラクタ
				/// </summary>
				/// <param name="type"></param>
				public EngineeringContext(EngineeringType type)
				{
					//エンジニアリングタイプ
					_EngineeringType = type;
				}

				/// <summary>
				/// エンジニアリングタイプ
				/// </summary>
				private EngineeringType _EngineeringType = EngineeringType.None;

				/// <summary>
				/// エンジニアリングハンドル
				/// </summary>
				private string _engineeringHandle { get; } = $"E-{Guid.NewGuid().ToString("N")}";

				/// <summary>
				/// コンポーネントハンドルディクショナリ(Key,Handle)
				/// </summary>
				private ConcurrentDictionary<string, string> _dicComponentHandle = new	 ConcurrentDictionary<string, string>();

				/// <summary>
				/// エンジニアリングハンドルの取得
				/// </summary>
				/// <returns>エンジニアリングハンドル</returns>
				public string GetEngineeringHandle()
				{
					//エンジニアリングハンドルを返却する
					return _engineeringHandle;
				}

				/// <summary>
				/// コンポーネントハンドルの登録
				/// </summary>
				/// <param name="key">キー(検索時に使用する)</param>
				/// <param name="componenthandle">コンポーネントハンドル</param>
				public void RegistComponentHandle(string key, string componenthandle)
				{
					//コンポーネントハンドルを登録する
					_dicComponentHandle.TryAdd(key, componenthandle);
				}

				/// <summary>
				/// コンポーネントハンドルの登録削除
				/// </summary>
				/// <param name="key"></param>
				public void UnregistComponentHandle(string key)
				{
					//コンポーネントハンドルを削除する
					_dicComponentHandle.TryRemove(key, out _);
				}

				/// <summary>
				/// コンポーネントハンドルの取得
				/// </summary>
				/// <param name="key">検索キー</param>
				/// <returns></returns>
				public string GetComponentHandlef(string key)
				{
					//指定キーよりコンポーネントハンドルを検索する
					if (_dicComponentHandle.TryGetValue(key, out var handle) == false)
					{
						//キーが登録されていない
						return "";
					}

					//コンポーネントハンドルを返却する
					return handle;
				}
			}
			#endregion //エンジニアリング情報クラス                 

			/// <summary>
			/// モーションファクトリID
			/// </summary>
			public int MotionFactoryId { get; set; } = 0;

			/// <summary>
			/// セッションハンドル
			/// </summary>
			public string SessionHandle { get; } = $"S-{Guid.NewGuid().ToString("N")}";

			/// <summary>
			/// IPアドレス
			/// </summary>
			public string? IpAddress { get; set; }

			/// <summary>
			/// 従来プログラマ(FWXIDE)のプロセスID
			/// </summary>
			public Int32 LegacyProcessId {  get; set; }

			/// <summary>
			/// 最終アクセス時間
			/// </summary>
			private DateTime _lastAccessTime = DateTime.UtcNow;

			/// <summary>
			/// エンジニアリングハンドルディクショナリ
			/// </summary>
			private readonly ConcurrentDictionary<string, EngineeringContext> _dicEngineeringHandle = new();

			/// <summary>
			/// コンストラクタ
			/// </summary>
			public SessionContext(Int32 legacyProcessId)
			{
				//従来プログラマのプロセスIDを保持しておく
				LegacyProcessId = legacyProcessId;
			}

			/// <summary>
			/// エンジニアリング登録
			/// </summary>
			/// <param name="type">エンジニアリングタイプ</param>
			/// <returns>エンジニアリングハンドル</returns>
			public string RegistEngineering(EngineeringType type)
			{
				//エンジニアリング情報を登録する
				var engineeringInfo = new EngineeringContext(type);
				_dicEngineeringHandle.TryAdd(engineeringInfo.GetEngineeringHandle(), engineeringInfo);

				//エンジニアリングハンドルを返却
				return engineeringInfo.GetEngineeringHandle();
			}

			/// <summary>
			/// エンジニアリング開放
			/// </summary>
			/// <param name="engineeringHandle">エンジニアリングハンドル</param>
			public void UnregistEngineering(string engineeringHandle)
			{
				//ディクショナリよりエンジニアリングハンドルを削除する
				_dicEngineeringHandle.TryRemove(engineeringHandle, out _);
			}

			/// エンジニアリングハンドルの登録確認
			/// </summary>
			/// <param name="engineeringHandle">エンジニアリングハンドル</param>
			/// <returns>
			///		true=登録されている
			///		false=登録されていない
			/// </returns>
			public bool IsEngineeringHandleExist(string engineeringHandle)
			{
				return _dicEngineeringHandle.ContainsKey(engineeringHandle);
			}

			/// <summary>
			/// エンジニアリングハンドル取得
			/// </summary>
			/// <returns>エンジニアリングハンドル</returns>
			public List<string> GetEngineeringHandls()
			{
				List<string> result = new List<string>();
				foreach (var info in _dicEngineeringHandle)
				{
					result.Add(info.Value.GetEngineeringHandle());
				}

				return result;
			}

			/// <summary>
			/// コンポーネントハンドルの登録
			/// </summary>
			/// <param name="engineeringHandle">エンジニアリングハンドル</param>
			/// <param name="componentHandle">コンポーネントハンドル</param>
			/// <param name="key">キー(検索時に使用するキー)</param>
			//public void RegistComponent(string engineeringHandle, string componentHandle, string key)
			public void RegistComponent(string engineeringHandle, ComponentContextBase componentContext)
			{
				//エンジニアリングハンドルよりエンジニアリング情報を取得する
				if (_dicEngineeringHandle.TryGetValue(engineeringHandle, out var engineeringInfo) == false)
				{
					//エンジニアリングハンドルが登録されていない
					return;
				}

				//エンジニアリング情報にコンポーネントハンドルを登録
				engineeringInfo.RegistComponentHandle(componentContext.Key, componentContext.ComponentHandle);
			}

			/// <summary>
			/// コンポーネントハンドルの解放
			/// </summary>
			/// <param name="engineeringHandle">エンジニアリングハンドル</param>
			/// <param name="componentHandle">コンポーネントハンドル</param>
			public void UnregistComponent(string engineeringHandle, string componentHandle)
			{
				//エンジニアリングハンドルよりエンジニアリング情報を取得する
				if (_dicEngineeringHandle.TryGetValue(engineeringHandle, out var engineeringInfo) == false)
				{
					//エンジニアリングハンドルが登録されていない
					return;
				}

				//エンジニアリング情報よりコンポーネントハンドルを解放
				engineeringInfo.UnregistComponentHandle(componentHandle);
			}

			/// <summary>
			/// コンポーネントハンドルの取得
			/// </summary>
			/// <param name="engineeringHandle">エンジニアリングハンドル</param>
			/// <param name="key">キー</param>
			/// <returns></returns>
			public string GetComponentHandle(string engineeringHandle, string key)
			{
				//エンジニアリングハンドルよりエンジニアリング情報を取得する
				if (_dicEngineeringHandle.TryGetValue(engineeringHandle, out var engineeringInfo) == false)
				{
					//エンジニアリングハンドルが登録されていない
					return "";
				}

				//エンジニアリングハンドルよりコンポーネントハンドルを返却する
				return engineeringInfo.GetComponentHandlef(key);
			}

			#region //ヘルスチェック関連
			/// <summary>
			/// 最終アクセス時間の更新
			/// </summary>
			public void Touch() => _lastAccessTime = DateTime.UtcNow;

			/// <summary>
			/// 同一セッションハンドルかの確認
			/// </summary>
			/// <param name="handle">セッションハンドル</param>
			/// <returns>
			///		true=同一ハンドル
			///		false=異なるハンドル
			/// </returns>
			public bool IsSameSessionHandle(string handle)
			{
				return SessionHandle == handle;
			}

			//無通信セッションハンドルを解放するかの判定
			/// <summary>
			/// 無通信セッションハンドル解放判定
			/// </summary>
			/// <returns>
			///		true=開放が必要
			///		false=開放は不要
			/// </returns>
			public bool IsSessionHandleRelease()
			{
				//最終アクセス時間が60秒間経過していたら、解放の必要ありと判断する
				return DateTime.UtcNow - _lastAccessTime > TimeSpan.FromSeconds(60);
			}
			#endregion
		}
		#endregion //セッション情報クラス

		/// <summary>
		/// セッションハンドルディクショナリ
		/// </summary>
		private readonly ConcurrentDictionary<string, SessionContext> _dicSessionHandle = new();

		/// <summary>
		/// ライフタイムインスタンス
		/// </summary>
		private IHostApplicationLifetime _lifetime;

		/// <summary>
		/// レガシーブリッジマネージャインスタンス
		/// </summary>
		private readonly ILegacyBridgeServiceManager _LegacyBridgeManager;

		/// <summary>
		/// オプションインスタンス
		/// </summary>
		private volatile ServerOpsions _options;

		/// <summary>
		/// パイプクライアント
		/// </summary>
		private readonly YCEServicePipeClient _yceServicePipeClient;

		/// <summary>
		/// コンポーネントコンテキストマネージャ
		/// </summary>
		private ComponentContextManager _componentContextManager;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="lifetime">ライフタイムインスタンス</param>
		/// <param name="legacyBridgeManager">レガシーブリッジマネージャインスタンス</param>
		/// <param name="options">オプションインスタンス</param>
		public SessionHandleManager(
			IHostApplicationLifetime lifetime, 
			ILegacyBridgeServiceManager legacyBridgeManager, 
			ServerOpsions options,
			ComponentContextManager componentContext,
			YCEServicePipeClient yceServicePipeClient
			)
		{
			_lifetime = lifetime;
			_LegacyBridgeManager = legacyBridgeManager;
			_options = options;
			_componentContextManager = componentContext;
			_yceServicePipeClient = yceServicePipeClient;
		}

		/// <summary>
		/// クライアント登録
		/// </summary>
		/// <returns></returns>
		public async Task<string> RegistClientAsync(int legacyProcessId)
		{
			SessionContext info = new SessionContext(legacyProcessId);
			//セッションハンドルに紐つくセッション情報を登録する
			_dicSessionHandle.TryAdd(info.SessionHandle, info);

			//プロセスIDが指定されていいるか？
			if (info.LegacyProcessId != 0)
			{
				//従来プログラマのプロセスIDから該当するセッション数を取得する
				if (GetSessionCount(info.LegacyProcessId) == 1)
				{
					//登録されているセッション数が自分のみなので、従来プログラマとの接続パイプを開く
					await _yceServicePipeClient.ConnectAsync(info.LegacyProcessId);
				}
			}

			//セッションハンドルを返却する
			return  await Task.FromResult(info.SessionHandle);
		}

		/// <summary>
		/// クライアント解放
		/// </summary>
		/// <param name="sessionHandle"></param>
		/// <returns></returns>
		public async Task<bool> ReleaseClientAsync(string sessionHandle)
		{
			if (sessionHandle == null)
			{
				return await Task.FromResult(false);
			}
			//セッションハンドルより該当するモーションファクトリIDを取り出す
			if (_dicSessionHandle.TryGetValue(sessionHandle, out var info) == false)
			{
				//該当するセッションハンドルは登録されていない
				return await Task.FromResult(false);
			}

			//従来プログラマのプロセスIDから該当するセッション数を取得する
			if (GetSessionCount(info.LegacyProcessId) == 1)
			{
				//登録されているセッション数は自分のみなので、従来プログラマとの接続パイプを閉じる
				_yceServicePipeClient.Disconnect(info.LegacyProcessId);
			}

			//セッションハンドルディクショナリからも削除する
			_dicSessionHandle.TryRemove(sessionHandle, out _);

			return await Task.FromResult(true);
		}

		//同一のプロセスIDが登録されているセッション数を取得する
		private Int32 GetSessionCount(int processId)
		{
			int count = 0;

			//指定されたプロセスIDから、該当するセッション数を調べる
			foreach (var context in _dicSessionHandle.Values)
			{
				if (context.LegacyProcessId == processId) 
				{
					//プロセスIDが一致した
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// エンジニアリング使用登録
		/// </summary>
		/// <param name="sessionHandle">セッションハンドル</param>
		/// <param name="engineeringType">エンジニアリングタイプ</param>
		/// <returns>エンジニアリングハンドル</returns>
		public string RegistEngineering(string sessionHandle, EngineeringType engineeringType)
		{
			//セッションハンドルより該当するセッション情報を取得する
			if (_dicSessionHandle.TryGetValue(sessionHandle, out var info) == false)
			{
				//このセッションは登録されていない
				return "";
			}
			//エンジニアリングを登録して、エンジニアリングハンドルを返却する
			return info.RegistEngineering(engineeringType);
		}

		/// <summary>
		/// エンジニアリング使用解除
		/// </summary>
		/// <param name="engineeringHandle">エンジニアリングハンドル</param>
		public void UnregistEngineering(string engineeringHandle)
		{
			//指定されたコンポーネントハンドルが、どのセッションハンドルと紐ついているのかを調べる
			foreach (var context in _dicSessionHandle.Values)
			{
				if (context.IsEngineeringHandleExist(engineeringHandle))
				{
					context.UnregistEngineering(engineeringHandle);
					return;
				}
			}
		}

		/// <summary>
		/// モーションファクトリIDの取得
		/// </summary>
		/// <param name="sessioinHandle">セッションハンドル</param>
		/// <returns>モーションファクトリID</returns>
		public int GetMotionFacotyId(string sessioinHandle)
		{
			//セッションハンドルが登録されている時、モーションファクトリIDを返却する
			if (_dicSessionHandle.TryGetValue(sessioinHandle, out var info))
			{
				return info.MotionFactoryId;
			}

			//該当するセッションハンドルがない
			return 0;
		}

		/// <summary>
		/// エンジニアリングハンドル登録確認
		/// </summary>
		/// <param name="engineeringHandle">エンジニアリングハンドル</param>
		/// <returns>
		///		true=ハンドルが登録されている
		///		false=ハンドルは登録されていない
		/// </returns>
		public bool IsEngineeringHandleExist(string engineeringHandle)
		{
			//指定されたコンポーネントハンドルが、どのセッションハンドルと紐ついているのかを調べる
			foreach (var context in _dicSessionHandle.Values)
			{
				if (context.IsEngineeringHandleExist(engineeringHandle))
				{
					//このセッションで使用されているコンポーネントハンドル
					return true;
				}
			}

			//このコンポーネントハンドルは登録されていない
			return false;
		}

		/// <summary>
		/// コンポーネントハンドルの登録
		/// </summary>
		/// <param name="engineeringHandle">エンジニアリングハンドル</param>
		/// <param name="key">キー</param>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		public void RegistComponent(string engineeringHandle,ComponentContextBase componentContext)
		{
			//指定されたエンジニアリングハンドルが、どのセッションハンドルと紐ついているのかを調べる
			foreach (var sessionContext in _dicSessionHandle.Values)
			{
				if (sessionContext.IsEngineeringHandleExist(engineeringHandle))
				{
					//このセッションで使用されているエンジニアリングハンドル

					//コンポーネント情報に従来プログラマのプロセスIDを登録する
					componentContext.LegacyProcessId = sessionContext.LegacyProcessId;

					//コンポーネントハンドルを登録する
					sessionContext.RegistComponent(engineeringHandle, componentContext);

					//コンテキストマネージャにも登録する
					_componentContextManager.AddHandle(componentContext);
					return;
				}
			}
		}

		/// <summary>
		/// コンポーネントハンドルの解放
		/// </summary>
		/// <param name="engineeringHandle">エンジニアリングハンドル</param>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		public void UnregistComponent(string engineeringHandle, string componentHandle)
		{
			//指定されたエンジニアリングハンドルが、どのセッションハンドルと紐ついているのかを調べる
			foreach (var context in _dicSessionHandle.Values)
			{
				if (context.IsEngineeringHandleExist(engineeringHandle))
				{
					//このセッションで使用されているエンジニアリングハンドルなので、
					//コンポーネントハンドルを開放する
					context.UnregistComponent(engineeringHandle, componentHandle);

					//コンテキストマネージャからも開放する
					_componentContextManager.RemoveHandle(componentHandle);
					return;
				}
			}
		}

		/// <summary>
		/// コンポーネントハンドルの取得
		/// </summary>
		/// <param name="engineeringHandle">エンジニアリングハンドル</param>
		/// <param name="key">キー</param>
		/// <returns></returns>
		public string GetComponentHandle(string engineeringHandle, string key)
		{
			//指定されたエンジニアリングハンドルが、どのセッションハンドルと紐ついているのかを調べる
			foreach (var context in _dicSessionHandle.Values)
			{
				//このセッションで使用されているエンジニアリングハンドルか調べる
				if (context.IsEngineeringHandleExist(engineeringHandle))
				{
					//コンポーネントハンドルを返却する
					return context.GetComponentHandle(engineeringHandle,key);
				}
			}

			//エンジニアリングハンドルが登録されていない
			return "";
		}

		//従来プログラマのプロセスIDを取得
		public Int32 GetLegacyProcessId(string engineeringHandle)
		{
			//指定されたエンジニアリングハンドルよりセッション情報を取得する
			if(_dicSessionHandle.TryGetValue(engineeringHandle, out var info))
			{
				//登録されているので、プロセスIDを返却する
				return info.LegacyProcessId;
			}

			//エンジニアリングハンドルが登録されていない
			return 0;
		}

		#region ヘルスチェック関連
		/// <summary>
		/// 最終アクセス時間の更新
		/// </summary>
		/// <param name="sessionHandle">セッションハンドル</param>
		public void UpdateLastAccessTime(string sessionHandle)
		{
			//セッションハンドルよりセッション情報を取得する
			if (_dicSessionHandle.TryGetValue(sessionHandle, out var info))
			{
				//セッションハンドルは登録されているので最終アクセス時間を更新する
				info.Touch();
			}
		}

		/// <summary>
		/// セッションハンドルの存在確認
		/// </summary>
		/// <param name="sessionHandle">セッションハンドル</param>
		/// <returns>
		///		true=存在する
		///		false=存在しない
		/// </returns>
		public bool IsSessionHandleExist(string sessionHandle)
		{
			return _dicSessionHandle.TryGetValue(sessionHandle, out _);
		}

		/// <summary>
		/// セッションハンドル未登録カウンター
		/// </summary>
		private int _unregisteredSessionHandleCount = 0;
		/// <summary>
		/// セッションハンドルのクリーンナップ
		/// </summary>
		public void Cleanup()
		{
			//登録されているセッション情報をチェックする
			foreach (var info in _dicSessionHandle)
			{
				if (info.Value.IsSessionHandleRelease())
				{
					//このセッションは60秒使われていないので、開放する
					ReleaseClientAsync(info.Value.SessionHandle);
				}
			}

			//自プロセスの終了を行うかを判定する
			if (_options.SelfShutdown == false)
			{
				//自プロセスは終了しない
				return;
			}

			//登録されているセッションハンドル数を確認する
			if (_dicSessionHandle.Count <= 0)
			{
				//登録されているセッションハンドルが無くなった判定が２回続いたら自身を終了する
				_unregisteredSessionHandleCount++;
				if (_unregisteredSessionHandleCount >= 2)
				{
					//自プロセスの終了
					_lifetime.StopApplication();
				}
			}
			else
			{
				//カウンターをクリアする
				_unregisteredSessionHandleCount = 0;
			}
		}
		#endregion	ヘルスチェック関連
	}
}
