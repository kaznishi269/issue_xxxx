using Grpc.Core;
using System.Buffers.Binary;
using System.Text;
using Yaskawa.ControllerEngineeringService.Client.V1.GrpcService;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerEngineeringService.Infrastructure;

namespace Yaskawa.ControllerEngineeringService.Application
{
	/// <summary>
	/// Variableサービスのコンポーネントコンテキスト
	/// </summary>
	public class VariableComponentContext: ComponentContextBase
	{
		//このサービス特有の情報を保持する場合は、ここに追加する。
		public string sss { get; set; } = "";
	}

	/// <summary>
	/// Variableサービスクラス
	/// </summary>
	public class VariableService : IVariableService
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<ClientGrpcServiceImpl> _logger;

		/// <summary>
		/// 既存プログラマとの連携
		/// </summary>
		private readonly YCEServicePipeClient _ycesPipeClient;

		/// <summary>
		/// コンポーネントコンテキストマネージャ
		/// </summary>
		private readonly ComponentContextManager _componentContextManager;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービス</param>
		/// <param name="appClient">既存プログラマとの連携</param>
		/// <param name="componentContextManager">コンポーネントコンテキストマネージャ</param>
		public VariableService(
			ILogger<ClientGrpcServiceImpl> logger,
			YCEServicePipeClient appClient,
			ComponentContextManager componentContextManager
			)
		{
			_logger = logger;
			_ycesPipeClient = appClient;
			_componentContextManager = componentContextManager;
		}

		/// <summary>
		/// ディスポーズ
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// Variableサービスのオープン
		/// </summary>
		/// <returns>Variableサービスコンテキスト</returns>
		public async Task<ComponentContextBase> OpenComponentAsync()
		{
			//コンポーネントハンドルを作成
			string componentHandle = $"C-{Guid.NewGuid().ToString("N")}";

			return await Task.FromResult(new VariableComponentContext()
			{
				Type = ComponentType.TypeVariable,
				ComponentHandle = componentHandle
			});
		}

		/// <summary>
		/// Variableサービスのクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>リターンステータス</returns>
		public async Task<int> CloseComponentAsync(string componentHandle)
		{
			return await Task.FromResult((int)ErrorCode.Normal);
		}

		/// <summary>
		/// シンボル/レジスタ検索
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <param name="isSymbol">シンボル検索か？
		///		true＝シンボル検索　false=レジスタ検索
		/// </param>
		/// <param name="isGlobal">グローバルレジスタか？
		///		true=グローバルレジスタ　false=ローカルレジスタ
		/// </param>
		/// <param name="findText">検索対象テキスト</param>
		/// <param name="progName">プログラム名</param>
		/// <returns>検索結果</returns>
		/// <exception cref="InvalidOperationException">検索エラー</exception>
		/// <exception cref="InvalidDataException">指定データ不正</exception>
		public async Task<string> FindSymbolRegisterAsync(string componentHandle, bool isSymbol, bool isGlobal, string findText, string progName)
		{
			//コンポーネントハンドルより従来プログラマのプロセスIDを取得する
			var context = _componentContextManager.GetComponentContextFromHandle(componentHandle);
			int legacyProcessId = context!.LegacyProcessId;

			//----------------------------------------------------------
			// Request本体作成
			//
			//	struct YceServiceFindSymbolRegisterReq
			//	{
			//		BOOL	bSymbol;
			//		BOOL	bGlobal;
			//		UINT32	nFindTextLength;
			//		UINT32	nProgNameLength;
			//	};
			//----------------------------------------------------------

			//検索文字列、プログラム名をUTF-8へ変換する
			byte[] stringBytesText = Encoding.UTF8.GetBytes(findText);
			byte[] stringBytesProg = Encoding.UTF8.GetBytes(progName);

			byte[] requestData = new byte[16 + stringBytesText.Length + stringBytesProg.Length];

			//SymbolまたはRegister
			BinaryPrimitives.WriteInt32LittleEndian(requestData, isSymbol ? 1 : 0);

			//GlobalまたはLocal
			BinaryPrimitives.WriteInt32LittleEndian(requestData.AsSpan(4, 4), isGlobal ? 1 : 0);

			//検索テキストのサイズ
			BinaryPrimitives.WriteInt32LittleEndian(requestData.AsSpan(8, 4), stringBytesText.Length);

			//プログラム名のサイズ
			BinaryPrimitives.WriteInt32LittleEndian(requestData.AsSpan(12, 4), stringBytesProg.Length);

			//検索テキスト
			stringBytesText.CopyTo(requestData.AsSpan(16));

			//プログラム名
			stringBytesProg.CopyTo(requestData.AsSpan(16 + stringBytesText.Length));

			////----------------------------------------------------------
			//// RequestHeader作成
			////
			//// struct RequestHeader
			//// {
			////		DWORD nVersion;
			////		DWORD nRequestId;
			////		DWORD nCommand;
			////		DWORD nDataLength;
			//// };
			////----------------------------------------------------------

			//リクエストヘッダ送信
			await _ycesPipeClient.WriteRequestHeaderAsync(
				legacyProcessId,
				(uint)YcePipeCommand.FindSymbolRegister,
				(uint)requestData.Length);

			//リクエストデータ送信
			await _ycesPipeClient.WritePipeAsync(legacyProcessId, requestData, true);

			//レスポンスヘッダ受信
			var resHeader = await _ycesPipeClient.ReadResponceHeaderAsync(legacyProcessId);

			//処理結果
			uint result = resHeader.result;
			//データレングス
			uint dataLength = resHeader.dataLength;

			// エラーチェック
			if (result != (uint)YcePipeResult.Success)
			{
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.LegacyProgramCommandExecutionFailed)).ToString()},
					{"result", result.ToString() }
				};
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			//----------------------------------------------------------
			// 固定長レスポンス受信
			//
			// C++側
			//
			//struct YceServiceFindSymbolRegisterRes
			//	{
			//		INT32  status;
			//		UINT32 nLength;
			//	};
			//----------------------------------------------------------

			const int ResponseStructSize = 8;
			byte[] responseInfo =
				new byte[ResponseStructSize];

			await _ycesPipeClient.ReadExactAsync(legacyProcessId, responseInfo);

			int offset = 0;
			int status = BinaryPrimitives.ReadInt32LittleEndian(
					responseInfo.AsSpan(offset, 4));
			offset += 4;

			uint resultTextLength = BinaryPrimitives.ReadUInt32LittleEndian(
					responseInfo.AsSpan(offset, 4));
			offset += 4;

			// 念のためサイズチェック
			if (dataLength != ResponseStructSize + resultTextLength)
			{
				var trailers = new Metadata
				{
					{"error-code", ((int)(ErrorCode.LegacyProgramPipeCommunicationFailed)).ToString()},
					{"result", "Invalid response length." }
				};
				throw new RpcException(
					new Status(
					StatusCode.Unknown,
						""),
					trailers);
			}

			// UTF-8文字列受信
			string resultText = string.Empty;
			if (resultTextLength > 0)
			{
				byte[] resultTextBytes = new byte[resultTextLength];

				await _ycesPipeClient.ReadExactAsync(legacyProcessId, resultTextBytes);
				resultText = Encoding.UTF8.GetString(resultTextBytes);
			}

			return await Task.FromResult(resultText);
		}
	}
}
