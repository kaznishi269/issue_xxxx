using Google.Protobuf;
using Grpc.Core;
using Yaskawa.ControllerEngineeringService.Client.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// プログラムサービスコンテキスト
	/// </summary>
	public class ProgramComponentContext : ComponentContextBase
	{
		//このサービス特有の情報を保持する場合は、ここに追加する。
		public int MotionFactoryId { get; set; }
	}

	/// <summary>
	/// プログラムサービスクラス
	/// </summary>
	public class ProgramService:IProgramService
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<ClientGrpcServiceImpl> _logger;

		/// <summary>
		/// レガシーブリッジサービスマネージャインスタンス
		/// </summary>
		private readonly ILegacyBridgeServiceManager _legacyBridgeService;

		/// <summary>
		/// コンポーネントコンテキストマネージャ
		/// </summary>
		ComponentContextManager _componentContextManager;

		/// <summary>
		/// AIサービス
		/// </summary>
		private readonly IAiChatService _aiService;

		/// <summary>
		/// Variableサービス
		/// </summary>
		private readonly IVariableService _variableService;

		/// <summary>
		/// AIコンポーネントコンテキスト
		/// </summary>
		private readonly string aiComponentKey = "AiComponent";

		private readonly string variableComponentKey = "VariableComponent";

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービス</param>
		/// <param name="legacyBridgeService">レガシーブリッジサービス</param>
		public ProgramService(
			ILogger<ClientGrpcServiceImpl> logger,
			ILegacyBridgeServiceManager legacyBridgeService,
			ComponentContextManager componentContextManager,
            IAiChatService aiService,
			IVariableService variableService
			)
		{
			_logger = logger;
			_legacyBridgeService = legacyBridgeService;
			_aiService = aiService;
			_componentContextManager = componentContextManager;
			_variableService = variableService;
		}

		/// <summary>
		/// ディスポーズ
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// プログラムサービスのオープン
		/// </summary>
		/// <param name="isOnline">オンライン情報 true=オンライン false=オフライン</param>
		/// <param name="motionFactoryId">モーションファクトリID</param>
		/// <returns>コンポーネントハンドル</returns>
		/// <exception cref="RpcException">
		/// CimScopeException:CimScope例外
		/// </exception>
		public async Task<ProgramComponentContext> OpenComponentAsync(bool isOnline,int motionFactoryId)
		{
			//レガシーブリッジサービスへCimScopeの接続を依頼する
			var result = await _legacyBridgeService.OpenComponentLadderComAsync(
				isOnline,
				motionFactoryId
				);

			string componentHandle = "";
			if(result.Item1.result != Common.ErrorCode.Normal)
			{
				//CimScopeの例外を発行する
				//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
				var trailers = new Metadata
				{
					{"error-code", ((int)result.Item1.result).ToString()},
				};
				if (result.Item1.result == ErrorCode.CimScopeException)
				{
					trailers.Add("hresult", result.Item1.cimScopeHResult.ToString());
				}
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}
			else
			{
				//コンポーネントハンドル
				componentHandle = result.Item2;
			}

			//AIサービスをオープンする
			var aiContext = await _aiService.OpenComponentAsync();

			//アクセスキーを作成。アクセスキーをユニークにするため、モーションファクトリIDを追加する
			aiContext.Key = aiComponentKey + motionFactoryId.ToString();
			//AIサービスのコンテキストを登録しておく
			_componentContextManager.AddHandle(aiContext);

			//Variableサービスをオープンする
			var variableContext = await _variableService.OpenComponentAsync();
			//アクセスキーを作成。アクセスキーをユニークにするため、モーションファクトリIDを追加する
			variableContext.Key = variableComponentKey + motionFactoryId.ToString();
			//AIサービスのコンテキストを登録しておく
			_componentContextManager.AddHandle(variableContext);

			return await Task.FromResult(new ProgramComponentContext
			{
				Type = ComponentType.TypeProgram,
				ComponentHandle = componentHandle,
				MotionFactoryId = motionFactoryId,
			});
		}

		/// <summary>
		/// プログラムサービスのクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>リターンステータス</returns>
		/// <exception cref="RpcException">
		/// CimScopeException:CimScope例外
		/// </exception>
		public async Task<int> CloseComponentAsync(string componentHandle)
		{
			//モーションファクトリIDを取得
			var programContext = _componentContextManager.GetComponentContextFromHandle(componentHandle);
			if (programContext == null)
			{
				//コンポーネントハンドルが登録されていない（システムエラー)
				return (int)ErrorCode.SystemError;
			}
			//AIコンテキストを取得するため、キーを作成する
			int motionFactoryId = ((ProgramComponentContext)programContext).MotionFactoryId;
			string aiKey = aiComponentKey + motionFactoryId.ToString();

			//レガシーブリッジサービスへラダーのクローズを依頼する
			var resultClose = await _legacyBridgeService.CloseComponentLadderComAsync(componentHandle);
			if (resultClose.result != 0)
			{
				//CimScopeの例外を発行する
				//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
				var trailers = new Metadata
				{
					{"error-code", ((int)resultClose.result).ToString()},
				};
				if (resultClose.result == ErrorCode.CimScopeException)
				{
					trailers.Add("hresult", resultClose.cimScopeHResult.ToString());
				}
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			//AIサービスのコンテキストを削除する
			var aiContext = _componentContextManager.GetComponentContextFromKey(aiKey);
			if (aiContext != null)
			{
				_componentContextManager.RemoveHandle(aiContext.ComponentHandle);
			}

			//Variableコンテキストを取得するため、キーを作成する
			string variableKey = variableComponentKey + motionFactoryId.ToString();

			//Variableサービスのコンテキストを削除する
			var variableContext = _componentContextManager.GetComponentContextFromKey(variableKey);
			if (variableContext != null)
			{
				_componentContextManager.RemoveHandle(variableContext.ComponentHandle);
			}

			return (int)ErrorCode.Normal;
		}

		/// <summary>
		/// プログラムのオープン
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <param name="progName">プログラム名</param>
		/// <returns>リターンステータス</returns>
		/// <exception cref="RpcException">
		/// CimScopeException:CimScope例外
		/// </exception>
		public async Task<int> OpenAsync(
			string componentHandle, string progName)
		{
			//レガシーブリッジサービスへラダーのオープンを依頼する
			var resultOpen = await _legacyBridgeService.LadderOpenAsync(componentHandle, progName);
			if (resultOpen.result != 0)
			{
				//CimScopeの例外を発行する
				//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
				var trailers = new Metadata
				{
					{"error-code", ((int)resultOpen.result).ToString()},
				};
				if (resultOpen.result == ErrorCode.CimScopeException)
				{
					trailers.Add("hresult", resultOpen.cimScopeHResult.ToString());
				}
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			return (int)resultOpen.result;
		}

		/// <summary>
		/// プログラムのクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>リターンステータス</returns>
		/// <exception cref="RpcException">
		/// CimScopeException:CimScope例外
		/// </exception>
		public async Task<int> CloseAsync(string componentHandle)
		{
			//レガシーブリッジサービスへラダーのクローズを依頼する
			var resultClose = await _legacyBridgeService.LadderCloseAsync(componentHandle);
			if (resultClose.result != 0)
			{
				//CimScopeの例外を発行する
				//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
				var trailers = new Metadata
				{
					{"error-code", ((int)resultClose.result).ToString()},
				};
				if (resultClose.result == ErrorCode.CimScopeException)
				{
					trailers.Add("hresult", resultClose.cimScopeHResult.ToString());
				}
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			return  (int)resultClose.result;
		}

		/// <summary>
		/// プログラムの読み込み(STプログラムのみ)
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>STプログラムのテキストデータ</returns>
		/// <exception cref="RpcException"></exception>
		public async Task<string> ReadAsync(string componentHandle)
		{
			//レガシーブリッジサービスへラダーパケットの読み込みを依頼する
			//とりあえず、STプログラムのテキストパケットIDを0x107Fとする
			var (result, data) = await _legacyBridgeService.LadderProgReadProgramDataPacketAsync(componentHandle, 0x107f, 0);
			if (result.result != 0)
			{
#warning エラーの値をどこかに定義しないと。。
				//パケット無しの場合は例外にしない
				if (result.cimScopeHResult != unchecked((int)0xa0a0240d))
				{
					//CimScopeの例外を発行する
					//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
					var trailers = new Metadata
					{
						{"error-code", ((int)result.result).ToString()},
					};
					if (result.result == ErrorCode.CimScopeException)
					{
						trailers.Add("hresult", result.cimScopeHResult.ToString());
					}
					throw new RpcException(
						new Status(
						StatusCode.Unknown,
							""),
						trailers);
				}
			}

			//読み込んだデータをパケットヘッダとボディに分割する
			if (ApplicationHelper.TryParsePacketByteString(
				data,
				out Structures.PkHead header,
				out ByteString body
				) == false)
			{
				//分割エラー
				return await Task.FromResult("");
			}

			return await Task.FromResult(body.ToStringUtf8());
		}

		/// <summary>
		/// プログラムのコンパイル（STプログラムのみ）
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <param name="programText">STプログラムのテキストデータ</param>
		/// <returns>
		///		0：正常
		///		他は未定
		/// </returns>
		/// <exception cref="RpcException"></exception>
		public async Task<int> CompileAsync(string componentHandle, string programText)
		{
			//渡されたプログラムテキストをコンパイルする
			//compile()

			//プログラムテキストのパケットを書き込む

			//プログラムテキストをバイトストリングに変換
			ByteString programTextData = ByteString.CopyFromUtf8(programText);

			//パケットのバイトストリングを作成する
			ByteString pktData = ApplicationHelper.MakePacketByteString(new Structures.PkHead
			{
				Id = 0x107f,	//パケットID
				Level = 0,		//パケット特権
				Atr = 0,		//パケット属性
				Ver = 0,		//バージョン番号
				No = 0,			//パケット番号
				Cnt = 0,		//更新カウンター
				Step = 0,		//ステップ
				Size = 0		//パケットサイズ

			},
			programTextData);

			//レガシーブリッジサービスへラダーパケットの書き込みを依頼する
			var resultWritePacket = await _legacyBridgeService.LadderProgWriteProgramDataPacketAsync(componentHandle, pktData);
			if (resultWritePacket.result != 0)
			{
				//CimScopeの例外を発行する
				//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
				var trailers = new Metadata
				{
					{"error-code", ((int)resultWritePacket.result).ToString()},
				};
				if (resultWritePacket.result == ErrorCode.CimScopeException)
				{
					trailers.Add("hresult", resultWritePacket.cimScopeHResult.ToString());
				}
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			//コンパイルされたメインプログラムのパケットを書き込む
			//ファイルを保存する
			var resultWrite = await _legacyBridgeService.LadderProgSaveAsync(componentHandle);
			if (resultWrite.result != 0)
			{
				//CimScopeの例外を発行する
				//CimScope例外の場合、gRPC定義にStatusCodeに分類するのが困難なので、Unknownで返却する。
				var trailers = new Metadata
				{
					{"error-code", ((int)resultWrite.result).ToString()},
				};
				if (resultWrite.result == ErrorCode.CimScopeException)
				{
					trailers.Add("hresult", resultWrite.cimScopeHResult.ToString());
				}
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			return await Task.FromResult(0);
		}

		//以降、試験実装
		//プロパティ取得（試験実装)
		public async Task<string> GetPropertyAsync(string componentHandle)
		{
			//レガシーブリッジサービスへラダープロパティの取得を依頼する
			var result = await _legacyBridgeService.LadderProgGetPropertyAsync(componentHandle);

			string comment =  new("");
			if(result.Item1.result == ErrorCode.Normal)
			{
				comment = result.Item2;
			}
			return await Task.FromResult(comment);
		}
	}
}
