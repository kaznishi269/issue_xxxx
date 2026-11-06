using Google.Protobuf;
using Grpc.Core;
using MpLadderProg;
using MpMotionFactory;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yaskawa.ControllerLegacyBridgeService.Infrastructure;
using Yaskawa.ControllerEngineeringCommon.Utility;
using YCSCONTROLLERSERVERLib;
using YCSPROJECTSERVERLib;

namespace Yaskawa.ControllerLegacyBridgeService.CimScopeWrapper.V1.GrpcService
{
	//
	/// <summary>
	/// LadderProgCOMコンポーネントタイプ
	/// </summary>
	public class ComponentContextLadderCom : ComponentContextBase
	{
		/// <summary>
		/// モーションファクトリ
		/// </summary>
		public object? motionFactory { set; get; } = null;
		/// <summary>
		/// オンライン情報
		/// </summary>
		public bool isOnline { set;get; }=false;
		/// <summary>
		/// モーションファクトリID
		/// </summary>
		public int motionfactoryId { set; get; }= 0;
		/// <summary>
		/// ラダープログラムCOM
		/// </summary>
		public object? ladderCom {  set; get; } = null;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ComponentContextLadderCom()
				: base(ComponentType.TypeLadderCom)
		{
			motionFactory = null;
			motionfactoryId = 0;
			ladderCom = null;
		}
	}

	/// <summary>
	/// CimScopeラッパーサービス
	/// </summary>
	public class CimScopeWrapperGrpcServiceImpl : CimScopeWrapperGrpcService.CimScopeWrapperGrpcServiceBase
	{
		/// <summary>
		/// ロガーサービスインスタンス
		/// </summary>
		private readonly ILogger<CimScopeWrapperGrpcServiceImpl> _logger;
		/// <summary>
		/// コンポーネントマネージャインスタンス
		/// </summary>
		private readonly ComponentManager _componentManager;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="logger">ロガーサービスインスタンス</param>
		/// <param name="componentManager">コンポーネントマネージャインスタンス</param>
		public CimScopeWrapperGrpcServiceImpl(ILogger<CimScopeWrapperGrpcServiceImpl> logger, ComponentManager componentManager)
		{
			_logger = logger;
			_componentManager = componentManager;
		}

		/// <summary>
		/// コンポーネントオープン開始
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		bool isOnline:オンライン情報 true=オンライン　false=オフライン
		///		int32 motionFactoryId:モーションファクトリID
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </returns>
		[SupportedOSPlatform("windows")]
		public override Task<OpenComponentRes> OpenComponent(OpenComponentReq req, ServerCallContext context)
		{
			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			//CimScopeのコントローラサーバーまたはプロジェクトサーバーを取得する
			var ladderComComponentContext = new ComponentContextLadderCom();
			ladderComComponentContext.motionfactoryId = req.MotionFactoryId;
			ladderComComponentContext.isOnline = req.IsOnline;
			try
			{
				if (req.IsOnline)
				{
					//オンライン
					IMpControllerServer controllerServer = new MpControllerServer();

					//取得したサーバーよりMotionFactoryを取得する
					object motionFactory;
					object exFactories;
					controllerServer.GetMotionFactoryFromAccessCode(req.MotionFactoryId, out motionFactory, out exFactories);
					IMpMotionFactory mf = (IMpMotionFactory)motionFactory;

					//アクセス情報にモーションファクトリを登録する
					ladderComComponentContext.motionFactory = mf;

					//サーバーを解放する
					Marshal.ReleaseComObject(controllerServer);
				}
				else
				{
					//オフライン
					IMpProjectServer projectServer = new MpProjectServer();
					//取得したサーバーよりMotionFactoryを取得する
					object motionFactory;
					object exFactories;
					projectServer.GetMotionFactoryFromAccessCode(req.MotionFactoryId, out motionFactory, out exFactories);
					IMpMotionFactory mf = (IMpMotionFactory)motionFactory;

					//アクセス情報にモーションファクトリを登録する
					ladderComComponentContext.motionFactory = mf;

					//サーバーを解放する
					Marshal.ReleaseComObject(projectServer);
				}
			}
			catch (COMException ex)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = ex.HResult;
			}

