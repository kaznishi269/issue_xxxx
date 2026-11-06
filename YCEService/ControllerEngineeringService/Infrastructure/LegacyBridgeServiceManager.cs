using Google.Protobuf;
using Grpc.Net.Client;
using System.Diagnostics;
using System.IO.Pipes;
using Yaskawa.ControllerEngineeringService.Common;
using Yaskawa.ControllerLegacyBridgeService.CimScopeWrapper.V1;
using Yaskawa.ControllerLegacyBridgeService.CmifWrapper.V1;
using Yaskawa.ControllerLegacyBridgeService.Server.V1;
using static Yaskawa.ControllerEngineeringService.Infrastructure.ILegacyBridgeServiceManager;

//エイリアス
using CimScopeClass = Yaskawa.ControllerLegacyBridgeService.CimScopeWrapper.V1;
using CmifClass = Yaskawa.ControllerLegacyBridgeService.CmifWrapper.V1;

namespace Yaskawa.ControllerEngineeringService.Infrastructure
{
	/// <summary>
	/// レガシーブリッジサービスマネージャ
	/// </summary>
	public class LegacyBridgeServiceManager: ILegacyBridgeServiceManager
	{
		/// <summary>
		/// レガシーブリッジサービスプロセス
		/// </summary>
		private Process? _legacyBridgeServiceProcess;

		/// <summary>
		/// 接続チャンネル
		/// </summary>
		private GrpcChannel? _channel;

		/// <summary>
		/// コンフィグレーションインタンス
		/// </summary>
		private readonly IConfiguration _configuration;

		/// <summary>
		/// LegacyBridgeServiceで公開されているサービス
		/// </summary>
		private LBServerGrpcService.LBServerGrpcServiceClient? _server;
		private CimScopeWrapperGrpcService.CimScopeWrapperGrpcServiceClient? _cimScope;
		private CmifWrapperGrpcService.CmifWrapperGrpcServiceClient? _cmif;

		#region 共通部
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="configuration">コンフィグレーションインスタンス</param>
		public LegacyBridgeServiceManager(IConfiguration configuration)
		{
			_configuration = configuration; 
		}

		/// <summary>
		/// 接続判定
		/// </summary>
		public bool IsConnected =>
			_cimScope != null && _cmif != null && _server != null;

		/// <summary>
		/// プロセス開始
		/// </summary>
		/// <returns>なし</returns>
		public async Task StartAsync()
		{
			//レガシーブリッジサービスは起動済みか判定する
			if (_legacyBridgeServiceProcess != null)
			{
				return;
			}

			//同一フォルダのEXEをキックする
			string exePath = Path.Combine(
				AppContext.BaseDirectory,
				"Yaskawa.ControllerLegacyBridgeService.exe");
			var startInfo = new ProcessStartInfo
			{
				FileName = exePath,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = AppContext.BaseDirectory,
			};

			_legacyBridgeServiceProcess = Process.Start(startInfo);
			if (_legacyBridgeServiceProcess == null)
			{
				throw new InvalidOperationException("Failed to start ControllerLegacyBridgeService");
			}

			//レガシーブリッジサービスのNamed Pipeに接続する
			//ツール名称をjsonファイルより取得する
			var toolName = _configuration
				.GetSection("AppSettings")
				.GetValue<string>("ToolName")
				?? throw new InvalidOperationException("Tool name is null.");

			var socketsHttpHandler = new SocketsHttpHandler();
			//通常のTCPソケット接続の代わりに、
			//Windows Named Pipeへ接続するためのコールバックを設定
			socketsHttpHandler.ConnectCallback =
				async (context, token) =>
				{
					//レガシーブリッジサービスのNamed Pipeへ接続する
					//'.'はローカルコンピュータの意味
					var stream = new NamedPipeClientStream(
					".",
					$"Yaskawa.{toolName}.ControllerLegacyBridgeService",
					PipeDirection.InOut,
					PipeOptions.Asynchronous);

					//サーバーがPipeを生成するまで待機する
					await stream.ConnectAsync(token);
					//gRPCの通信ストリームとして返却する
					return stream;
				};

			//gRPCチャネルを生成する。
			//URLは、形式上必要だが、実際の通信経路はConnectCallbackで指定した
			//Named Pipeが使用される。
			_channel = GrpcChannel.ForAddress("http://localhost",
				new GrpcChannelOptions
				{
					HttpHandler = socketsHttpHandler
				});

			//各サービスインスタンスを生成する
			_cimScope = new CimScopeWrapperGrpcService.CimScopeWrapperGrpcServiceClient(_channel);
			_cmif = new CmifWrapperGrpcService.CmifWrapperGrpcServiceClient(_channel);
			_server = new LBServerGrpcService.LBServerGrpcServiceClient(_channel);

			//レガシーブリッジサービスの起動待ち
			await WaitUntilReadyAsync();
		}