			if (result == 0)
			{
				//コンポーネントマネージャへ登録する
				_componentManager.AddComponent(ladderComComponentContext);
			}

			//返却する
			return Task.FromResult(new OpenComponentRes
			{
				//ステータス
				Result = result,
				//CimScope例外
				CimScopeExceptionResult= cimScopeResult,
				//コンポーネントハンドル
				ComponentHandle = result == 0?ladderComComponentContext.GetComponentHandle():"",
			});
		}

		/// <summary>
		/// コンポーネントクローズ
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		/// </returns>
		[SupportedOSPlatform("windows")]
		public override Task<CloseComponentRes> CloseComponent(CloseComponentReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているかチェックする
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new CloseComponentRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			//本当は、コンポーネントのベースを取得して、type別に処理する？
			ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
			if (component == null)
			{
				//コンポーネントが取得できない
				return Task.FromResult(new CloseComponentRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}
			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			try
			{
				//CimScopeのコントローラサーバーまたはプロジェクトサーバーを取得する
				if (component.isOnline)
				{
					//オンライン
					IMpControllerServer controllerServer = new MpControllerServer();

					//取得したサーバーからMotionFactoryを解放する
					controllerServer.ReleaseMotionFactoryFromAccessCode(
						component.motionfactoryId
						);
					//サーバーを解放する
					Marshal.ReleaseComObject(controllerServer);
				}
				else
				{
					//オフライン
					IMpProjectServer projectServer = new MpProjectServer();

					//取得したサーバーからMotionFactoryを解放する
					projectServer.ReleaseMotionFactoryFromAccessCode(
						component.motionfactoryId
						);
					//サーバーを解放する
					Marshal.ReleaseComObject(projectServer);
				}
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			if (result == CimScopeWrapperErrorCode.NoError)
			{
				//コンポーネントマネージャから削除する
				_componentManager.RemoveComponent(req.ComponentHandle);
			}

			//返却する
			return Task.FromResult(new CloseComponentRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult
			});
		}

		#region YcsLadderProgCOMのラッパーメソッド
		/// <summary>
		/// ラダープログラムのオープン
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		///		string PrgFile;プログラム名
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		int32 Result:実行結果
		/// </returns>
		public override Task<LadderProgOpenRes> LadderProgOpen(LadderProgOpenReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているか？
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle)==false)
			{
				//コンポーネントハンドルが登録されていない
				return Task.FromResult(new LadderProgOpenRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			try
			{
				//モーションファクトリよりラダーCOMを生成する
				//モーションファクトリを取得する
				ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
				if (component == null)
				{
					//コンポーネントハンドルが取得できない
					return Task.FromResult(new LadderProgOpenRes
					{
						Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
					});
				}

				IMpMotionFactory? mf = (IMpMotionFactory?)component.motionFactory;
				if (mf == null)
				{
					//モーションファクトリが取得できない
					return Task.FromResult(new LadderProgOpenRes
					{
						Result = CimScopeWrapperErrorCode.MotionFactoryNotFound,
					});
				}

				//ラダープログラム名称からラダー種別を取得する
				string outPrgFile;
				short programType;
				if (MiscUtil.DbUtil_FormalizePrograms(req.PrgFile, out outPrgFile, out programType) == false)
				{
					//指定されたプログラム名称のエラー
					//コンポーネントハンドルが登録されていない
					return Task.FromResult(new LadderProgOpenRes
					{
						Result = CimScopeWrapperErrorCode.InvalidProgramName,
					});
				}
				//P図面の場合、"."を消す
				if(programType== LegacyConstants.PRGID_LADDER_DWGP)
				{
					outPrgFile = outPrgFile.Replace(".", "");
				}

				//対象が関数でない場合は、先頭に"$G"が必要
				if (programType != LegacyConstants.PRGID_LADDER_FUNC)
				{
					outPrgFile = $"$G{outPrgFile}";
				}

				IMpLadderProg LadderProg = (IMpLadderProg)mf.CreateMotionInstanceClsId(
					typeof(MpLadderProg.MpLadderProgClass).GUID,
					(int)programType,   //ラダーの種別(ラダー種別は、I4でないと開けない)
					outPrgFile			//ラダープログラム名
					);

				//ラダーをオープンする
				LadderProg.Open();

				//ラダーCOM用のアクセス情報を保存する
				component.ladderCom = LadderProg;
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			//返却する
			return Task.FromResult(new LadderProgOpenRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult
			});
		}

		/// <summary>
		/// ラダープログラムのクローズ
		/// </summary>
		/// <param name="req">
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>
		/// レスポンスメッセージ
		///		int32 Result:実行結果
		/// </returns>
		public override Task<LadderProgCloseRes> LadderProgClose(LadderProgCloseReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているか？
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new LadderProgCloseRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			try
			{
				//ラダーをクローズする
				ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
				if (component == null)
				{
					//コンポーネントが取得できない
					return Task.FromResult(new LadderProgCloseRes
					{
						Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
					});
				}
				IMpLadderProg? LadderProg = (IMpLadderProg?)component.ladderCom;
				if (LadderProg == null)
				{
					//ラダーCOMが取得できない
					return Task.FromResult(new LadderProgCloseRes
					{
						Result = CimScopeWrapperErrorCode.ComInstanceCreateFailed,
					});
				}

				//ラダーをクローズする
				LadderProg.Close();
			
				//ラダーCOMのインスタンスを解放する
				//登録されているラダープログラムCOMを解放する
				IMpMotionFactory? mf = (IMpMotionFactory?)component.motionFactory;
				if (mf != null && LadderProg != null)
				{
					mf.ReleaseMotionInstanceClsId(LadderProg);
				}
				component.ladderCom = null;
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			//返却する
			return Task.FromResult(new LadderProgCloseRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult
			});
		}

		/// <summary>
		/// ラダープログラムパケットの読み込み
		/// </summary>
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override Task<LadderProgReadProgramDataPacketRes> LadderProgReadProgramDataPacket(LadderProgReadProgramDataPacketReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているか？
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new LadderProgReadProgramDataPacketRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			byte[]? data = null;

			try
			{
				//コンポーネントを取得する
				ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
				if (component == null)
				{
					//コンポーネントが取得できない
					return Task.FromResult(new LadderProgReadProgramDataPacketRes
					{
						Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
					});
				}
				IMpLadderProg? LadderProg = (IMpLadderProg?)component.ladderCom;
				if (LadderProg == null)
				{
					//ラダーCOMが取得できない
					return Task.FromResult(new LadderProgReadProgramDataPacketRes
					{
						Result = CimScopeWrapperErrorCode.ComInstanceCreateFailed,
					});
				}

				//ラダーのパケットを読み込む
				object value;
				LadderProg.ReadProgramDataPacket((short)req.PacketId, (short)req.PacketNumber, out value);
				data = (byte[])value;
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			//返却する
			ByteString resultData = ByteString.Empty;
			if(result == CimScopeWrapperErrorCode.NoError )
			{
				resultData = ByteString.CopyFrom(data!);
			}
			return Task.FromResult(new LadderProgReadProgramDataPacketRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult,
				Data = resultData
			});
		}

		/// <summary>
		/// ラダープログラムパケットの書き込み
		/// </summary>
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		///		bytes data :パケットデータ
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override Task<LadderProgWriteProgramDataPacketRes> LadderProgWriteProgramDataPacket(LadderProgWriteProgramDataPacketReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているか？
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new LadderProgWriteProgramDataPacketRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			try
			{
				//コンポーネントを取得する
				ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
				if (component == null)
				{
					//コンポーネントが取得できない
					return Task.FromResult(new LadderProgWriteProgramDataPacketRes
					{
						Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
					});
				}
				IMpLadderProg? LadderProg = (IMpLadderProg?)component.ladderCom;
				if (LadderProg == null)
				{
					//ラダーCOMが取得できない
					return Task.FromResult(new LadderProgWriteProgramDataPacketRes
					{
						Result = CimScopeWrapperErrorCode.ComInstanceCreateFailed,
					});
				}

				//ラダーのパケットを書き込む
				byte[] data = req.Data.ToByteArray();
				object value = data;
				LadderProg.WriteProgramDataPacket(ref value);
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			//返却する
			return Task.FromResult(new LadderProgWriteProgramDataPacketRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult
			});
		}

		/// <summary>
		/// ラダープログラムの保存
		/// </summary>
		/// </summary>
		/// リクエストメッセージ
		///		string componentHandle:コンポーネントハンドル
		/// </param>
		/// <param name="context">リクエスト情報</param>
		/// <returns>レスポンスメッセージ</returns>
		public override Task<LadderProgSaveRes> LadderProgSave(LadderProgSaveReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているか？
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new LadderProgSaveRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
				});
			}

			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			try
			{
				//コンポーネントを取得する
				ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
				if (component == null)
				{
					//コンポーネントが取得できない
					return Task.FromResult(new LadderProgSaveRes
					{
						Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered,
					});
				}
				IMpLadderProg? LadderProg = (IMpLadderProg?)component.ladderCom;
				if (LadderProg == null)
				{
					//ラダーCOMが取得できない
					return Task.FromResult(new LadderProgSaveRes
					{
						Result = CimScopeWrapperErrorCode.ComInstanceCreateFailed,
					});
				}

				//ラダーを保存する
				LadderProg.Write();
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			//返却する
			return Task.FromResult(new LadderProgSaveRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult
			});
		}


		//以降、検証のため実装
		//ラダープロパティ取得
		public override Task<LadderProgGetPropertyRes> LadderProgGetProperty(LadderProgGetPropertyReq req, ServerCallContext context)
		{
			//コンポーネントハンドルが登録されているか？
			if (_componentManager.IsComponentHandleExist(req.ComponentHandle) == false)
			{
				//指定されたコンポーネントハンドルが登録されていない
				return Task.FromResult(new LadderProgGetPropertyRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered
				});
			}

			//ラダーCOMよりプロパティを取得する
			ComponentContextLadderCom? component = (ComponentContextLadderCom?)_componentManager.GetComponent(req.ComponentHandle);
			if (component == null)
			{
				//コンポーネントハンドルが取得できない
				return Task.FromResult(new LadderProgGetPropertyRes
				{
					Result = CimScopeWrapperErrorCode.ComponentHandleNotRegistered
				});
			}
			IMpLadderProg? LadderProg = (IMpLadderProg?)component.ladderCom;
			IMpLadderProgConfig? config = (IMpLadderProgConfig?)LadderProg;
			if (config == null)
			{
				//ラダーCOMが取得できない
				return Task.FromResult(new LadderProgGetPropertyRes
				{
					Result = CimScopeWrapperErrorCode.ComInstanceCreateFailed
				});
			}

			string programComment = new string("");
			CimScopeWrapperErrorCode result = CimScopeWrapperErrorCode.NoError;
			int cimScopeResult = 0;
			try
			{
				programComment = config.ProgramComment;
			}
			catch (COMException e)
			{
				result = CimScopeWrapperErrorCode.CimScopeException;
				cimScopeResult = e.HResult;
			}

			LadderProg = null;
			config = null;

			//返却する
			return Task.FromResult(new LadderProgGetPropertyRes
			{
				Result = result,
				CimScopeExceptionResult = cimScopeResult,
				ProgramComment = programComment//プログラムコメント
			});
		}
		#endregion YcsLadderProgCOMのラッパーメソッド
	}
}