		/// <summary>
		/// レガシーブリッジサービスの起動待ち
		/// </summary>
		/// <returns>なし</returns>
		/// <exception cref="TimeoutException">
		/// レガシーブリッジサービスが規定時間内(30秒)以外に起動できなかった
		/// </exception>
		private async Task WaitUntilReadyAsync()
		{
			var timeout = TimeSpan.FromSeconds(30);	//30秒

			//ストップウォッチスタート
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed < timeout)
			{
				try
				{
					if (_server != null)
					{
						//バージョン取得
						var res = await _server.GetVersionAsync(new LBGetVersonReq());
						break;
					}
				}
				catch
				{
				}
				//接続できないので、少し待つ
				await Task.Delay(300);
			}

			if (sw.Elapsed >= timeout)
			{
				throw new TimeoutException("gRPC service startup timeout.");
			}
		}

		/// <summary>
		/// プロセス停止
		/// </summary>
		/// <returns>なし</returns>
		public async Task StopAsync()
		{
			_cimScope = null;

			_channel?.Dispose();
			_channel = null;

			if (_legacyBridgeServiceProcess != null)
			{
				try
				{
					//レガシーブリッジサービスを起動した場合、プロセスを終了する
					if (!_legacyBridgeServiceProcess.HasExited)
					{
						_legacyBridgeServiceProcess.Kill(true);
						await _legacyBridgeServiceProcess.WaitForExitAsync();
					}
				}
				finally
				{
					_legacyBridgeServiceProcess.Dispose();
					_legacyBridgeServiceProcess = null;
				}
			}
		}

		/// <summary>
		/// ディスポーズ
		/// </summary>
		public void Dispose()
		{
			StopAsync().GetAwaiter().GetResult();
		}
		#endregion	共通部

		#region レガシーブリッジServerServiceのサービスメソッド
		//
		//ここからレガシーブリッジServerServiceのサービスメソッド用
		//

		/// <summary>
		/// 生存確認
		/// </summary>
		public void ServerHeartBeat()
		{
			//レガシーブリッジサービスへ生存確認を送信する
			var res =  _server!.HeartbeatAsync(
				new Yaskawa.ControllerLegacyBridgeService.Server.V1.HeartbeatReq
				{
				});
		}
		#endregion	レガシーブリッジServerServiceのサービスメソッド

		#region レガシーブリッジLadderComWrapperのサービスメソッド
		//
		//レガシーブリッジLadderComWrapperのサービスメソッド用
		//

		/// <summary>
		/// ラダーCOMのオープンコンポーネント
		/// </summary>
		/// <param name="isOnline">オンライン情報 true=オンライン　false=オフライン</param>
		/// <param name="motionFactoryId">モーションファクトリID</param>
		/// <returns>
		///		CimScopeラッパー用リターンステータス
		///			1:エラーコード
		///			2:コンポーネントハンドル
		/// </returns>
		public async Task<(CimScopeResult, string)> OpenComponentLadderComAsync(
			bool isOnline,
			int motionFactoryId)
		{
			var res =
				await _cimScope!.OpenComponentAsync(
					new CimScopeClass.OpenComponentReq
					{
						IsOnline = isOnline,
						MotionFactoryId = motionFactoryId,
					});

			//エラーコード変換して返却する
			var result = ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
			return (result, res.ComponentHandle);
		}

		/// <summary>
		/// ラダーCOMのクローズコンポーネント
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>CimScopeラッパー用リターンステータス</returns>
		public async Task<CimScopeResult> CloseComponentLadderComAsync(
			string componentHandle)
		{
			var res =
				await _cimScope!.CloseComponentAsync(
					new Yaskawa.ControllerLegacyBridgeService.CimScopeWrapper.V1.CloseComponentReq
					{
						ComponentHandle= componentHandle
					});

			//エラーコード変換して返却する
			return ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
		}

		/// <summary>
		/// ラダーCOMのプログラムオープン
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <param name="progName">プログラム名</param>
		/// <returns>CimScopeラッパー用リターンステータス</returns>
		public async Task<CimScopeResult> LadderOpenAsync(
			string componentHandle, string progName)
		{
			var res = await _cimScope!.LadderProgOpenAsync(
				new LadderProgOpenReq
				{
					ComponentHandle= componentHandle,
					PrgFile= progName
				});

			//エラーコード変換して返却する
			return ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
		}

		/// <summary>
		/// ラダーCOMのプログラムクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns>CimScopeラッパー用リターンステータス</returns>
		public async Task<CimScopeResult> LadderCloseAsync(
			string componentHandle)
		{
			var res = await _cimScope!.LadderProgCloseAsync(
				new LadderProgCloseReq
				{
					ComponentHandle = componentHandle
				});

			//エラーコード変換して返却する
			return ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
		}
		#endregion		レガシーブリッジLadderComWrapperのサービスメソッド

		/// <summary>
		/// エラーコード変換
		/// </summary>
		/// <param name="csError">CimScopeラッパーのエラーコード</param>
		/// <param name="csHresult">CimScopeの例外コード</param>
		/// <returns>CimScopeラッパー用リターンステータス</returns>
		private CimScopeResult ConvertErrorCode(CimScopeWrapperErrorCode csError, int csHresult)
		{
			ErrorCode errorCode = ErrorCode.Normal;
			switch (csError)
			{
				case CimScopeWrapperErrorCode.NoError:							//正常
					errorCode = ErrorCode.Normal;
					break;
				case CimScopeWrapperErrorCode.ComponentHandleNotRegistered:		//コンポーネントハンドルが登録されていない
					errorCode = ErrorCode.EngineeringNotRegistered;
					break;
				case CimScopeWrapperErrorCode.MotionFactoryNotFound:			//モーションファクトリが取得できない
					errorCode = ErrorCode.SystemError;
					break;
				case CimScopeWrapperErrorCode.InvalidProgramName:				//プログラム名称の誤り
					errorCode = ErrorCode.InvalidProgramName;
					break;
				case CimScopeWrapperErrorCode.CimScopeException:				//CimScopeの例外
					errorCode = ErrorCode.CimScopeException;
					break;
				case CimScopeWrapperErrorCode.ComInstanceCreateFailed:			//COMのインスタンス生成に失敗
					errorCode = ErrorCode.SystemError;
					break;
			}

			return new CimScopeResult
			{
				result = errorCode,
				cimScopeHResult = csHresult
			};
		}

		//試験実装(プロパティ/パケット読み書き)
		public async Task<(CimScopeResult,string)> LadderProgGetPropertyAsync(
			string componentHandle)
		{
			var res = await _cimScope!.LadderProgGetPropertyAsync(
				new LadderProgGetPropertyReq
				{
					ComponentHandle = componentHandle
				});

			//エラーコード変換して返却する
			var result = ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
			return (result,res.ProgramComment);
		}

		//ラダープログラムパケットの読み込み
		public async Task<(CimScopeResult, ByteString)> LadderProgReadProgramDataPacketAsync(
			string componentHandle, 
			short packetId, 
			short packetNumber)
		{
			var res = await _cimScope!.LadderProgReadProgramDataPacketAsync(
				new LadderProgReadProgramDataPacketReq
				{
					ComponentHandle = componentHandle,
					PacketId = packetId,
					PacketNumber = packetNumber
				});

			var result = ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
			return (result, res.Data);
		}

		//ラダープログラムパケットの書き込み
		public async Task<CimScopeResult> LadderProgWriteProgramDataPacketAsync(
			string componentHandle,
			ByteString data)
		{
			var res = await _cimScope!.LadderProgWriteProgramDataPacketAsync(
				new LadderProgWriteProgramDataPacketReq
				{
					ComponentHandle = componentHandle,
					Data = data
				});

			//エラーコード変換して返却する
			return ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
		}

		//ラダープログラムの保存
		public async Task<CimScopeResult> LadderProgSaveAsync(
			string componentHandle)
		{
			var res = await _cimScope!.LadderProgSaveAsync(
				new LadderProgSaveReq
				{
					ComponentHandle = componentHandle,
				});

			//エラーコード変換して返却する
			return ConvertErrorCode(res.Result, res.CimScopeExceptionResult);
		}

		#region レガシーブリッジCmifWrapperServiceのサービスメソッド
		//
		//ここからレガシーブリッジCmifWrapperServiceのサービスメソッド用
		//

		/// <summary>
		/// Cmif用のオープンコンポーネント
		/// </summary>
		/// <param name="isOnline">オンライン情報 true=オンライン false=オフライン</param>
		/// <param name="portNo">論理ポート番号</param>
		/// <param name="ipAddress">IPアドレス</param>
		/// <returns>コンポーネントハンドル</returns>
		public async Task<string> OpenComponentCmifAsync(
			bool isOnline,
			int portNo,
			string ipAddress)
		{
			var res=
				await _cmif!.OpenComponentAsync(
					new Yaskawa.ControllerLegacyBridgeService.CmifWrapper.V1.OpenComponentReq
					{
						IsOnline = isOnline,
						PortNo = portNo,
						IpAddress = ipAddress
					});

			return res.ComponentHandle;
		}

		/// <summary>
		/// Cmifのコンポーネントクローズ
		/// </summary>
		/// <param name="componentHandle">コンポーネントハンドル</param>
		/// <returns></returns>
		public async Task<int> CloseComponentCmifAsync(
			string componentHandle)
		{
			var res =
				await _cmif!.CloseComponentAsync(
					new Yaskawa.ControllerLegacyBridgeService.CmifWrapper.V1.CloseComponentReq
					{
						ComponentHandle = componentHandle
					});

			return (int)res.Result;
		}

		//レジスタ読み込み(試験実装)
		public async Task<(int,string)> RegReadAsync(
			string componentHandle,string register)
		{
			var res =
				await _cmif!.RegReadAsync(
					new RegReadReq
					{
						ComponentHandle = componentHandle,
						Registar = register
					});


			return ((int)res.Result, res.Value);
		}
		#endregion	レガシーブリッジCmifWrapperServiceのサービスメソッド
	}
}
